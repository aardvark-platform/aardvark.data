using Aardvark.Base;

using System;
using System.Collections.Generic;
using System.Linq;

using Xbim.Common;
using Xbim.Ifc4.Interfaces;

namespace Aardvark.Data.Ifc
{
    public static class GridExt
    {
        public static IIfcGridAxis CreateGridAxis(this IModel model, string name, V2d start, V2d end)
        {
            return model.Factory().GridAxis(a =>
            {
                a.AxisTag = name;
                a.AxisCurve = model.CreatePolyLine(start, end);
                a.SameSense = true;
            });
        }

        public static IIfcGrid CreateGrid(this IModel model, string name, string[] xAxis, string[] vAxes, double offset)
        {
            // Create axis
            var xAxisEntities = xAxis.Select((a, i) => model.CreateGridAxis(a, new V2d(-offset / 2.0, offset * i), new V2d(offset * vAxes.Length, offset * i)));
            var yAxisEntities = vAxes.Select((a, i) => model.CreateGridAxis(a, new V2d(offset * i, -offset / 2.0), new V2d(offset * i, offset * xAxis.Length)));

            // Create regular grid
            var grid = model.Factory().Grid(g =>
            {
                g.Name = name;
                g.UAxes.AddRange(xAxisEntities);
                g.VAxes.AddRange(yAxisEntities);
                g.PredefinedType = IfcGridTypeEnum.RECTANGULAR;
                g.ObjectPlacement = model.CreateLocalPlacement(V3d.Zero);
            });

            return grid;
        }

        /// <summary>
        /// Grid with U axes (lines along x at the given y positions) and V axes (lines along y at the given x positions) in the xy plane of its placement.
        /// A rotated placement gives a vertical grid, e.g. the headframe of a light tower.
        /// </summary>
        public static IIfcGrid CreateGrid(this IModel model, string name, string[] uTags, double[] uPositions, string[] vTags, double[] vPositions, IIfcObjectPlacement placement = null, double overhang = 0.0)
        {
            if ((uTags.Length != uPositions.Length) || (vTags.Length != vPositions.Length))
                throw new ArgumentException("every grid axis needs a tag and a position");

            var xMin = vPositions.Min() - overhang;
            var xMax = vPositions.Max() + overhang;
            var yMin = uPositions.Min() - overhang;
            var yMax = uPositions.Max() + overhang;

            var uAxes = uTags.Select((tag, i) => model.CreateGridAxis(tag, new V2d(xMin, uPositions[i]), new V2d(xMax, uPositions[i]))).ToArray();
            var vAxes = vTags.Select((tag, i) => model.CreateGridAxis(tag, new V2d(vPositions[i], yMin), new V2d(vPositions[i], yMax))).ToArray();

            return model.Factory().Grid(g =>
            {
                g.Name = name;
                g.UAxes.AddRange(uAxes);
                g.VAxes.AddRange(vAxes);
                g.PredefinedType = IfcGridTypeEnum.RECTANGULAR;
                g.ObjectPlacement = placement ?? model.CreateLocalPlacement(V3d.Zero);
            });
        }

        /// <summary>
        /// Adds the axis curves as 'FootPrint' representation, so viewers show the grid and it can be selected.
        /// </summary>
        public static IIfcGrid AddFootPrintRepresentation(this IIfcGrid grid)
        {
            var model = grid.Model;
            var curves = grid.UAxes.Concat(grid.VAxes).Concat(grid.WAxes).Select(a => a.AxisCurve).ToArray();
            var set = model.Factory().GeometricCurveSet(s => s.Elements.AddRange(curves));

            grid.AddRepresentation(model.Factory().ShapeRepresentation(r =>
            {
                r.RepresentationIdentifier = "FootPrint";
                r.RepresentationType = "GeometricCurveSet";
                r.Items.Add(set);
                r.ContextOfItems = model.GetGeometricRepresentationContextModel();
            }));

            return grid;
        }

        /// <summary>
        /// Intersections of the U and V axes in grid coordinates, i.e. the discrete positions things can be mounted at (U axis by U axis).
        /// Place a product there with grid.CreateRelativePlacement(position.XYO).
        /// </summary>
        public static IEnumerable<(IIfcGridAxis U, IIfcGridAxis V, V2d Position)> GetAxisIntersections(this IIfcGrid grid)
        {
            foreach (var u in grid.UAxes)
            {
                var (uStart, uDir) = u.AxisCurve.GetChord();
                foreach (var v in grid.VAxes)
                {
                    var (vStart, vDir) = v.AxisCurve.GetChord();
                    if (TryIntersectLines(uStart, uDir, vStart, vDir, out V2d position))
                        yield return (u, v, position);
                }
            }
        }

        private static (V2d Start, V2d Direction) GetChord(this IIfcCurve curve)
        {
            var points = curve.ToV3d().ToArray();
            if (points.Length < 2)
                throw new NotSupportedException($"a grid axis needs a curve with at least two points, {curve.GetType().Name} is not supported");
            return (points[0].XY, points[points.Length - 1].XY - points[0].XY);
        }

        private static bool TryIntersectLines(V2d p0, V2d d0, V2d p1, V2d d1, out V2d point)
        {
            // infinite lines, the axes may stop short of each other
            var det = d0.X * d1.Y - d0.Y * d1.X;
            if (Math.Abs(det) < 1e-12 * d0.Length * d1.Length)
            {
                point = default;
                return false;
            }

            var t = ((p1.X - p0.X) * d1.Y - (p1.Y - p0.Y) * d1.X) / det;
            point = p0 + t * d0;
            return true;
        }
    }
}
