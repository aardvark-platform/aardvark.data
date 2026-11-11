using Aardvark.Base;
using Aardvark.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.Common;
using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.Interfaces;

namespace Aardvark.Data.Ifc
{
    /// <summary>
    /// Evaluates IFC curves to polylines. xBIM's Xbim3DModelContext only tessellates solids, so the curves of 'Axis', 'Annotation'
    /// and 'FootPrint' representations never reach the geometry store and are read from the entities here.
    /// Polylines, indexed poly curves (line and arc segments), composite curves and trimmed lines are supported, other curve types are reported once and skipped.
    /// </summary>
    public static class CurveExt
    {
        /// <summary>Angular step used to sample arcs, a semicircle gives 18 segments with the default.</summary>
        public static double ArcSampleAngleInDegrees { get; set; } = 10.0;

        private const double ContinuityEps = 1e-6;      // distance below which the end of one segment counts as the start of the next

        private static readonly HashSet<string> s_reported = [];

        /// <summary>
        /// The presentation styles assigned to the item itself through styled items, directly or via a style assignment.
        /// </summary>
        public static IEnumerable<IIfcPresentationStyle> GetPresentationStyles(this IIfcRepresentationItem item)
        {
            foreach (var styledItem in item.StyledByItem)
            {
                foreach (var style in styledItem.Styles)
                {
                    if (style is IIfcPresentationStyle presentationStyle)
                        yield return presentationStyle;
                    else if (style is IIfcPresentationStyleAssignment assignment)
                    {
                        foreach (var assigned in assignment.Styles.OfType<IIfcPresentationStyle>())
                            yield return assigned;
                    }
                }
            }
        }

        /// <summary>
        /// The colour of the first curve style on the item, null without one.
        /// </summary>
        public static C3b? GetCurveColor(this IIfcRepresentationItem item)
        {
            foreach (var style in item.GetPresentationStyles())
                if ((style is IIfcCurveStyle curveStyle) && (curveStyle.CurveColour is IIfcColourRgb rgb))
                    return rgb.ToC3b();
            return null;
        }

        /// <summary>
        /// The curve as polylines in its own coordinates; an indexed poly curve with non-continuous segments gives several.
        /// </summary>
        public static IEnumerable<V3d[]> ToPolyLines(this IIfcCurve curve)
        {
            var lineSet = curve.ToPolyLineSet();
            if (lineSet == null)
                return [];
            return Enumerable.Range(0, lineSet.PolyLineCount).Select(i => lineSet.GetPolyLine(i).ToArray());
        }

        /// <summary>
        /// The curve as a line set in its own coordinates, null when nothing could be evaluated.
        /// </summary>
        public static PolyLineSet ToPolyLineSet(this IIfcCurve curve)
        {
            var builder = new PolyLineSetBuilder();
            builder.AddCurve(curve, null);
            return builder.Build();
        }

        /// <summary>
        /// All curves of the representation as one line set in the coordinates of the product: curve items, the curves of
        /// geometric sets and mapped items (transformed by their mapping) are collected, everything else is ignored.
        /// Curves on one IfcCartesianPointList share their vertices. When any curve carries a curve style its colour goes to
        /// LineAttributes[Colors], one C4b per polyline, white for unstyled ones. Null when the representation holds no curve.
        /// </summary>
        public static PolyLineSet ToPolyLineSet(this IIfcShapeRepresentation representation)
        {
            var builder = new PolyLineSetBuilder();
            foreach (var item in representation.Items)
                builder.AddItem(item, null);
            return builder.Build();
        }

