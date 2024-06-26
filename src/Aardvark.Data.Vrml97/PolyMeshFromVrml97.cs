﻿using Aardvark.Base;
using Aardvark.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Aardvark.Data.Vrml97
{
    public static class PolyMeshFromVrml97
    {
        [Flags]
        public enum Options
        {
            Default                     = 0x0000,

            ReverseTriangles            = 0x01,
            AddPerFaceNormals           = 0x02,
            AddCreaseNormals            = 0x04,
            SkipDegenerateFaces         = 0x08,

            PreMultiplyTransform        = 0x10,

            TryFixSpecViolations        = 0x100,
            IgnorePresentNormals        = 0x200,

            NoVertexColorsFromMaterial  = 0x1000,

            AddPerFaceNormalsAndPreMultiplyTransform =
                AddPerFaceNormals | PreMultiplyTransform ,

            AddCreaseNormalsAndPreMultiplyTransform =
                AddPerFaceNormals | PreMultiplyTransform,

            StandardSettings = AddCreaseNormalsAndPreMultiplyTransform
                                | SkipDegenerateFaces,
        }

        public static PolyMesh CreateFromIfs(SymMapBase ifs)
        {
            return CreateFromIfs(
                ifs, Options.StandardSettings
                );
        }

        public static PolyMesh CreateFromIfs(SymMapBase ifs,
                Options options
            )
        {
            #region Setup
            /*
             * [Vrml97 SPEC]
             * IndexedFaceSet { 
             *    SFNode  color             NULL
             *    SFNode  coord             NULL
             *    SFNode  normal            NULL
             *    SFNode  texCoord          NULL
             *    SFBool  ccw               TRUE
             *    MFInt32 colorIndex        []    # [-1,)
             *    SFBool  colorPerVertex    TRUE
             *    SFBool  convex            TRUE
             *    MFInt32 coordIndex        []    # [-1,)
             *    SFFloat creaseAngle       0     # [0,)
             *    MFInt32 normalIndex       []    # [-1,)
             *    SFBool  normalPerVertex   TRUE
             *    SFBool  solid             TRUE
             *    MFInt32 texCoordIndex     []    # [-1,)
             *  }
             */
            SymMapBase color = ifs.Get<SymMapBase>(Vrml97Sym.color);
            SymMapBase coord = ifs.Get<SymMapBase>(Vrml97Sym.coord);
            SymMapBase normal = ifs.Get<SymMapBase>(Vrml97Sym.normal);
            SymMapBase texCoord = ifs.Get<SymMapBase>(Vrml97Sym.texCoord);
            bool ccw = ifs.Get<bool>(Vrml97Sym.ccw, true);
            var colorIndex = ifs.Get<List<int>>(Vrml97Sym.colorIndex);
            bool colorPerVertex = ifs.Get<bool>(Vrml97Sym.colorPerVertex, true);
            bool convex = ifs.Get<bool>(Vrml97Sym.convex, true);
            var coordIndex = ifs.Get<List<int>>(Vrml97Sym.coordIndex);
            float creaseAngle = ifs.Get<float>(Vrml97Sym.creaseAngle, 0.0f);
            var normalIndex = ifs.Get<List<int>>(Vrml97Sym.normalIndex);
            bool normalPerVertex = ifs.Get<bool>(Vrml97Sym.normalPerVertex, true);
            bool solid = ifs.Get<bool>(Vrml97Sym.solid, true);
            var texCoordIndex = ifs.Get<List<int>>(Vrml97Sym.texCoordIndex);

            bool skipDegenerates = (options & Options.SkipDegenerateFaces) != 0;
            bool addPerFaceNormals = (options & Options.AddPerFaceNormals) != 0;
            bool addCreaseNormals = (options & Options.AddCreaseNormals) != 0;
            bool ignorePresentNormals = (options & Options.IgnorePresentNormals) != 0;
            bool performCreateNormals = false;
            bool preMultiplyTransform
                    = (options & Options.PreMultiplyTransform) != 0;

            if ((options & Options.ReverseTriangles) != 0)
                ccw = !ccw;

            PolyMesh m = new PolyMesh();

            #endregion

            #region Vertices

            if (coord == null)
                throw new Exception(
                    "Vrml97 spec violation!" +
                    "IndexedFaceSet node: field 'coord' MUST NOT be null."
                    );
            if (!coord.Contains(Vrml97Sym.point))
                throw new Exception(
                    "Vrml97 spec violation!" +
                    "Coordinate node: field 'coord' MUST NOT be null."
                    );

            List<V3f> vertexPositionList = coord.Get<List<V3f>>(Vrml97Sym.point);
            int vertexCount = vertexPositionList.Count;

            if (vertexCount == 0)
            {
                Report.Line(2, "Note: Ignoring an IndexedFaceSet with 0 vertices.");
                return m;
            }

            var positionArray = new V3d[vertexCount];

            if (ifs.Contains(Vrml97Sym.transform))
            {
                Trafo3d trafo = ifs.Get<Trafo3d>(Vrml97Sym.transform, Trafo3d.Identity);
                if (preMultiplyTransform)
                {
                    M44d mat = trafo.Forward;
                    for (int vi = 0; vi < vertexCount; vi++)
                        positionArray[vi]
                            = Mat.TransformPos(mat, (V3d)vertexPositionList[vi]);
                }
                else
                {
                    m.InstanceAttributes[PolyMesh.Property.Trafo3d] = trafo;

                    for (int vi = 0; vi < vertexCount; vi++)
                        positionArray[vi] = (V3d)vertexPositionList[vi];
                }
            }
            else
            {
                for (int vi = 0; vi < vertexCount; vi++)
                    positionArray[vi] = (V3d)vertexPositionList[vi];
            }
            m.PositionArray = positionArray;

            #endregion

            #region Faces

            /*
             * [Vrml97 SPEC]
             * IndexedFaceSet uses the indices in its coordIndex field
             * to specify the polygonal faces by indexing into the
             * coordinates in the Coordinate node. An index of "-1"
             * indicates that the current face has ended and the next
             * one begins. The last face may be (but does not have to be)
             * followed by a "-1" index. If the greatest index in the
             * coordIndex field is N, the Coordinate node shall contain
             * N+1 coordinates (indexed as 0 to N).
             * 
             * Each face of the IndexedFaceSet shall have:
             *      1. at least three non-coincident vertices;
             *      2. vertices that define a planar polygon;
             *      3. vertices that define a non-self-intersecting polygon. 
             * 
             * Otherwise, The results are undefined.
             */

            if (coordIndex == null)
                throw new Exception(
                    "Vrml97 spec violation!" +
                    "IndexedFaceSet node: field 'coordIndex' MUST NOT be null."
                    );

            int faceCount = 0;
            int vrmlFaceCount = 0;
            int fvc = 0;
            int vertexIndexCount = 0;
            bool validFace = true;

            if (coordIndex.Count == 0)
                return new PolyMesh();

            int coordIndexCount = coordIndex[coordIndex.Count - 1] == -1
                            ? coordIndex.Count
                            : coordIndex.Count + 1;

            for (int xi = 0; xi < coordIndexCount; xi++)
            {
                int x = xi == coordIndex.Count ? -1 : coordIndex[xi];
                if (x == -1) // face end marker
                {
                    if (fvc == 0) break; // can happen at the very end
                    if (fvc < 3)
                        throw new Exception(
                            "Vrml97 spec violation! "
                            + "IndexedFaceSet node: each face of the "
                            + "IndexedFaceSet shall have at least three "
                            + "non-coincident vertices (non-conincidence"
                            + " is not checked here)"
                            );
                    if (validFace && skipDegenerates)
                    {
                        if (coordIndex[xi - fvc] == coordIndex[xi - 1])
                            validFace = false;
                        else
                            for (int fvi = -fvc + 1; fvi < 0; fvi++)
                                if (coordIndex[xi + fvi - 1] == coordIndex[xi + fvi])
                                { validFace = false; break; }
                    }
                    if (validFace)
                    {
                        ++faceCount;
                        vertexIndexCount += fvc;
                    }
                    else
                        validFace = true;
                    ++vrmlFaceCount;
                    fvc = 0;
                }
                else
                {
                    fvc += 1;
                    if (x >= vertexCount) validFace = false;
                }
            }

            int[] firstIndexArray = new int[faceCount + 1];
            int[] vertexIndexArray = new int[vertexIndexCount];
            bool[] isValidOfVrmlFace = new bool[vrmlFaceCount];
            int[] vrmlFaceIndexOfFace = new int[faceCount];

            int faceIndex = 0;
            int vii = 0;
            int vrmlFaceIndex = 0;
            fvc = 0;
            validFace = true;
            firstIndexArray[0] = 0;

            for (int xi = 0; xi < coordIndexCount; xi++)
            {
                int x = xi == coordIndex.Count ? -1 : coordIndex[xi];
                if (x == -1) // face end
                {
                    if (fvc == 0) break; // can happen at the very end
                    if (validFace && skipDegenerates)
                    {
                        if (coordIndex[xi - fvc] == coordIndex[xi - 1])
                            validFace = false;
                        else
                            for (int fvi = -fvc + 1; fvi < 0; fvi++)
                                if (coordIndex[xi + fvi - 1] == coordIndex[xi + fvi])
                                    { validFace = false; break; }
                    }
                    if (validFace)
                    {
                        vrmlFaceIndexOfFace[faceIndex] = vrmlFaceIndex;
                        isValidOfVrmlFace[vrmlFaceIndex] = true;
                        if (ccw)
                        {
                            for (int xbi = xi - fvc, i = 0; i < fvc; i++)
                                vertexIndexArray[vii++] = coordIndex[xbi + i];
                        }
                        else
                        {
                            for (int xbi = xi - 1, i = 0; i < fvc; i++)
                                vertexIndexArray[vii++] = coordIndex[xbi - i];
                        }
                        ++faceIndex;
                        firstIndexArray[faceIndex] = vii;
                    }
                    else
                        validFace = true;
                    vrmlFaceIndex++;
                    fvc = 0;
                }
                else
                {
                    fvc += 1;
                    if (x >= vertexCount) validFace = false;
                }
            }

            m.FirstIndexArray = firstIndexArray;
            m.VertexIndexArray = vertexIndexArray;

            #endregion

            #region Colors

            if (color != null)
            {
                if (!color.Contains("color"))
                    throw new Exception(
                        "Vrml97 spec violation!" +
                        "Color node: field 'color' MUST NOT be null."
                        );

                List<C3f> colorList = color.Get<List<C3f>>(Vrml97Sym.color);

                if (colorPerVertex == false)
                {
                    if (colorIndex != null)
                    {
                        int colorCount = colorList.Count;
                        if (colorCount < vrmlFaceCount)
                        {
                            var msg = "Vrml97 spec violation! "
                                + "IndexedFaceSet node: there shall be at "
                                + "least as many indices in the colorIndex "
                                + "field as there are faces in the "
                                + "IndexedFaceSet";
                            if ((options & Options.TryFixSpecViolations) == 0)
                                throw new Exception(msg);
                            else
                                Report.Warn(msg);
                            for (int i = colorCount; i < faceCount; i++)
                                colorList.Add(colorList[i % colorCount]);
                            colorCount = colorList.Count;
                        }
                        if (colorIndex.Max() >= colorCount)
                            throw new Exception(
                                "Vrml97 spec violation! "
                                + "If the greatest index in the colorIndex "
                                + "field is N, then there shall be N+1 "
                                + "colours in the Color node.");
                        if (colorIndex.Min() < 0)
                            throw new Exception(
                                "Vrml97 spec violation! "
                                + "The colorIndex field shall not contain "
                                + "any negative entries.");

                        var colorIndexArray
                                = colorIndex.BackwardMappedCopyToArray(vrmlFaceIndexOfFace);
                        m.FaceAttributes[PolyMesh.Property.Colors] = colorIndexArray;

                        var colorArray = colorList.MapToArray(C4f.FromC3f);
                        m.FaceAttributes[-PolyMesh.Property.Colors] = colorArray;
                    }
                    else // if (colorIndex == null)
                    {
                        int colorCount = colorList.Count;
                        if (colorCount < faceCount)
                            throw new Exception(
                                "Vrml97 spec violation! "
                                + "There shall be at least as many colours "
                                + "in the Color node as there are faces.");
                        /* 
                         * [Vrml97 SPEC]
                         * ... then the colours in the Color node are applied
                         * to each face of the IndexedFaceSet in order.
                         */
                        var colorArray
                                = colorList.BackwardMappedCopyToArray(vrmlFaceIndexOfFace);
                        m.FaceAttributes[PolyMesh.Property.Colors] = colorArray;
                    }
                }
                else // if (colorPerVertex == true)
                {
                    /*
                     * [Vrml97 SPEC]
                     * a. If the colorIndex field is not empty, then colours
                     *    are applied to each vertex of the IndexedFaceSet in
                     *    exactly the same manner that the coordIndex field is
                     *    used to choose coordinates for each vertex from the
                     *    Coordinate node. The colorIndex field shall contain
                     *    at least as many indices as the coordIndex field, and
                     *    shall contain end-of-face markers (-1) in exactly the
                     *    same places as the coordIndex field. If the greatest
                     *    index in the colorIndex field is N, then there shall
                     *    be N+1 colours in the Color node.
                     * 
                     * b. If the colorIndex field is empty, then the coordIndex
                     *    field is used to choose colours from the Color node.
                     *    If the greatest index in the coordIndex field is N,
                     *    then there shall be N+1 colours in the Color node. 
                     */
                    int colorCount = colorList.Count;

                    if (colorIndex != null)
                    {
                        vii = 0;
                        vrmlFaceIndex = 0;
                        fvc = 0;
                        int colorIndexCount = colorIndex[colorIndex.Count - 1] == -1
                                                ? colorIndex.Count
                                                : colorIndex.Count + 1;

                        var colorIndexArray = new int[vertexIndexCount];
                        for (int xi = 0; xi < colorIndexCount; xi++)
                        {
                            int x = xi == colorIndex.Count ? -1 : colorIndex[xi];
                            if (x == -1) // face end
                            {
                                if (fvc == 0) break; // can happen at the very end
                                if (isValidOfVrmlFace[vrmlFaceIndex])
                                {
                                    if (ccw)
                                    {
                                        for (int xbi = xi - fvc, i = 0; i < fvc; i++)
                                            colorIndexArray[vii++] = colorIndex[xbi + i];
                                    }
                                    else
                                    {
                                        for (int xbi = xi - 1, i = 0; i < fvc; i++)
                                            colorIndexArray[vii++] = colorIndex[xbi - i];
                                    }
                                }
                                ++vrmlFaceIndex;
                                fvc = 0;
                            }
                            else
                                ++fvc;
                        }
                        m.FaceVertexAttributes[PolyMesh.Property.Colors] = colorIndexArray;

                        var colorArray = colorList.MapToArray(C4f.FromC3f);
                        m.FaceVertexAttributes[-PolyMesh.Property.Colors] = colorArray;
                    }
                    else
                    {
                        var colorArray = colorList.MapToArray(vertexCount, C4f.FromC3f);
                        m.VertexAttributes[PolyMesh.Property.Colors] = colorArray;
                    }
                }
            }

            #endregion

            #region Normals

            if ((normal != null) && (!ignorePresentNormals))
            {
                if (!normal.Contains("vector"))
                    throw new Exception(
                        "Vrml97 spec violation!" +
                        "Normal node: field 'vector' MUST NOT be null."
                        );

                List<V3f> normalList = normal.Get<List<V3f>>(Vrml97Sym.vector);

                if (preMultiplyTransform && ifs.Contains(Vrml97Sym.transform))
                {
                    Trafo3d trafo = ifs.Get<Trafo3d>(Vrml97Sym.transform, Trafo3d.Identity);
                    M44d transposedInverse = trafo.Backward.Transposed;
                    int imax = normalList.Count;

                    for (int i = 0; i < imax; i++)
                    {
                        normalList[i] = (V3f)(Mat.TransformDir(
                            transposedInverse, (V3d)normalList[i]
                            ).Normalized);
                    }
                }

                if (normalPerVertex == false)
                {
                    if (normalIndex != null)
                    {
                        int normalCount = normalList.Count;
                        if (normalCount < faceCount)
                            throw new Exception(
                                "Vrml97 spec violation! "
                                + "IndexedFaceSet node: there shall be at "
                                + "least as many indices in the normalIndex "
                                + "field as there are faces in the "
                                + "IndexedFaceSet"
                                );
                        if (normalIndex.Max() >= normalCount)
                            throw new Exception(
                                "Vrml97 spec violation!" +
                                "If the greatest index in the normalIndex " +
                                "is N, then there shall be N+1 normals in " +
                                "the Normal node."
                                );
                        if (normalIndex.Min() < 0)
                            throw new Exception(
                                "Vrml97 spec violation! "
                                + "The normalIndex field shall not contain "
                                + "any negative entries.");
                        /* 
                         * [Vrml97 SPEC]
                         * ... then one normal is used for each face of the
                         * IndexedFaceSet.
                         */

                        var normalIndexArray =
                                normalIndex.BackwardMappedCopyToArray(vrmlFaceIndexOfFace);
                        m.FaceAttributes[PolyMesh.Property.Normals] = normalIndexArray;

                        var normalArray =
                            (options & Options.ReverseTriangles) == Options.Default
                                ? normalList.CopyToArray(normalCount)
                                : normalList.MapToArray(normalCount, n => -n);
                        m.FaceAttributes[-PolyMesh.Property.Normals] = normalArray;
                    }
                    else // if (normalIndex == null)
                    {
                        if (normalList.Count < vrmlFaceCount)
                            throw new Exception(
                                "Vrml97 spec violation! "
                                + "There shall be at least as many normals "
                                + "in the Normals node as there are faces.");
                        /* 
                         * [Vrml97 SPEC]
                         * ... then the normals in the Normal node are applied
                         * to each face of the IndexedFaceSet in order.
                         */
                        var normalArrayX = new V3f[faceCount];
                        var normalArray = 
                                (options & Options.ReverseTriangles) == Options.Default
                                    ? normalList.BackwardMappedCopyToArray(vrmlFaceIndexOfFace)
                                    : normalList.BackwardMappedCopyToArray(vrmlFaceIndexOfFace,
                                                                           n => -n);
                        m.FaceAttributes[PolyMesh.Property.Normals] = normalArray;
                    }
                }
                else // if (normalPerVertex == true)
                {
                    int normalCount = normalList.Count;
                    if (normalIndex != null)
                    {
                        vii = 0;
                        vrmlFaceIndex = 0;
                        fvc = 0;
                        int normalIndexCount = normalIndex[normalIndex.Count - 1] == -1
                                                ? normalIndex.Count
                                                : normalIndex.Count + 1;

                        var normalIndexArray = new int[vertexIndexCount];
                        for (int xi = 0; xi < normalIndexCount; xi++)
                        {
                            int x = xi == normalIndex.Count ? -1 : normalIndex[xi];
                            if (x == -1) // face end
                            {
                                if (fvc == 0) break; // can happen at the very end
                                if (isValidOfVrmlFace[vrmlFaceIndex])
                                {
                                    if (ccw)
                                    {
                                        for (int xbi = xi - fvc, i = 0; i < fvc; i++)
                                            normalIndexArray[vii++] = normalIndex[xbi + i];
                                    }
                                    else
                                    {
                                        for (int xbi = xi - 1, i = 0; i < fvc; i++)
                                            normalIndexArray[vii++] = normalIndex[xbi - i];
                                    }
                                }
                                ++vrmlFaceIndex;
                                fvc = 0;
                            }
                            else
                                ++fvc;
                        }
                        m.FaceVertexAttributes[PolyMesh.Property.Normals] = normalIndexArray;

                        var normalArray =
                            (options & Options.ReverseTriangles) == Options.Default
                                ? normalList.CopyToArray(normalCount)
                                : normalList.MapToArray(normalCount, n => -n);
                        m.FaceVertexAttributes[-PolyMesh.Property.Normals] = normalArray;
                    }
                    else
                    {
                        var normalArray =
                            (options & Options.ReverseTriangles) == Options.Default
                                ? normalList.CopyToArray(vertexCount)
                                : normalList.MapToArray(vertexCount, n => -n);
                        m.VertexAttributes[PolyMesh.Property.Normals] = normalArray;
                    }
                }
            }
            else // if (normal == null)
            {
                /*
                 * the browser shall automatically generate normals, using
                 * creaseAngle to determine if and how normals are smoothed
                 * across shared vertices (see 4.6.3.5, Crease angle field).
                 */
                if (addPerFaceNormals || addCreaseNormals)
                     performCreateNormals = true;
            }

            #endregion

            #region Texture Coordinates

            /*
             * TEXCOORDS.
             * 
             * [Vrml97 SPEC]
             * If the texCoord field is not NULL, it shall contain a
             * TextureCoordinate node. The texture coordinates in that
             * node are applied to the vertices of the IndexedFaceSet
             * as follows:
             */
            if (texCoord != null)
            {
                if (!texCoord.Contains(Vrml97Sym.point))
                    throw new Exception(
                        "Vrml97 spec violation!" +
                        "TextureCoordinate node: field 'point' MUST NOT be null."
                        );

                // texCoord array (from texCoord node)
                var texCoordList = texCoord.Get<List<V2f>>(Vrml97Sym.point);
                int texCoordCount = texCoordList.Count;

                // map texture coordinates to Aardvark texture coordinate system (flip Y)
                var texCoordArray = texCoordList.MapToArray(crd => new V2f(crd.X, 1 - crd.Y)).ToArray();

                // if texCoordIndex is empty, then the coordIndex array is used to choose texture coordinates.
                if (texCoordIndex == null)
                    texCoordIndex = coordIndex.Copy();

                if (texCoordIndex != null)
                {
                    if (texCoordIndex.Count != coordIndex.Count)
                        throw new Exception(
                            "Vrml97 spec violation!" +
                            "The texCoordIndex field shall contain at least " +
                            "as many indices as the coordIndex field."
                            );

                    vii = 0;
                    vrmlFaceIndex = 0;
                    fvc = 0;
                    int texCoordIndexCount = texCoordIndex[texCoordIndex.Count - 1] == -1
                                            ? texCoordIndex.Count
                                            : texCoordIndex.Count + 1;

                    var texCoordIndexArray = new int[vertexIndexCount];
                    for (int xi = 0; xi < texCoordIndexCount; xi++)
                    {
                        int x = xi == texCoordIndex.Count ? -1 : texCoordIndex[xi];
                        if (x == -1) // face end
                        {
                            if (fvc == 0) break; // can happen at the very end
                            if (isValidOfVrmlFace[vrmlFaceIndex])
                            {
                                if (ccw)
                                {
                                    for (int xbi = xi - fvc, i = 0; i < fvc; i++)
                                        texCoordIndexArray[vii++] = texCoordIndex[xbi + i];
                                }
                                else
                                {
                                    for (int xbi = xi - 1, i = 0; i < fvc; i++)
                                        texCoordIndexArray[vii++] = texCoordIndex[xbi - i];
                                }
                            }
                            ++vrmlFaceIndex;
                            fvc = 0;
                        }
                        else
                            ++fvc;
                    }
                    m.FaceVertexAttributes[PolyMesh.Property.DiffuseColorCoordinates] = texCoordIndexArray;

                    /*
                        * and shall contain end-of-face markers (-1) in exactly
                        * the same places as the coordIndex field.
                        */
                    int N = 0;
                    int imax = texCoordIndex.Count;
                    for (int i = 0; i < imax; i++)
                    {
                        int index = texCoordIndex[i];
                        if (index == -1)
                        {
                            if (coordIndex[i] != -1)
                            {
                                // SPEC VIOLATION!
                                throw new Exception(
                                    "Vrml97 spec violation!" +
                                    "The texCoordIndex field shall contain " +
                                    "end-of-face markers (-1) in exactly the " +
                                    "same places as the coordIndex field."
                                    );
                            }
                        }
                        else
                        {
                            if (index > N) N = index;
                        }
                    }

                    /*
                        * If the greatest index in the texCoordIndex field is N,
                        * then there shall be N+1 texture coordinates in the
                        * TextureCoordinate node.
                        */
                    if (N + 1 != texCoordList.Count)
                    {
                        // SPEC VIOLATION!
                        var msg = String.Format(
                            "Vrml97 spec violation!" +
                            "If the greatest index in the texCoordIndex " +
                            "field is N, then there shall be N+1 texture " +
                            "coordinates in the TextureCoordinate node." +
                            "(have {0}, want {1}", N + 1, texCoordList.Count);
                        if ((options & Options.TryFixSpecViolations) != 0)
                        {
                            Report.Warn(msg);
                        }
                        else
                        {
                            throw new Exception(msg);
                        }
                    }

                    m.FaceVertexAttributes[-PolyMesh.Property.DiffuseColorCoordinates] = texCoordArray;
                }
                else
                {
                    m.VertexAttributes[PolyMesh.Property.DiffuseColorCoordinates] = texCoordArray;
                }

            }
            else // if (texCoord == null)
            {
                /* ..., a default texture coordinate mapping is calculated
                 * using the local coordinate system bounding box of the shape.
                 * The longest dimension of the bounding box defines the
                 * S coordinates, and the next longest defines the T
                 * coordinates. If two or all three dimensions of the
                 * bounding box are equal, ties shall be broken by
                 * choosing the X, Y, or Z dimension in that order
                 * of preference. The value of the S coordinate ranges
                 * from 0 to 1, from one end of the bounding box to
                 * the other. The T coordinate ranges between 0 and
                 * the ratio of the second greatest dimension of the
                 * bounding box to the greatest dimension.
                 */

                // [TODO] unimplented VRML spec
                // throw new NotImplementedException();
            }

            #endregion

            #region Compute Normals

            if (performCreateNormals)
            {
                if ((options & Options.AddPerFaceNormals) != 0)
                {
                    m.AddPerFaceNormals();
                }

                if ((options & Options.AddCreaseNormals) != 0)
                {
                    m = m.WithPerVertexIndexedNormals(creaseAngle);
                }
            }

            #endregion

            #region Instance Color from Material

            var material = ifs.Get<SymMapBase>(Vrml97Sym.material);

            if (material != null)
            {
                if (!m.HasColors && (options & Options.NoVertexColorsFromMaterial) == 0)
                {
                    var opacity = 1 - material.Get<float>(Vrml97Sym.transparency);
                    var diffuse = material.Get<C3f>(Vrml97Sym.diffuseColor);
                    m.InstanceAttributes[PolyMesh.Property.Colors]
                            = new C4f(diffuse.R, diffuse.G, diffuse.B, opacity);
                }
            }

            #endregion

            #region Instance Attributes

            // store name, creaseAngle, windingOrder and solid attributes as PolyMesh instance attributes

            if (ifs.Contains(Vrml97Sym.creaseAngle))
                m.InstanceAttributes[PolyMesh.Property.CreaseAngle] = creaseAngle;

            if (ifs.Contains(Vrml97Sym.solid))
                m.InstanceAttributes[Vrml97Sym.solid] = solid;

            m.InstanceAttributes[PolyMesh.Property.WindingOrder] = !ifs.Contains(Vrml97Sym.ccw) ? ccw ?
                        PolyMesh.WindingOrder.Undefined : PolyMesh.WindingOrder.CounterClockwise : PolyMesh.WindingOrder.Clockwise;

            // add name for string definition of the vertex geometry
            var DefName = ifs.Get<string>(Vrml97Sym.DEFname);
            if (DefName != null)
                m.InstanceAttributes[PolyMesh.Property.Name] = DefName;

            #endregion

            return m;
        }
    }
}
