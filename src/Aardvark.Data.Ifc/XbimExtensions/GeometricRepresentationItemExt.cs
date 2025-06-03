using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using Aardvark.Base;

using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.MeasureResource;

namespace Aardvark.Data.Ifc
{ 
    public static class GeometricRepresentationItemExt
    {
        public static IIfcGeometricRepresentationContext GetGeometricRepresentationContextPlan(this IModel model)
            => model.Instances.OfType<IIfcGeometricRepresentationContext>().Where(c => c.ContextType == "Plan").First();

        public static IIfcGeometricRepresentationContext GetGeometricRepresentationContextModel(this IModel model)
            => model.Instances.OfType<IIfcGeometricRepresentationContext>().Where(c => c.ContextType == "Model").First();

        #region CartesianPoint
        public static void Set(this IIfcCartesianPoint point, V2d vec) 
            => point.SetXY(vec.X, vec.Y);

        public static void Set(this IIfcCartesianPoint point, V3d vec) 
            => point.SetXYZ(vec.X, vec.Y, vec.Z);

        public static V2d ToV2d(this IIfcCartesianPoint point)
            => new(point.X, point.Y);
        public static V3d ToV3d(this IIfcCartesianPoint point)
            => new(point.X, point.Y, double.IsNaN(point.Z) ? 0 : point.Z);

        public static IIfcCartesianPoint CreatePoint(this IModel model, V2d point)
            => model.Factory().CartesianPoint(c => c.Set(point));

        public static IIfcCartesianPoint CreatePoint(this IModel model, V3d point)
            => model.Factory().CartesianPoint(c => c.Set(point));
        #endregion

        #region Direction
        public static void Set(this IIfcDirection dir, V2d d) 
            => dir.SetXY(d.X, d.Y);

        public static void Set(this IIfcDirection dir, V3d d) 
            => dir.SetXYZ(d.X, d.Y, d.Z);

        public static V2d ToV2d(this IIfcDirection direction)
            => new(direction.X, direction.Y);

        public static V3d ToV3d(this IIfcDirection direction)
            => new(direction.X, direction.Y, double.IsNaN(direction.Z) ? 0 : direction.Z);

        public static IIfcDirection CreateDirection(this IModel model, V2d direction)
            => model.Factory().Direction(rd => rd.Set(direction)); // NOTE: Direction may be normalized!

        public static IIfcDirection CreateDirection(this IModel model, V3d direction)
            => model.Factory().Direction(rd => rd.Set(direction)); // NOTE: Direction may be normalized!
        #endregion

        #region Vector
        public static V2d ToV2d(this IIfcVector vec)
            => vec.Orientation.ToV2d() * vec.Magnitude;

        public static V3d ToV3d(this IIfcVector vec)
            => vec.Orientation.ToV3d() * vec.Magnitude;

        public static IIfcVector CreateVector(this IModel model, V2d vector)
        {
            return model.Factory().Vector(v =>
            {
                v.Magnitude = vector.Length;
                v.Orientation = model.CreateDirection(vector.Normalized);
            });
        }
        public static IIfcVector CreateVector(this IModel model, V3d vector)
        {
            return model.Factory().Vector(v =>
            {
                v.Magnitude = vector.Length;
                v.Orientation = model.CreateDirection(vector.Normalized);
            });
        }
        #endregion

        #region Axis2Placement3D
        public static Trafo3d ToTrafo3d(this IIfcAxis2Placement3D p)
        {
            if (p.RefDirection == null || p.Axis == null) return Trafo3d.Translation(p.Location.ToV3d());
            
            var xAxis = p.RefDirection == null ? V3d.XAxis : p.RefDirection.ToV3d();
            var zAxis = p.Axis == null ? V3d.ZAxis : p.Axis.ToV3d();
            var yAxis = zAxis.Cross(xAxis);
            return Trafo3d.FromBasis(xAxis, yAxis, zAxis, p.Location.ToV3d());
        }

