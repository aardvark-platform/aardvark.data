using System;
using System.Collections.Generic;
using System.Linq;
using Aardvark.Base;

using Xbim.Common;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.GeometricModelResource;
using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.PresentationOrganizationResource;
using Xbim.Ifc4.RepresentationResource;
using Xbim.Ifc4.GeometricConstraintResource;

namespace Aardvark.Data.Ifc
{
    public static class GeometricRepresentationItemExt
    {
        public static IfcGeometricRepresentationContext GetGeometricRepresentationContextPlan(this IModel model)
            => model.Instances.OfType<IfcGeometricRepresentationContext>().Where(c => c.ContextType == "Plan").First();

        public static IfcGeometricRepresentationContext GetGeometricRepresentationContextModel(this IModel model)
            => model.Instances.OfType<IfcGeometricRepresentationContext>().Where(c => c.ContextType == "Model").First();

        #region CartesianPoint
        public static void Set(this IfcCartesianPoint point, V2d vec) 
            => point.SetXY(vec.X, vec.Y);

        public static void Set(this IfcCartesianPoint point, V3d vec) 
            => point.SetXYZ(vec.X, vec.Y, vec.Z);

        public static V3d ToV3d(this IIfcCartesianPoint point)
            => new(point.X, point.Y, point.Z);

        public static IfcCartesianPoint CreatePoint(this IModel model, V2d point)
            => model.New<IfcCartesianPoint>(c => c.Set(point));

        public static IfcCartesianPoint CreatePoint(this IModel model, V3d point)
            => model.New<IfcCartesianPoint>(c => c.Set(point));
        #endregion

        #region Direction
        public static void Set(this IfcDirection dir, V2d d) 
            => dir.SetXY(d.X, d.Y);

        public static void Set(this IfcDirection dir, V3d d) 
            => dir.SetXYZ(d.X, d.Y, d.Z);

        public static V2d ToV2d(this IIfcDirection direction)
            => new(direction.X, direction.Y);

        public static V3d ToV3d(this IIfcDirection direction)
            => new(direction.X, direction.Y, direction.Z);

        public static IfcDirection CreateDirection(this IModel model, V2d direction)
            => model.New<IfcDirection>(rd => rd.Set(direction)); // NOTE: Direction may be normalized!

        public static IfcDirection CreateDirection(this IModel model, V3d direction)
            => model.New<IfcDirection>(rd => rd.Set(direction)); // NOTE: Direction may be normalized!
        #endregion

        #region Vector
        public static V2d ToV2d(this IIfcVector vec)
            => vec.Orientation.ToV2d() * vec.Magnitude;

        public static V3d ToV3d(this IIfcVector vec)
            => vec.Orientation.ToV3d() * vec.Magnitude;

        public static IfcVector CreateVector(this IModel model, V2d vector)
        {
            return model.New<IfcVector>(v =>
            {
                v.Magnitude = vector.Length;
                v.Orientation = model.CreateDirection(vector.Normalized);
            });
        }
        public static IfcVector CreateVector(this IModel model, V3d vector)
        {
            return model.New<IfcVector>(v =>
            {
                v.Magnitude = vector.Length;
                v.Orientation = model.CreateDirection(vector.Normalized);
            });
        }
        #endregion

        #region Axis2Placement3D
        public static Trafo3d ToTrafo3d(this IIfcAxis2Placement3D p)
        {
            var xAxis = p.RefDirection == null ? V3d.XAxis : p.RefDirection.ToV3d();
            var zAxis = p.Axis == null ? V3d.ZAxis : p.Axis.ToV3d();
            var yAxis = zAxis.Cross(xAxis);
            return Trafo3d.FromBasis(xAxis, yAxis, zAxis, p.Location.ToV3d());
        }

        public static IfcAxis2Placement3D CreateAxis2Placement3D(this IModel model, V3d location, V3d refDir, V3d axis)
        {
            return model.New<IfcAxis2Placement3D>(a =>
            {
                a.Location = model.CreatePoint(location);
                a.RefDirection = model.CreateDirection(refDir); // default x-axis
                a.Axis = model.CreateDirection(axis);           // default z-axis
            });
        }

