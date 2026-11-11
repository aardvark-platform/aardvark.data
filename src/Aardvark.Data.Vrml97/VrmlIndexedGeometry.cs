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
        /// Indexed LineList of the line set with the polyline colours resolved to the vertices: a vertex shared by polylines of different
        /// colours is split by PolyLineSet.WithResolvedLineAttributes, everything else stays shared. Null for a line set that draws nothing.
        /// NOTE: LineList (and not LineStrip) because a single IndexedLineSet holds multiple disjoint PolyLines and
        ///       because the thickLine shader of Aardvark.Rendering expects a line list as input topology.
        /// </summary>
        public static IndexedGeometry GetIndexedGeometry(this VrmlLineSet lineSet)
            => lineSet.LineSet?.GetIndexedGeometry(resolveLineAttributes: true);
    }
}