        public static Trafo3d ToTrafo3d(this IIfcAxis2Placement3D p, ConcurrentDictionary<int, object> maps = null)
        {
            if (maps == null)
                return p.ToTrafo3d();

            if (maps.TryGetValue(p.EntityLabel, out object transform)) //already converted it just return cached
                return (Trafo3d)transform;

            transform = p.ToTrafo3d();
            maps.TryAdd(p.EntityLabel, transform);
            return (Trafo3d)transform;
        }

        public static IIfcAxis2Placement3D CreateAxis2Placement3D(this IModel model, V3d location, V3d refDir, V3d axis)
        {
            return model.Factory().Axis2Placement3D(a =>
            {
                a.Location = model.CreatePoint(location);
                a.RefDirection = model.CreateDirection(refDir); // default x-axis
                a.Axis = model.CreateDirection(axis);           // default z-axis
            });
        }

        public static IIfcAxis2Placement3D CreateAxis2Placement3D(this IModel model, Trafo3d trafo)
            => model.CreateAxis2Placement3D(trafo.Forward.C3.XYZ, trafo.Forward.C0.XYZ, trafo.Forward.C2.XYZ);

        public static IIfcAxis2Placement3D CreateAxis2Placement3D(this IModel model, V3d location)
            => model.CreateAxis2Placement3D(location, V3d.XAxis, V3d.ZAxis);

        public static Trafo2d ToTrafo2D(this IIfcAxis2Placement2D obj, ConcurrentDictionary<int, object> maps = null)
        {
            if (maps != null && maps.TryGetValue(obj.EntityLabel, out object transform)) //already converted it just return cached
                return (Trafo2d)transform;

            if (obj.RefDirection != null)
            {
                var dir = obj.RefDirection.ToV2d();
                transform = Trafo2d.FromBasis(dir.XY, dir.YX, obj.Location.ToV2d());
            }
            else
                transform = Trafo2d.Translation(obj.Location.ToV2d());

            maps?.TryAdd(obj.EntityLabel, transform);
            return (Trafo2d)transform;
        }

        public static IIfcAxis2Placement2D CreateAxis2Placement2D(this IModel model, V2d location, V2d refDir)
        {
            return model.Factory().Axis2Placement2D(a =>
            {
                a.Location = model.CreatePoint(location);
                a.RefDirection = model.CreateDirection(refDir);
            });
        }
        public static IIfcAxis2Placement2D CreateAxis2Placement2D(this IModel model, V2d location)
            => model.CreateAxis2Placement2D(location, V2d.XAxis);

        public static Trafo3d ToTrafo3d(this Trafo2d t)
        {
            var x = new V3d(t.Forward.M00, t.Forward.M10, 0.0);
            var y = new V3d(t.Forward.M01, t.Forward.M11, 0.0);
            var z = x.Cross(y);
            var p = new V3d(t.Forward.M02, t.Forward.M12, 0.0);
            return Trafo3d.FromBasis(x,y,z,p);
        }

        public static Trafo3d ToTrafo3d(this IIfcAxis2Placement placement)
        {
            return placement switch
            {
                IIfcAxis2Placement3D ax3 => ax3.ToTrafo3d(),
                IIfcAxis2Placement2D ax2 => ax2.ToTrafo2D().ToTrafo3d(),
                _ => Trafo3d.Identity
            };
        }

        public static IIfcLocalPlacement CreateLocalPlacement(this IModel model, V3d shift)
            => model.Factory().LocalPlacement(p => p.RelativePlacement = model.CreateAxis2Placement3D(shift));
        #endregion

        #region Line and Curve
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

        public static IIfcCartesianPointList2D CreateCartesianPointList2D(this IModel model, params V2d[] points)
        {
            return model.CartesianPointList2DFactory(pl =>
            {
                for (int i = 0; i < points.Length; i++)
                {
                    pl.CoordList.GetAt(i).AddRange(points[i].ToArray().Select(v => new IfcLengthMeasure(v)));
                }
            });
        }