        public static IIfcAxis2Placement3D CreateAxis2Placement3D(this IModel model, Trafo3d trafo)
            => model.CreateAxis2Placement3D(trafo.Forward.C3.XYZ, trafo.Forward.C0.XYZ, trafo.Forward.C2.XYZ);

        public static IfcAxis2Placement3D CreateAxis2Placement3D(this IModel model, V3d location)
            => model.CreateAxis2Placement3D(location, V3d.XAxis, V3d.ZAxis);

        public static IfcAxis2Placement2D CreateAxis2Placement2D(this IModel model, V2d location, V2d refDir)
        {
            return model.New<IfcAxis2Placement2D>(a =>
            {
                a.Location = model.CreatePoint(location);
                a.RefDirection = model.CreateDirection(refDir);
            });
        }
        public static IfcAxis2Placement2D CreateAxis2Placement2D(this IModel model, V2d location)
            => model.CreateAxis2Placement2D(location, V2d.XAxis);

        public static IfcLocalPlacement CreateLocalPlacement(this IModel model, V3d shift)
            => model.New<IfcLocalPlacement>(p => p.RelativePlacement = model.CreateAxis2Placement3D(shift));
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
        public static IfcCartesianPointList2D CreateCartesianPointList2D(this IModel model, params V2d[] points)
        {
            return model.New<IfcCartesianPointList2D>(pl =>
            {
                for (int i = 0; i < points.Length; i++)
                {
                    pl.CoordList.GetAt(i).AddRange(points[i].ToArray().Select(v => new IfcLengthMeasure(v)));
                }
            });
        }

        public static IfcCartesianPointList3D CreateCartesianPointList3D(this IModel model, params V3d[] points)
        {
            return model.New<IfcCartesianPointList3D>(pl =>
            {
                for (int i = 0; i < points.Length; i++)
                {
                    pl.CoordList.GetAt(i).AddRange(points[i].ToArray().Select(v => new IfcLengthMeasure(v)));
                }
            });
        }

        public static IfcLine CreateLine(this IModel model, V2d start, V2d end)
        {
            var diff = end - start;

            return model.New<IfcLine>(line =>
            {
                line.Pnt = model.CreatePoint(start);
                line.Dir = model.CreateVector(diff);
            });
        }

        public static IfcLine CreateLine(this IModel model, V3d start, V3d end)
        {
            var diff = end - start;

            return model.New<IfcLine>(line =>
            {
                line.Pnt = model.CreatePoint(start);
                line.Dir = model.CreateVector(diff);
            });
        }

        public static IfcLine CreateLine(this IModel model, Line2d line)
            => model.CreateLine(line.P0, line.P1);

        public static IfcLine CreateLine(this IModel model, Line3d line)
            => model.CreateLine(line.P0, line.P1);

        public static IfcPolyline CreatePolyLine(this IModel model, params V2d[] points)
        {
            return model.New<IfcPolyline>(line =>
            {
                line.Points.AddRange(points.Select(x => model.CreatePoint(x)));
            });
        }

        public static IfcPolyline CreatePolyLine(this IModel model, params V3d[] points)
        {
            return model.New<IfcPolyline>(line =>
            {
                line.Points.AddRange(points.Select(x => model.CreatePoint(x)));
            });
        }

        public static IfcPolyline CreatePolyLine(this IModel model, IEnumerable<V2d> points)
            => model.CreatePolyLine(points.ToArray());

        public static IfcPolyline CreatePolyLine(this IModel model, IEnumerable<V3d> points)
            => model.CreatePolyLine(points.ToArray());

