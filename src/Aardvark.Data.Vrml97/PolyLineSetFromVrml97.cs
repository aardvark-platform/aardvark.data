using Aardvark.Base;
using Aardvark.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Aardvark.Data.Vrml97
{
    /// <summary>
    /// Builds the shared PolyLineSet from an IndexedLineSet node map, the line counterpart of PolyMeshFromVrml97.
    /// </summary>
    public static class PolyLineSetFromVrml97
    {
        /// <summary>
        /// The polylines of an IndexedLineSet, packed like PolyMesh faces with the end-of-polyline markers stripped, vertices as V3d.
        /// The Vrml97 colour table and indices are resolved: colorPerVertex FALSE gives one C4b per polyline in LineAttributes[Colors],
        /// colorPerVertex TRUE one per polyline vertex in LineVertexAttributes[Colors]; a line set without a Color node has no colour attribute.
        /// Empty polylines, polylines with a single vertex and polylines with a coordIndex outside the Coordinate node are dropped (the latter two with a warning).
        /// Returns null for a node that draws nothing: no vertices, an empty coordIndex or no valid polyline.
        /// Spec violations that cannot be repaired throw.
        /// </summary>
        public static PolyLineSet CreateFromIls(SymMapBase ils)
        {
            // [Vrml97 SPEC]
            // IndexedLineSet {
            //     SFNode  color          NULL
            //     SFNode  coord          NULL
            //     MFInt32 colorIndex     []
            //     SFBool  colorPerVertex TRUE
            //     MFInt32 coordIndex     []
            // }
            static Exception Violation(string msg) => new Exception("Vrml97 spec violation! " + msg);
            const string MarkerMismatch = "The colorIndex field must contain end-of-polyline markers (-1) in exactly the same places as the coordIndex field.";

            var color = ils.Get<SymMapBase>(Vrml97Sym.color);
            var coord = ils.Get<SymMapBase>(Vrml97Sym.coord);
            var colorPerVertex = ils.Get(Vrml97Sym.colorPerVertex, true);

            // NOTE: both index fields default to [] -> treat an empty list like an absent field
            var colorIndex = ils.Get<List<int>>(Vrml97Sym.colorIndex);
            var coordIndex = ils.Get<List<int>>(Vrml97Sym.coordIndex);
            if (colorIndex != null && colorIndex.Count == 0)
                colorIndex = null;
            if (coordIndex != null && coordIndex.Count == 0)
                coordIndex = null;

            if (coord == null)
                throw Violation("IndexedLineSet node: field 'coord' MUST NOT be null.");
            if (!coord.Contains(Vrml97Sym.point))
                throw Violation("Coordinate node: field 'point' MUST NOT be null.");

            var vertexPositionList = coord.Get<List<V3f>>(Vrml97Sym.point);
            int vertexCount = vertexPositionList != null ? vertexPositionList.Count : 0;

            if (vertexCount == 0)
            {
                Report.Line(2, "Note: Ignoring an IndexedLineSet with 0 vertices.");
                return null;
            }

            // NOTE: an empty coordIndex is the field default and simply means "no lines"
            if (coordIndex == null)
            {
                Report.Line(2, "Note: Ignoring an IndexedLineSet with an empty coordIndex.");
                return null;
            }

            List<C3f> colorList = null;
            if (color != null)
            {
                if (!color.Contains(Vrml97Sym.color))
                    throw Violation("Color node: field 'color' MUST NOT be null.");

                colorList = color.Get<List<C3f>>(Vrml97Sym.color);
                if (colorList == null || colorList.Count == 0)
                {
                    Report.Warn("IndexedLineSet: Color node without colors -> ignoring colors.");
                    colorList = null;
                }
            }

            if (colorList == null) // colorIndex is meaningless without colors
                colorIndex = null;

            if (colorIndex != null)
            {
                // -1 is only a legal entry in the per-vertex case (end-of-polyline marker)
                if (colorIndex.Min() < (colorPerVertex ? -1 : 0))
                    throw Violation("The colorIndex field shall not contain any negative entries.");
                if (colorIndex.Max() >= colorList.Count)
                    throw Violation("If the greatest index in the colorIndex field is N, then there shall be N+1 colors in the Color node.");
            }

            // [Vrml97 SPEC] colorPerVertex TRUE:
            //  a. If the colorIndex field is not empty, it selects a color per vertex in exactly the same manner
            //     that coordIndex chooses coordinates: it shall contain at least as many indices as coordIndex
            //     and shall have its end-of-polyline markers (-1) in exactly the same places.
            //  b. If the colorIndex field is empty, coordIndex is used to choose colors from the Color node.
            //  In both cases the greatest used index N requires N+1 colors in the Color node.
            var indexedPerVertexColors = colorPerVertex && colorIndex != null;
            if (indexedPerVertexColors && colorIndex.Count < coordIndex.Count)
                throw Violation("IndexedLineSet node: the colorIndex field shall contain at least as many indices as the coordIndex field.");

            // NOTE: the trailing -1 is optional -> if missing, loop one entry further and pretend there is one
            int coordIndexCount = coordIndex[coordIndex.Count - 1] == -1 ? coordIndex.Count : coordIndex.Count + 1;

            var indices = new List<int>(coordIndex.Count);
            var firstIndices = new List<int> { 0 };
            var vertexColorIndices = indexedPerVertexColors ? new List<int>(coordIndex.Count) : null; // kept parallel to indices
            var polyLineSources = new List<int>(); // ordinal of each emitted polyline within coordIndex (per-polyline colors must not shift when degenerate polylines are dropped)

            var polyLineStart = 0;    // start of the current polyline within indices
            var polyLineOrdinal = 0;  // ordinal of the current polyline within coordIndex
            var polyLineValid = true; // false once an illegal index was seen -> the whole polyline is dropped

            for (int xi = 0; xi < coordIndexCount; xi++)
            {
                int x = xi < coordIndex.Count ? coordIndex[xi] : -1;

                if (x == -1) // polyline end marker
                {
                    if (indexedPerVertexColors && xi < colorIndex.Count && colorIndex[xi] != -1)
                        throw Violation(MarkerMismatch);

                    var polyLineVertexCount = indices.Count - polyLineStart;
                    if (polyLineValid && polyLineVertexCount >= 2)
                    {
                        firstIndices.Add(indices.Count);
                        polyLineSources.Add(polyLineOrdinal);
                    }
                    else // drop: empty polylines (consecutive/trailing -1) are common in the wild and stay silent, real data loss warns
                    {
                        indices.RemoveRange(polyLineStart, polyLineVertexCount);
                        vertexColorIndices?.RemoveRange(polyLineStart, polyLineVertexCount);
                        if (polyLineValid && polyLineVertexCount == 1)
                            Report.Warn("IndexedLineSet: dropping polyline {0} with a single vertex.", polyLineOrdinal);
                    }

                    polyLineStart = indices.Count;
                    polyLineOrdinal++;
                    polyLineValid = true;
                }
                else if (x < 0 || x >= vertexCount) // Vrml97 spec violation -> drop the whole polyline instead of failing the load
                {
                    if (polyLineValid)
                        Report.Warn("IndexedLineSet: coordIndex {0} out of range [0, {1}) -> dropping polyline {2}.", x, vertexCount, polyLineOrdinal);
                    polyLineValid = false;
                }
                else
                {
                    indices.Add(x);
                    if (indexedPerVertexColors)
                    {
                        if (colorIndex[xi] == -1)
                            throw Violation(MarkerMismatch);
                        vertexColorIndices.Add(colorIndex[xi]);
                    }
                }
            }

            if (polyLineSources.Count == 0)
            {
                Report.Line(2, "Note: Ignoring an IndexedLineSet without any valid polyline.");
                return null;
            }

            var result = new PolyLineSet
            {
                VertexArray = vertexPositionList.Select(p => p.ToV3d()).ToArray(),
                VertexIndexArray = indices.ToArray(),
                FirstIndexArray = firstIndices.ToArray(),
            };

            if (colorList == null)
                return result;

            if (colorPerVertex)
            {
                // (a) explicit indices, markers already stripped -> parallel to VertexIndexArray
                // (b) no colorIndex -> VertexIndexArray doubles as color index
                if (!indexedPerVertexColors && colorList.Count <= indices.Max())
                    throw Violation("If the greatest index in the coordIndex field is N, then there shall be N+1 colors in the Color node.");

                var colors = new C4b[indices.Count];
                for (int i = 0; i < colors.Length; i++)
                    colors[i] = new C4b(colorList[indexedPerVertexColors ? vertexColorIndices[i] : indices[i]]);
                result.LineVertexAttributes[PolyLineSet.Property.Colors] = colors;
            }
            else
            {
                // one color per polyline, addressed by the ordinal within coordIndex (through colorIndex if present)
                if (colorIndex != null && colorIndex.Count < polyLineOrdinal)
                    throw Violation("IndexedLineSet node: there shall be at least as many indices in the colorIndex field as there are polylines in the IndexedLineSet.");
                if (colorIndex == null && colorList.Count < polyLineOrdinal)
                    throw Violation("There shall be at least as many colors in the Color node as there are polylines.");

                var colors = polyLineSources.Select(o => new C4b(colorList[colorIndex != null ? colorIndex[o] : o])).ToArray();
                result.LineAttributes[PolyLineSet.Property.Colors] = colors;
            }

            return result;
        }
    }
}