        public static IIfcCartesianPointList3D CreateCartesianPointList3D(this IModel model, params V3d[] points)
        {
            return model.CartesianPointList3DFactory(pl =>
            {
                for (int i = 0; i < points.Length; i++)
                {
                    pl.CoordList.GetAt(i).AddRange(points[i].ToArray().Select(v => new IfcLengthMeasure(v)));
                }
            });
        }

        public static IIfcLine CreateLine(this IModel model, V2d start, V2d end)
        {
            var diff = end - start;

            return model.Factory().Line(line =>
            {
                line.Pnt = model.CreatePoint(start);
                line.Dir = model.CreateVector(diff);
            });
        }

        public static IIfcLine CreateLine(this IModel model, V3d start, V3d end)
        {
            var diff = end - start;

            return model.Factory().Line(line =>
            {
                line.Pnt = model.CreatePoint(start);
                line.Dir = model.CreateVector(diff);
            });
        }

        public static IIfcLine CreateLine(this IModel model, Line2d line)
            => model.CreateLine(line.P0, line.P1);

        public static IIfcLine CreateLine(this IModel model, Line3d line)
            => model.CreateLine(line.P0, line.P1);

        public static IIfcPolyline CreatePolyLine(this IModel model, params V2d[] points)
        {
            return model.Factory().Polyline(line =>
            {
                line.Points.AddRange(points.Select(x => model.CreatePoint(x)));
            });
        }

        public static IIfcPolyline CreatePolyLine(this IModel model, params V3d[] points)
        {
            return model.Factory().Polyline(line =>
            {
                line.Points.AddRange(points.Select(x => model.CreatePoint(x)));
            });
        }

        public static IIfcPolyline CreatePolyLine(this IModel model, IEnumerable<V2d> points)
            => model.CreatePolyLine(points.ToArray());

        public static IIfcPolyline CreatePolyLine(this IModel model, IEnumerable<V3d> points)
            => model.CreatePolyLine(points.ToArray());

        public static IIfcIndexedPolyCurve CreateIndexedPolyCurve(this IModel model, IEnumerable<V2d> points, IEnumerable<int[]> indices = null)
        {
            // only available in IFC4+
            // NOTE: Indices start with 1!
            return model.IndexedPolyCurveFactory(poly =>
            {
                poly.Points = model.CreateCartesianPointList2D(points.ToArray());

                if (indices != null)
                {
                    var index = indices.Select(i =>
                    {
                        if (i.Length == 3) return (IfcSegmentIndexSelect)new IfcArcIndex(i.Select(x => new IfcPositiveInteger(x)).ToList());
                        else if (i.Length == 2) return (IfcSegmentIndexSelect)new IfcLineIndex(i.Select(x => new IfcPositiveInteger(x)).ToList());
                        else return null;
                    });
                    poly.Segments.AddRange(index);
                }
            });
        }

        #endregion

        #region Surfaces

        public static IIfcPlane CreatePlane(this IModel model, Plane3d plane)
        {
            var refDir = (plane.Normal.MajorDim == 2) ? plane.Normal.ZXY : plane.Normal.YXZ;
            return model.Factory().Plane(pl => pl.Position = model.CreateAxis2Placement3D(plane.Point, refDir, plane.Normal));
        }

        public static IIfcCurveBoundedPlane CreateCurveBoundedPlane(this IModel model, Plane3d plane, Polygon2d poly)
        {
            return model.Factory().CurveBoundedPlane(p =>
            {
                p.BasisSurface = model.CreatePlane(plane);
                p.OuterBoundary = model.CreatePolyLine(poly.Points);
            });
        }

        #endregion

        #region Lights

        // https://standards.buildingsmart.org/IFC/DEV/IFC4_2/FINAL/HTML/link/lighting-geometry.htm