        public static IfcIndexedPolyCurve CreateIndexedPolyCurve(this IModel model, IEnumerable<V2d> points, IEnumerable<int[]> indices = null)
        {
            // NOTE: Indices start with 1!
            return model.New<IfcIndexedPolyCurve>(poly =>
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

        public static IfcPlane CreatePlane(this IModel model, Plane3d plane)
        {
            var refDir = (plane.Normal.MajorDim == 2) ? plane.Normal.ZXY : plane.Normal.YXZ;
            return model.New<IfcPlane>(pl => pl.Position = model.CreateAxis2Placement3D(plane.Point, refDir, plane.Normal));
        }

        public static IfcCurveBoundedPlane CreateCurveBoundedPlane(this IModel model, Plane3d plane, Polygon2d poly)
        {
            return model.New<IfcCurveBoundedPlane>(p =>
            {
                p.BasisSurface = model.CreatePlane(plane);
                p.OuterBoundary = model.CreatePolyLine(poly.Points);
            });
        }

        #endregion

        #region Lights

        // https://standards.buildingsmart.org/IFC/DEV/IFC4_2/FINAL/HTML/link/lighting-geometry.htm

        public static IfcLightSourceAmbient CreateLightSourceAmbient(this IModel model, C3d color, string name = null, double? intensity = null, double? ambientIntensity = null)
        {
            return model.New<IfcLightSourceAmbient>(ls =>
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

        public static IfcLightSourceDirectional CreateLightSourceDirectional(this IModel model, C3d color, V3d direction, string name = null, double? intensity = null, double? ambientIntensity = null)
        {
            return model.New<IfcLightSourceDirectional>(ls =>
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

        public static IfcLightSourcePositional CreateLightSourcePositional(this IModel model, C3d color, V3d position, double radius, double constantAttenuation, double distanceAttenuation, double quadricAttenuation, string name = null, double? intensity = null, double? ambientIntensity = null)
        {
            // The Point light node specifies a point light source at a 3D location in the local coordinate system.
            // A point light source emits light equally in all directions; that is, it is omnidirectional.

            return model.New<IfcLightSourcePositional>(ls =>
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

        public static IfcLightSourceSpot CreateLightSourceSpot(this IModel model, C3d color, V3d position, double radius, double constantAttenuation, double distanceAttenuation, double quadricAttenuation, V3d direction, double spreadAngle, double beamWidthAngle, string name = null, double? intensity = null, double? ambientIntensity = null, double? concentrationExponent = null)
        {
            // The Point light node specifies a point light source at a 3D location in the local coordinate system.
            // A point light source emits light equally in all directions; that is, it is omnidirectional.

            return model.New<IfcLightSourceSpot>(ls =>
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

        public static IfcLightIntensityDistribution CreateLightIntensityDistribution(this IModel model, IfcLightDistributionCurveEnum distributionEnum, IEnumerable<LightIntensityDistributionData> data)
        {
            return model.New<IfcLightIntensityDistribution>(d =>
            {
                // Type C is the recommended standard system. The C-Plane system equals a globe with a vertical axis. C-Angles are valid from 0° to 360°, γ-Angles are valid from 0° (south pole) to 180° (north pole).
                // Type B is sometimes used for floodlights.The B-Plane System has a horizontal axis.B - Angles are valid from - 180° to + 180° with B 0° at the bottom and B180°/ B - 180° at the top, β - Angles are valid from - 90° to + 90°.
                // Type A is basically not used.For completeness the Type A Photometry equals the Type B rotated 90° around the Z - Axis counter clockwise.
                d.LightDistributionCurve = distributionEnum;
                d.DistributionData.AddRange(data.Select(a =>
                {
                    return model.New<IfcLightDistributionData>(data =>
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

        public static IfcLightSourceGoniometric CreateLightSourceGoniometric(this IModel model, C3d color, double colourTemperature, double luminousFlux,
            IfcLightEmissionSourceEnum lightEmissionSource, IfcLightIntensityDistribution data, IfcAxis2Placement3D placement, C3d? appearance = null, string name = null, double? intensity = null, double? ambientIntensity = null)
        {
            return model.New<IfcLightSourceGoniometric>(ls =>
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