        /// <summary>
        /// Transformation of a mapped item: Axis1/2/3 are the directions of the target axes (world axes when missing), LocalOrigin the target origin, Scale (and Scale2/3 for a non-uniform operator) the scaling.
        /// </summary>
        public static Trafo3d ToTrafo3d(this IIfcCartesianTransformationOperator op)
        {
            var scale1 = op.Scale.HasValue ? (double)op.Scale.Value : 1.0;
            var scale2 = scale1;
            var scale3 = scale1;
            if (op is IIfcCartesianTransformationOperator3DnonUniform nonUniform3D)
            {
                scale2 = nonUniform3D.Scale2.HasValue ? (double)nonUniform3D.Scale2.Value : scale1;
                scale3 = nonUniform3D.Scale3.HasValue ? (double)nonUniform3D.Scale3.Value : scale1;
            }
            else if (op is IIfcCartesianTransformationOperator2DnonUniform nonUniform2D)
                scale2 = nonUniform2D.Scale2.HasValue ? (double)nonUniform2D.Scale2.Value : scale1;

            var x = AxisOrDefault(op.Axis1, V3d.XAxis);
            var y = AxisOrDefault(op.Axis2, V3d.YAxis);
            var cross = x.Cross(y);
            var z = AxisOrDefault((op as IIfcCartesianTransformationOperator3D)?.Axis3, cross.LengthSquared > 0 ? cross.Normalized : V3d.ZAxis);
            return Trafo3d.FromBasis(x * scale1, y * scale2, z * scale3, op.LocalOrigin.ToV3d());
        }

        private static V3d AxisOrDefault(IIfcDirection direction, V3d fallback)
            => direction != null ? direction.ToV3d().Normalized : fallback;

        private static void ReportUnsupported(IIfcCurve curve, string detail = "")
        {
            var key = curve.ExpressType.Name + detail;
            lock (s_reported)
            {
                if (!s_reported.Add(key))
                    return;
            }
            Report.Warn("[Ifc] {0}{1} is not evaluated to polylines, such curves are skipped", curve.ExpressType.Name, detail);
        }

        /// <summary>
        /// Inner sample points of the arc from p0 over p1 to p2 (both ends excluded). Collinear points give p1, so the arc degenerates to its two chords.
        /// </summary>
        private static IEnumerable<V3d> SampleArcInterior(V3d p0, V3d p1, V3d p2)
        {
            var ab = p1 - p0;
            var ac = p2 - p0;
            var normal = ab.Cross(ac);
            var normalLengthSquared = normal.LengthSquared;
            if (normalLengthSquared <= 1e-20 * ab.LengthSquared * ac.LengthSquared)
            {
                yield return p1;
                yield break;
            }

            // circumcentre of the three points, the arc runs counter-clockwise around the triangle normal, so it passes p1 before p2
            var center = p0 + (normal.Cross(ab) * ac.LengthSquared + ac.Cross(normal) * ab.LengthSquared) / (2 * normalLengthSquared);
            var u = p0 - center;
            var v = normal.Normalized.Cross(u);
            var w = p2 - center;
            var sweep = Math.Atan2(w.Dot(v), w.Dot(u));
            if (sweep <= 0)
                sweep += Constant.PiTimesTwo;

            // the small offset keeps a sweep that is an exact multiple of the step from rounding up to one extra segment
            var count = Math.Max(2, (int)Math.Ceiling(sweep / ArcSampleAngleInDegrees.RadiansFromDegrees() - 1e-9));
            for (var i = 1; i < count; i++)
            {
                var angle = sweep * i / count;
                yield return center + u * Math.Cos(angle) + v * Math.Sin(angle);
            }
        }

        /// <summary>
        /// Collects polylines over one growing vertex list; an optional transformation is applied to every vertex as it is added.
        /// </summary>
        private sealed class PolyLineSetBuilder
        {
            private readonly List<V3d> m_vertices = [];
            private readonly List<int[]> m_runs = [];
            private readonly List<C3b?> m_runColors = [];                       // parallel to m_runs, the curve style colour of the curve a run came from
            private readonly Dictionary<int, int> m_pointListOffsets = [];     // entity label of an untransformed point list -> its first vertex, so curves on one list share vertices
            private C3b? m_currentColor;                                        // colour of the curve being added, inherited from its set or mapped item when it has none