        public static IIfcLightSourceAmbient CreateLightSourceAmbient(this IModel model, C3d color, string name = null, double? intensity = null, double? ambientIntensity = null)
        {
            return model.Factory().LightSourceAmbient(ls =>
            {
                ls.LightColour = model.CreateColor(color);

                if (name != null) ls.Name = name;

                // Light intensity may range from 0.0 (no light emission) to 1.0 (full intensity).
                // The intensity field specifies the brightness of the direct emission from the ligth.
                if (intensity.HasValue) ls.Intensity = new IfcNormalisedRatioMeasure(intensity.Value);
                // The ambientIntensity specifies the intensity of the ambient emission from the light.
                if (ambientIntensity.HasValue) ls.AmbientIntensity = ambientIntensity.Value;
            });
        }

        public static IIfcLightSourceDirectional CreateLightSourceDirectional(this IModel model, C3d color, V3d direction, string name = null, double? intensity = null, double? ambientIntensity = null)
        {
            return model.Factory().LightSourceDirectional(ls =>
            {
                ls.LightColour = model.CreateColor(color);

                if (name != null) ls.Name = name;

                // Light intensity may range from 0.0 (no light emission) to 1.0 (full intensity).
                // The intensity field specifies the brightness of the direct emission from the ligth.
                if (intensity.HasValue) ls.Intensity = new IfcNormalisedRatioMeasure(intensity.Value);
                // The ambientIntensity specifies the intensity of the ambient emission from the light.
                if (ambientIntensity.HasValue) ls.AmbientIntensity = ambientIntensity.Value;

                // Directional properties
                ls.Orientation = model.CreateDirection(direction);
            });
        }

        public static IIfcLightSourcePositional CreateLightSourcePositional(this IModel model, C3d color, V3d position, double radius, double constantAttenuation, double distanceAttenuation, double quadricAttenuation, string name = null, double? intensity = null, double? ambientIntensity = null)
        {
            // The Point light node specifies a point light source at a 3D location in the local coordinate system.
            // A point light source emits light equally in all directions; that is, it is omnidirectional.

            return model.Factory().LightSourcePositional(ls =>
            {
                ls.LightColour = model.CreateColor(color);

                if (name != null) ls.Name = name;

                // Light intensity may range from 0.0 (no light emission) to 1.0 (full intensity).
                // The intensity field specifies the brightness of the direct emission from the ligth.
                if (intensity.HasValue) ls.Intensity = new IfcNormalisedRatioMeasure(intensity.Value);
                // The ambientIntensity specifies the intensity of the ambient emission from the light.
                if (ambientIntensity.HasValue) ls.AmbientIntensity = ambientIntensity.Value;

                // Definition from ISO/CD 10303-46:1992: The Cartesian point indicates the position of the light source. Definition from VRML97 - ISO/IEC 14772-1:1997: A Point light node illuminates geometry within radius of its location.
                ls.Position = ls.Model.CreatePoint(position);

                // The maximum distance from the light source for a surface still to be illuminated. Definition from VRML97 - ISO/IEC 14772-1:1997: A Point light node illuminates geometry within radius of its location.
                ls.Radius = new IfcPositiveLengthMeasure(radius);

                // Definition from ISO/CD 10303-46:1992: This real indicates the value of the attenuation in the lighting equation that is constant.
                ls.ConstantAttenuation = constantAttenuation;

                // Definition from ISO/CD 10303-46:1992: This real indicates the value of the attenuation in the lighting equation that proportional to the distance from the light source.
                ls.DistanceAttenuation = distanceAttenuation;

                // This real indicates the value of the attenuation in the lighting equation that proportional to the square value of the distance from the light source.
                ls.QuadricAttenuation = quadricAttenuation;
            });
        }

