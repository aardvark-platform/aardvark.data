using Aardvark.Base;
using System.Collections.Generic;
using System.Linq;

using Xbim.Ifc4.GeometricModelResource;
using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.PresentationAppearanceResource;

namespace Aardvark.Data.Ifc
{
    public static class AardvarkExt
    {
        // Overrides values from Aardvark-Types
        public static void Set(this IfcCartesianPoint point, V2d vec) => point.SetXY(vec.X, vec.Y);

        public static void Set(this IfcCartesianPoint point, V3d vec) => point.SetXYZ(vec.X, vec.Y, vec.Z);

        public static void Set(this IfcDirection dir, V3d d) => dir.SetXYZ(d.X, d.Y, d.Z);

        public static void Set(this IfcDirection dir, V2d d) => dir.SetXY(d.X, d.Y);

        public static void Set(this IfcColourRgb c, C3d colour)
        {
            c.Red = colour.R;
            c.Green = colour.G;
            c.Blue = colour.B;
        }

        public static void Set(this IfcColourRgb c, C3f colour)
        {
            c.Red = colour.R;
            c.Green = colour.G;
            c.Blue = colour.B;
        }

        public static void Set(this IfcBoundingBox b, Box3d box)
        {
            b.Corner = b.Model.CreatePoint(box.Min);
            b.XDim = box.SizeX;
            b.YDim = box.SizeY;
            b.ZDim = box.SizeZ;
        }


        // Cast Ifc-Types to Aardvark-Types
        public static V3d ToV3d(this IIfcCartesianPoint point)
            => new(point.X, point.Y, point.Z);

        public static V3d ToV3d(this IIfcDirection direction)
            => new(direction.X, direction.Y, direction.Z);

        public static V3d ToV3d(this IIfcVector vec)
            => vec.Orientation.ToV3d() * vec.Magnitude;

        public static C3d ToC3d(this IIfcColourRgb col)
            => new(col.Red, col.Green, col.Blue);

        public static C3f ToC3f(this IIfcColourRgb col)
            => col.ToC3d().ToC3f();

        public static Box3d ToBox3d(this IIfcBoundingBox box)
        {
            var min = box.Corner.ToV3d();
            var max = min + new V3d(box.XDim, box.YDim, box.ZDim);
            return new Box3d(min, max);
        }

        public static IEnumerable<V3d> ToV3d(this IIfcCartesianPointList list)
        {
            return list switch
            {
                IIfcCartesianPointList2D list2D => list2D.CoordList.Select(v => new V3d(v[0], v[1], 0.0)),
                IIfcCartesianPointList3D list3D => list3D.CoordList.Select(v => new V3d(v[0], v[1], v[2])),
                _ => []
            };
        }

        public static IEnumerable<V3d> ToV3d(this IIfcCurve curve)
        {
            return curve switch
            {
                IIfcPolyline poly => poly.Points.Select(p => p.ToV3d()),
                IIfcIndexedPolyCurve indexed => indexed.Points.ToV3d(),
                IIfcCompositeCurve comp => comp.Segments.SelectMany(s => s.ParentCurve.ToV3d()),
                _ => []
            };
        }

        public static Trafo3d ToTrafo3d(this IIfcAxis2Placement3D p)
        {
            var xAxis = p.RefDirection == null ? V3d.XAxis : p.RefDirection.ToV3d();
            var zAxis = p.Axis == null ? V3d.ZAxis : p.Axis.ToV3d();
            var yAxis = zAxis.Cross(xAxis);
            return Trafo3d.FromBasis(xAxis, yAxis, zAxis, p.Location.ToV3d());
        }
    }
}