            public PolyLineSet Build()
            {
                if (m_runs.Count == 0)
                    return null;
                var lineSet = PolyLineSet.FromIndexed([.. m_vertices], m_runs);
                // the line attribute is a C4b like everywhere else in Aardvark, IFC curve colours have no alpha so the lines are opaque
                if (m_runColors.Any(c => c.HasValue))
                    lineSet.LineAttributes[PolyLineSet.Property.Colors] = m_runColors.Select(c => c.HasValue ? new C4b(c.Value) : C4b.White).ToArray();
                return lineSet;
            }

            public void AddItem(IIfcRepresentationItem item, Trafo3d? trafo, C3b? inheritedColor = null)
            {
                switch (item)
                {
                    case IIfcCurve curve:
                        AddCurve(curve, trafo, inheritedColor);
                        break;
                    case IIfcGeometricSet set:
                        var setColor = set.GetCurveColor() ?? inheritedColor;
                        foreach (var element in set.Elements)
                            if (element is IIfcCurve curve)
                                AddCurve(curve, trafo, setColor);
                        break;
                    case IIfcMappedItem mapped:
                        // the mapped representation is placed at the mapping origin and then transformed by the mapping target, like xBIM instantiates mapped solids
                        var mapping = mapped.MappingSource.MappingOrigin.ToTrafo3d() * mapped.MappingTarget.ToTrafo3d();
                        var combined = trafo.HasValue ? mapping * trafo.Value : mapping;
                        var mappedColor = mapped.GetCurveColor() ?? inheritedColor;
                        foreach (var sub in mapped.MappingSource.MappedRepresentation.Items)
                            AddItem(sub, combined, mappedColor);
                        break;
                }
            }

            public void AddCurve(IIfcCurve curve, Trafo3d? trafo, C3b? inheritedColor = null)
            {
                m_currentColor = curve.GetCurveColor() ?? inheritedColor;
                switch (curve)
                {
                    case IIfcPolyline polyline:
                        AddRun(polyline.Points.Select(p => AddVertex(p.ToV3d(), trafo)));
                        break;
                    case IIfcIndexedPolyCurve indexed:
                        AddIndexedPolyCurve(indexed, trafo);
                        break;
                    case IIfcCompositeCurve composite:
                        AddCompositeCurve(composite, trafo);
                        break;
                    case IIfcTrimmedCurve trimmed:
                        AddTrimmedCurve(trimmed, trafo);
                        break;
                    default:
                        ReportUnsupported(curve);
                        break;
                }
            }

            private void AddIndexedPolyCurve(IIfcIndexedPolyCurve curve, Trafo3d? trafo)
            {
                var points = curve.Points.ToV3d().ToArray();
                var offset = AddPointList(curve.Points.EntityLabel, points, trafo);
                if (curve.Segments.Count == 0)
                {
                    AddRun(Enumerable.Range(offset, points.Length));
                    return;
                }

                // segments are 1-based index runs into the point list, a new polyline starts where a segment does not continue at the last index
                var run = new List<int>();
                foreach (var segment in curve.Segments)
                {
                    var indices = SegmentIndices(segment);
                    if (indices == null || indices.Length < 2 || indices.Any(i => i < 0 || i >= points.Length))
                    {
                        ReportUnsupported(curve, " with an invalid segment");
                        continue;
                    }

                    if (run.Count > 0 && run[run.Count - 1] != offset + indices[0])
                    {
                        AddRun(run);
                        run = [];
                    }
                    if (run.Count == 0)
                        run.Add(offset + indices[0]);

                    if (segment is IfcArcIndex && indices.Length == 3)
                    {
                        foreach (var sample in SampleArcInterior(points[indices[0]], points[indices[1]], points[indices[2]]))
                            run.Add(AddVertex(sample, trafo));
                        run.Add(offset + indices[2]);
                    }
                    else
                    {
                        for (var i = 1; i < indices.Length; i++)
                            run.Add(offset + indices[i]);
                    }
                }
                AddRun(run);
            }