        public static IIfcLightSourceSpot CreateLightSourceSpot(this IModel model, C3d color, V3d position, double radius, double constantAttenuation, double distanceAttenuation, double quadricAttenuation, V3d direction, double spreadAngle, double beamWidthAngle, string name = null, double? intensity = null, double? ambientIntensity = null, double? concentrationExponent = null)
        {
            // The Point light node specifies a point light source at a 3D location in the local coordinate system.
            // A point light source emits light equally in all directions; that is, it is omnidirectional.

            return model.Factory().LightSourceSpot(ls =>
            {
                ls.LightColour = model.CreateColor(color);

                if (name != null) ls.Name = name;

                // Light intensity may range from 0.0 (no light emission) to 1.0 (full intensity).
                // The intensity field specifies the brightness of the direct emission from the ligth.
                if (intensity.HasValue) ls.Intensity = new IfcNormalisedRatioMeasure(intensity.Value);
                // The ambientIntensity specifies the intensity of the ambient emission from the light.
                if (ambientIntensity.HasValue) ls.AmbientIntensity = ambientIntensity.Value;

                // Definition from ISO/CD 10303-46:1992: The Cartesian point indicates the position of the light source. Definition from VRML97 - ISO/IEC 14772-1:1997: A Point light node illuminates geometry within radius of its location.
                ls.Position = ls.Model.CreatePoint(position);

                // The maximum distance from the light source for a surface still to be illuminated. Definition from VRML97 - ISO/IEC 14772-1:1997: A Point light node illuminates geometry within radius of its location.
                ls.Radius = new IfcPositiveLengthMeasure(radius);

                // Definition from ISO/CD 10303-46:1992: This real indicates the value of the attenuation in the lighting equation that is constant.
                ls.ConstantAttenuation = constantAttenuation;

                // Definition from ISO/CD 10303-46:1992: This real indicates the value of the attenuation in the lighting equation that proportional to the distance from the light source.
                ls.DistanceAttenuation = distanceAttenuation;

                // This real indicates the value of the attenuation in the lighting equation that proportional to the square value of the distance from the light source.
                ls.QuadricAttenuation = quadricAttenuation;

                //Definition from ISO / CD 10303 - 46:1992: This is the direction of the axis of the cone of the light source specified in the coordinate space of the representation being projected..Definition from VRML97 -ISO / IEC 14772 - 1:1997: The direction field specifies the direction vector of the light's central axis defined in the local coordinate system.	X
                ls.Orientation = ls.Model.CreateDirection(direction);

                // Definition from ISO / CD 10303 - 46:1992: This real is the exponent on the cosine of the angle between the line that starts at the position of the spot light source and is in the direction of the orientation of the spot light source and a line that starts at the position of the spot light source and goes through a point on the surface being shaded.NOTE This attribute does not exists in ISO / IEC 14772 - 1:1997.X
                if (concentrationExponent.HasValue) ls.ConcentrationExponent = concentrationExponent.Value;
                // Definition from ISO / CD 10303 - 46:1992: This planar angle measure is the angle between the line that starts at the position of the spot light source and is in the direction of the spot light source and any line on the boundary of the cone of influence. Definition from VRML97 - ISO / IEC 14772 - 1:1997: The cutOffAngle(name of spread angle in VRML) field specifies the outer bound of the solid angle.The light source does not emit light outside of this solid angle.	X
                ls.SpreadAngle = new IfcPositivePlaneAngleMeasure(spreadAngle);
                // Definition from VRML97 - ISO / IEC 14772 - 1:1997: The beamWidth field specifies an inner solid angle in which the light source emits light at uniform full intensity. The light source's emission intensity drops off from the inner solid angle (beamWidthAngle) to the outer solid angle (spreadAngle).	X
                ls.BeamWidthAngle = new IfcPositivePlaneAngleMeasure(beamWidthAngle);
            });
        }

        public readonly struct AngleAndIntensity
        {
            public double AnglesInDegree { get; }
            public double IntentsityInCandelaPerLumen { get; }

            public AngleAndIntensity(double anglesInDegree, double intentsityInCandelaPerLumen)
            {
                AnglesInDegree = anglesInDegree;
                IntentsityInCandelaPerLumen = intentsityInCandelaPerLumen;
            }
        }

