using Aardvark.Base;
using Aardvark.Geometry;
using Aardvark.Rendering;
using System;

namespace Aardvark.Data.Vrml97
{
    /// <summary>
    /// Conversion of Vrml97 geometry to renderable IndexedGeometry.
    /// NOTE: this is kept out of VrmlNodeTypes.cs on purpose, it is the only place where the Vrml97 data types
    ///       meet Aardvark.Rendering -> everything else in Aardvark.Data.Vrml97 stays free of that dependency.
    /// </summary>
    public static class VrmlIndexedGeometryExtensions
    {
        /// <summary>
        /// Renderable representation of the given geometry, or null if there is nothing to render.
        /// NOTE: the primitives (Box, Sphere, Cone, Cylinder) yield null until VrmlExtensions.PrimitivesToMeshes
        ///       has replaced them by a VrmlMesh.
        /// </summary>
        public static IndexedGeometry GetIndexedGeometry(this VrmlGeometry geometry)
        {
            return geometry switch
            {
                VrmlMesh mesh => mesh.Mesh?.GetIndexedGeometry(),
                VrmlLineSet lineSet => lineSet.GetIndexedGeometry(),
                _ => null
            };
        }

        /// <summary>
        /// Builds a renderable IndexedGeometry with IndexedGeometryMode.LineList.
        /// NOTE: LineList (and not LineStrip) because a single IndexedLineSet holds multiple disjoint PolyLines and
        ///       because the thickLine shader of Aardvark.Rendering expects a line list as input topology.
        ///       The PolyLines are expanded into individual segments, so no IndexArray is needed and each vertex
        ///       can carry its own color (a vertex shared by two PolyLines may have two different colors).
        /// </summary>
        public static IndexedGeometry GetIndexedGeometry(this VrmlLineSet lineSet)
        {
            if (lineSet.PolyLineCount == 0)
            {
                return null;
            }

            var segmentCount = lineSet.SegmentCount;
            var positions = new V3f[segmentCount * 2];
            var colors = lineSet.ColorArray != null ? new C4b[segmentCount * 2] : null;

            var vi = 0;
            for (int pi = 0; pi < lineSet.PolyLineCount; pi++)
            {
                for (int i = lineSet.FirstIndexArray[pi]; i < lineSet.FirstIndexArray[pi + 1] - 1; i++)
                {
                    positions[vi] = lineSet.VertexArray[lineSet.VertexIndexArray[i]];
                    positions[vi + 1] = lineSet.VertexArray[lineSet.VertexIndexArray[i + 1]];

                    if (colors != null)
                    {
                        colors[vi] = lineSet.GetColor(pi, i);
                        colors[vi + 1] = lineSet.GetColor(pi, i + 1);
                    }

                    vi += 2;
                }
            }

            var attributes = new SymbolDict<Array>() { { DefaultSemantic.Positions, positions } };
            if (colors != null)
            {
                attributes[DefaultSemantic.Colors] = colors;
            }

            return new IndexedGeometry()
            {
                Mode = IndexedGeometryMode.LineList,
                IndexedAttributes = attributes
            };
        }
    }
}
