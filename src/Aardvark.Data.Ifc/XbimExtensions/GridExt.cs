using Aardvark.Base;

using System.Linq;

using Xbim.Common;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.GeometricConstraintResource;
using Xbim.Ifc4.ProductExtension;

namespace Aardvark.Data.Ifc
{
    public static class GridExt
    {
        #region Grid
        public static IfcGridAxis CreateGridAxis(this IModel model, string name, V2d start, V2d end)
        {
            return model.New<IfcGridAxis>(a =>
            {
                a.AxisTag = name;
                a.AxisCurve = model.CreatePolyLine(start, end);
                a.SameSense = true;
            });
        }

        public static IfcGrid CreateGrid(this IModel model, string name, string[] xAxis, string[] vAxes, double offset)
        {
            // Create axis
            var xAxisEntities = xAxis.Select((a, i) => model.CreateGridAxis(a, new V2d(-offset / 2.0, offset * i), new V2d(offset * vAxes.Length, offset * i)));
            var yAxisEntities = vAxes.Select((a, i) => model.CreateGridAxis(a, new V2d(offset * i, -offset / 2.0), new V2d(offset * i, offset * xAxis.Length)));

            // Create regular grid
            var grid = model.New<IfcGrid>(g =>
            {
                g.Name = name;
                g.UAxes.AddRange(xAxisEntities);
                g.VAxes.AddRange(yAxisEntities);
                g.PredefinedType = IfcGridTypeEnum.RECTANGULAR;
                g.ObjectPlacement = model.CreateLocalPlacement(V3d.Zero);
            });

            return grid;
        }

        #endregion
    }
}