        public readonly struct LightIntensityDistributionData
        {
            public double MainAngleInDegree { get; }
            public AngleAndIntensity[] SecondaryAnglesAndIntensities { get; }

            public LightIntensityDistributionData(double angleInDegree, AngleAndIntensity[] data)
            {
                MainAngleInDegree = angleInDegree;
                SecondaryAnglesAndIntensities = data;
            }
        }

        public static IIfcLightIntensityDistribution CreateLightIntensityDistribution(this IModel model, IfcLightDistributionCurveEnum distributionEnum, IEnumerable<LightIntensityDistributionData> data)
        {
            var factory = model.Factory();
            return factory.LightIntensityDistribution(d =>
            {
                // Type C is the recommended standard system. The C-Plane system equals a globe with a vertical axis. C-Angles are valid from 0° to 360°, γ-Angles are valid from 0° (south pole) to 180° (north pole).
                // Type B is sometimes used for floodlights.The B-Plane System has a horizontal axis.B - Angles are valid from - 180° to + 180° with B 0° at the bottom and B180°/ B - 180° at the top, β - Angles are valid from - 90° to + 90°.
                // Type A is basically not used.For completeness the Type A Photometry equals the Type B rotated 90° around the Z - Axis counter clockwise.
                d.LightDistributionCurve = distributionEnum;
                d.DistributionData.AddRange(data.Select(a =>
                {
                    return factory.LightDistributionData (data =>
                    {
                        // The main plane angle (A, B or C angles, according to the light distribution curve chosen).
                        data.MainPlaneAngle = new IfcPlaneAngleMeasure(a.MainAngleInDegree.RadiansFromDegrees()); // measured in radians

                        // The list of secondary plane angles (the α, β or γ angles) according to the light distribution curve chosen.
                        // NOTE: The SecondaryPlaneAngle and LuminousIntensity lists are corresponding lists.
                        data.SecondaryPlaneAngle.AddRange(a.SecondaryAnglesAndIntensities.Select(sa => new IfcPlaneAngleMeasure(sa.AnglesInDegree.RadiansFromDegrees())));

                        // The luminous intensity distribution measure for this pair of main and secondary plane angles according to the light distribution curve chosen.	
                        data.LuminousIntensity.AddRange(a.SecondaryAnglesAndIntensities.Select(m => new IfcLuminousIntensityDistributionMeasure(m.IntentsityInCandelaPerLumen))); // measured in Candela/Lumen (cd/lm) or (cd/klm).
                    });
                }));
            });
        }

        public static IIfcLightSourceGoniometric CreateLightSourceGoniometric(this IModel model, C3d color, double colourTemperature, double luminousFlux,
            IfcLightEmissionSourceEnum lightEmissionSource, IIfcLightIntensityDistribution data, IIfcAxis2Placement3D placement, C3d? appearance = null, string name = null, double? intensity = null, double? ambientIntensity = null)
        {
            return model.Factory().LightSourceGoniometric(ls =>
            {

                ls.LightColour = model.CreateColor(color);

                if (name != null) ls.Name = name;

                // Light intensity may range from 0.0 (no light emission) to 1.0 (full intensity).
                // The intensity field specifies the brightness of the direct emission from the ligth.
                if (intensity.HasValue) ls.Intensity = new IfcNormalisedRatioMeasure(intensity.Value);
                // The ambientIntensity specifies the intensity of the ambient emission from the light.
                if (ambientIntensity.HasValue) ls.AmbientIntensity = ambientIntensity.Value;

                // Goniometric Properties
                ls.Position = placement;
                if (appearance.HasValue) ls.ColourAppearance = model.CreateColor(appearance.Value);
                ls.ColourTemperature = new IfcThermodynamicTemperatureMeasure(colourTemperature);
                ls.LuminousFlux = new IfcLuminousFluxMeasure(luminousFlux);

                ls.LightEmissionSource = lightEmissionSource;

                // https://standards.buildingsmart.org/IFC/DEV/IFC4_2/FINAL/HTML/link/lighting-geometry.htm
                ls.LightDistributionDataSource = data;

            });
        }

        #endregion
    }
}