            /// <summary>
            /// The 0-based point indices of an IfcLineIndex or IfcArcIndex. Both wrap a list of IfcPositiveInteger, read as an untyped sequence so the list type of the schema does not matter.
            /// </summary>
            private static int[] SegmentIndices(object segment)
            {
                var sequence = segment switch
                {
                    IExpressValueType valueType when valueType.Value is System.Collections.IEnumerable list => list,
                    System.Collections.IEnumerable list => list,
                    _ => null,
                };
                if (sequence == null)
                    return null;

                var indices = new List<int>();
                foreach (var item in sequence)
                {
                    var value = item is IExpressValueType index ? index.Value : item;
                    indices.Add((int)Convert.ToInt64(value) - 1);
                }
                return [.. indices];
            }

            private void AddCompositeCurve(IIfcCompositeCurve composite, Trafo3d? trafo)
            {
                // segments are chained while each one starts where the previous ended, a jump starts a new polyline
                var chain = new List<V3d>();
                foreach (var segment in composite.Segments)
                {
                    foreach (var polyLine in segment.ParentCurve.ToPolyLines())
                    {
                        var points = (bool)segment.SameSense ? polyLine : polyLine.Reverse().ToArray();
                        if (chain.Count > 0 && !chain[chain.Count - 1].ApproximateEquals(points[0], ContinuityEps))
                        {
                            AddRun(chain.Select(p => AddVertex(p, trafo)));
                            chain = [];
                        }
                        chain.AddRange(chain.Count > 0 ? points.Skip(1) : points);
                    }
                }
                AddRun(chain.Select(p => AddVertex(p, trafo)));
            }

            private void AddTrimmedCurve(IIfcTrimmedCurve trimmed, Trafo3d? trafo)
            {
                if (trimmed.BasisCurve is not IIfcLine line)
                {
                    ReportUnsupported(trimmed, $" over {trimmed.BasisCurve?.ExpressType.Name}");
                    return;
                }

                V3d? start = TrimPoint(trimmed.Trim1, line);
                V3d? end = TrimPoint(trimmed.Trim2, line);
                if (!start.HasValue || !end.HasValue)
                {
                    ReportUnsupported(trimmed, " without point or parameter trims");
                    return;
                }
                AddRun([AddVertex(start.Value, trafo), AddVertex(end.Value, trafo)]);
            }

            private static V3d? TrimPoint(IEnumerable<IIfcTrimmingSelect> trims, IIfcLine line)
            {
                // the trimmed curve runs from Trim1 to Trim2 whatever the sense of the basis line, a point trim is taken as is, a parameter one is evaluated on the line
                foreach (var trim in trims)
                    if (trim is IIfcCartesianPoint point)
                        return point.ToV3d();
                foreach (var trim in trims)
                    if ((trim is IExpressValueType parameter) && (parameter.Value is double t))
                        return line.Pnt.ToV3d() + line.Dir.ToV3d() * t;    // IfcParameterValue of any schema
                return null;
            }

            private int AddVertex(V3d point, Trafo3d? trafo)
            {
                m_vertices.Add(trafo.HasValue ? trafo.Value.Forward.TransformPos(point) : point);
                return m_vertices.Count - 1;
            }

            private int AddPointList(int entityLabel, V3d[] points, Trafo3d? trafo)
            {
                // transformed copies are not shared, the same list may be mapped to several places
                if (!trafo.HasValue && m_pointListOffsets.TryGetValue(entityLabel, out var offset))
                    return offset;

                offset = m_vertices.Count;
                foreach (var point in points)
                    AddVertex(point, trafo);
                if (!trafo.HasValue)
                    m_pointListOffsets[entityLabel] = offset;
                return offset;
            }

            private void AddRun(IEnumerable<int> indices)
            {
                var run = indices.ToArray();
                if (run.Length < 2)
                    return;
                m_runs.Add(run);
                m_runColors.Add(m_currentColor);
            }
        }
    }
}
