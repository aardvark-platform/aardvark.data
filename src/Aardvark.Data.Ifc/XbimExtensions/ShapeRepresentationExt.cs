
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Aardvark.Base;
using Aardvark.Geometry;

using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.GeometricModelResource;
using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.PresentationDefinitionResource;
using Xbim.Ifc4.PresentationOrganizationResource;
using Xbim.Ifc4.ProfileResource;
using Xbim.Ifc4.RepresentationResource;

namespace Aardvark.Data.Ifc
{
    public static class ShapeRepresentationExt
    {
        #region SurveyPoints (broken)
        public static IfcShapeRepresentation CreateShapeRepSurveyPoints(this IModel model, IfcPresentationLayerWithStyle layer, params V2d[] points)
        {
            // Set of Survey points 2D https://ifc43-docs.standards.buildingsmart.org/IFC/RELEASE/IFC4x3/HTML/concepts/Product_Shape/Product_Geometric_Representation/Annotation_Geometry/Set_Of_Survey_Points/content.html
            IfcGeometricRepresentationItem item = model.CreateCartesianPointList2D(points).AssignLayer(layer);

            return model.New<IfcShapeRepresentation>(r =>
            {
                r.RepresentationIdentifier = "Annotation";
                r.RepresentationType = "Point";
                r.Items.Add(item);
                r.ContextOfItems = model.GetGeometricRepresentationContextPlan();
            });
        }

        public static IfcShapeRepresentation CreateShapeRepresentationSurveyPoints(this IModel model, params V2d[] points)
            => CreateShapeRepSurveyPoints(model, null, points);

        public static IfcShapeRepresentation CreateShapeRepresentationSurveyPoints(this IModel model, IfcPresentationLayerWithStyle layer, params V3d[] points)
        {
            // Set of Survey points 3D https://ifc43-docs.standards.buildingsmart.org/IFC/RELEASE/IFC4x3/HTML/concepts/Product_Shape/Product_Geometric_Representation/Annotation_Geometry/Set_Of_Survey_Points/content.html
            IfcGeometricRepresentationItem item = model.CreateCartesianPointList3D(points).AssignLayer(layer);

            return model.New<IfcShapeRepresentation>(r =>
            {
                r.RepresentationIdentifier = "Annotation";
                r.RepresentationType = "Point";
                r.Items.Add(item);
                r.ContextOfItems = model.GetGeometricRepresentationContextModel();
            });
        }

        public static IfcShapeRepresentation CreateShapeRepresentationSurveyPoints(this IModel model, params V3d[] points)
            => CreateShapeRepresentationSurveyPoints(model, null, points);

        #endregion

        #region 3D Annotations
        public static IfcShapeRepresentation CreateShapeRepresentationAnnotation3d(this IfcRepresentationItem item)
        {
            return item.Model.New<IfcShapeRepresentation>(r =>
            {
                r.RepresentationIdentifier = "Annotation";
                r.RepresentationType = "GeometricSet";
                r.Items.Add(item);
                r.ContextOfItems = item.Model.GetGeometricRepresentationContextModel();
            });
        }

        public static IfcShapeRepresentation CreateShapeRepresentationAnnotation3dPoint(this IModel model, V3d point, IfcPresentationLayerWithStyle layer = null)
        {
            var content = model.CreatePoint(point);
            layer?.AssignedItems.Add(content);

            return content.CreateShapeRepresentationAnnotation3d();
        }

        public static IfcShapeRepresentation CreateShapeRepresentationAnnotation3dCurve(this IModel model, V3d[] points, IfcPresentationLayerWithStyle layer = null)
        {
            IfcGeometricRepresentationItem content = model.CreatePolyLine(points);
            layer?.AssignedItems.Add(content);

            return content.CreateShapeRepresentationAnnotation3d();
        }

        public static IfcShapeRepresentation CreateShapeRepresentationAnnotation3dCross(this IModel model, V3d origin, V3d normal, double angleInDegree, double scale, IfcPresentationLayerWithStyle layer = null)
        {
            //var crossPoints = new V3d[] {
            //    origin, origin+(axis1.Normalized * scale),
            //    origin, origin-(axis1.Normalized * scale),
            //    origin, origin+(axis2.Normalized * scale),
            //    origin, origin-(axis2.Normalized * scale),
            //};
            var plane = new Plane3d(normal, 0.0);
            var d = new Rot2d(angleInDegree.RadiansFromDegrees()) * V2d.XAxis * scale;

            var crossPoints = new V3d[] {
                origin, origin+plane.Unproject( d),         //+dir.XYO,
                origin, origin+plane.Unproject(-d),         //-dir.XYO,
                origin, origin+plane.Unproject( d.YX * new V2d(-1,1)),      //+dir.YXO * new V3d(-1,1,0),
                origin, origin+plane.Unproject( d.YX * new V2d(1,-1)),      //+dir.YXO * new V3d(1,-1,0)
            };

            IfcGeometricRepresentationItem content = model.CreatePolyLine(crossPoints);
            layer?.AssignedItems.Add(content);

            return content.CreateShapeRepresentationAnnotation3d();
        }

        public static IfcShapeRepresentation CreateShapeRepresentationAnnotation3dSurface(this IModel model, Plane3d plane, Polygon2d poly, IfcPresentationLayerWithStyle layer = null)
        {
            IfcGeometricRepresentationItem content = model.CreateCurveBoundedPlane(plane, poly);
            layer?.AssignedItems.Add(content);

            return content.CreateShapeRepresentationAnnotation3d();
        }
        #endregion

        #region 2D Annotations
        public static IfcShapeRepresentation CreateShapeRepresentationAnnotation2D(this IfcRepresentationItem item)
        {
            return item.Model.New<IfcShapeRepresentation>(r =>
            {
                r.RepresentationIdentifier = "Annotation";
                r.RepresentationType = "Annotation2D";
                r.Items.Add(item);
                r.ContextOfItems = item.Model.GetGeometricRepresentationContextPlan();
            });
        }

        public static IfcShapeRepresentation CreateShapeRepresentationAnnotation2dPoint(this IModel model, V2d point, IfcPresentationLayerWithStyle layer = null)
        {
            var item = model.CreatePoint(point);
            layer?.AssignedItems.Add(item);

            return item.CreateShapeRepresentationAnnotation2D();
        }

        public static IfcShapeRepresentation CreateShapeRepresentationAnnotation2dCurve(this IModel model, V2d[] points, IEnumerable<int[]> indices = null, IfcPresentationLayerWithStyle layer = null)
        {
            IfcGeometricRepresentationItem item = model.CreateIndexedPolyCurve(points, indices);
            layer?.AssignedItems.Add(item);

            return item.CreateShapeRepresentationAnnotation2D();
        }

        public static IfcShapeRepresentation CreateShapeRepresentationAnnotation2dText(this IModel model, string label, V2d position, IfcPresentationLayerWithStyle layer = null)
        {
            // ONLY visible in "BIMVISION"
            IfcGeometricRepresentationItem item = model.New<IfcTextLiteral>(l =>
            {
                // https://standards.buildingsmart.org/IFC/RELEASE/IFC4/ADD1/HTML/schema/ifcpresentationdefinitionresource/lexical/ifctextliteral.htm
                l.Path = IfcTextPath.RIGHT;
                l.Literal = label;
                //// Attributes for <IfcTextLiteralWithExtent>
                //l.BoxAlignment = new IfcBoxAlignment("center");
                //l.Extent = model.New<IfcPlanarExtent>(e =>
                //{
                //    e.SizeInX = new IfcLengthMeasure(300.0);
                //    e.SizeInY = new IfcLengthMeasure(200.0);
                //});
                l.Placement = model.CreateAxis2Placement2D(position);
            });
            layer?.AssignedItems.Add(item);

            return item.CreateShapeRepresentationAnnotation2D();
        }

        public static IfcShapeRepresentation CreateShapeRepresentationAnnotation2dArea(this IModel model, Box2d rect, IfcPresentationLayerWithStyle layer = null)
        {
            IfcGeometricRepresentationItem item = model.New<IfcAnnotationFillArea>(l =>
            {
                l.OuterBoundary = model.CreateIndexedPolyCurve(rect.ComputeCornersCCW());
                l.InnerBoundaries.Add(model.CreateIndexedPolyCurve(rect.ShrunkBy(new V2d(0.3)).ComputeCornersCCW()));
            });
            layer?.AssignedItems.Add(item);

            return item.CreateShapeRepresentationAnnotation2D();
        }

        #endregion

        #region RepresentationMap (instancing)
        public static IfcRepresentationMap CreateRepresentationMap(this IfcRepresentation item)
        {
            return item.Model.New<IfcRepresentationMap>(map =>
            {
                map.MappingOrigin = item.Model.CreateAxis2Placement3D(V3d.Zero);
                map.MappedRepresentation = item;
            });
        }

        public static IfcShapeRepresentation Instantiate(this IfcRepresentationMap map, Trafo3d trafo)
        {
            var item = map.Model.New<IfcMappedItem>(m =>
            {
                m.MappingSource = map;
                m.MappingTarget = map.Model.New<IfcCartesianTransformationOperator3DnonUniform>(x =>
                {
                    var scale = trafo.GetScaleVector();
                    x.Axis1 = map.Model.CreateDirection(trafo.Forward.C0.XYZ.Normalized);  // X - Axis
                    x.Axis2 = map.Model.CreateDirection(trafo.Forward.C1.XYZ.Normalized);  // Y - Axis
                    x.Axis3 = map.Model.CreateDirection(trafo.Forward.C2.XYZ.Normalized);  // Z - Axis
                    x.LocalOrigin = map.Model.CreatePoint(trafo.Forward.C3.XYZ);
                    x.Scale = scale.X;
                    x.Scale2 = scale.Y;
                    x.Scale3 = scale.Z;
                });
            });

            return item.Model.New<IfcShapeRepresentation>(r =>
            {
                r.RepresentationIdentifier = "Body";
                r.RepresentationType = "MappedRepresentation";
                r.Items.Add(item);
                r.ContextOfItems = item.Model.GetGeometricRepresentationContextModel();
            });
        }
        #endregion

        #region Lights
        public static IfcShapeRepresentation CreateShapeRepresentationLighting(this IfcRepresentationItem item)
        {
            return item.Model.New<IfcShapeRepresentation>(r =>
            {
                r.RepresentationIdentifier = "Lighting";
                r.RepresentationType = "LightSource";
                r.Items.Add(item);
                r.ContextOfItems = item.Model.GetGeometricRepresentationContextModel();
            });
        }

        public static IfcShapeRepresentation CreateShapeRepresentationLightingAmbient(this IModel model, C3d color, string name = null, double? intensity = null, double? ambientIntensity = null, IfcPresentationLayerAssignment layer = null)
        {

            IfcGeometricRepresentationItem item = model.CreateLightSourceAmbient(color, name, intensity, ambientIntensity);
            layer?.AssignedItems.Add(item);

            return CreateShapeRepresentationLighting(item);
        }

        public static IfcShapeRepresentation CreateShapeRepresentationLightingDirectional(this IModel model, C3d color, V3d direction, string name = null, double? intensity = null, double? ambientIntensity = null, IfcPresentationLayerAssignment layer = null)
        {
            IfcGeometricRepresentationItem item = model.CreateLightSourceDirectional(color, direction, name, intensity, ambientIntensity);
            layer?.AssignedItems.Add(item);

            return CreateShapeRepresentationLighting(item);
        }

        public static IfcShapeRepresentation CreateShapeRepresentationLightingPositional(this IModel model, C3d color, V3d position, double radius, double constantAttenuation, double distanceAttenuation, double quadricAttenuation, string name = null, double? intensity = null, double? ambientIntensity = null, IfcPresentationLayerAssignment layer = null)
        {
            IfcGeometricRepresentationItem item = model.CreateLightSourcePositional(color, position, radius, constantAttenuation, distanceAttenuation, quadricAttenuation, name, intensity, ambientIntensity);
            layer?.AssignedItems.Add(item);

            return CreateShapeRepresentationLighting(item);
        }

        public static IfcShapeRepresentation CreateShapeRepresentationLightingSpot(this IModel model, C3d color, V3d position, V3d direction, double radius, double constantAttenuation, double distanceAttenuation, double quadricAttenuation, double spreadAngle, double beamWidthAngle, string name = null, double? intensity = null, double? ambientIntensity = null, double? concentrationExponent = null, IfcPresentationLayerAssignment layer = null)
        {
            IfcGeometricRepresentationItem item = model.CreateLightSourceSpot(color, position, radius, constantAttenuation, distanceAttenuation, quadricAttenuation, direction, spreadAngle, beamWidthAngle, name, intensity, ambientIntensity, concentrationExponent);
            layer?.AssignedItems.Add(item);

            return CreateShapeRepresentationLighting(item);
        }

        public static IfcShapeRepresentation CreateShapeRepresentationLightingGoniometric(this IModel model, C3d color, V3d location, double colourTemperature, double luminousFlux, IfcLightIntensityDistribution distribution, IfcLightEmissionSourceEnum lightEmissionSource = IfcLightEmissionSourceEnum.NOTDEFINED, IfcPresentationLayerAssignment layer = null)
        {
            var placement = model.CreateAxis2Placement3D(location);
            IfcGeometricRepresentationItem item = model.CreateLightSourceGoniometric(color, colourTemperature, luminousFlux, lightEmissionSource, distribution, placement);
            layer?.AssignedItems.Add(item);

            return CreateShapeRepresentationLighting(item);
        }
        #endregion

        #region Box
        public static IfcShapeRepresentation CreateShapeRepresentationBoundingBox(this IModel model, Box3d box, IfcPresentationLayerAssignment layer = null)
        {
            IfcGeometricRepresentationItem item = model.New<IfcBoundingBox>(b => b.Set(box));
            layer?.AssignedItems.Add(item);

            return model.New<IfcShapeRepresentation>(r =>
            {
                r.RepresentationIdentifier = "Box";
                r.RepresentationType = "BoundingBox";
                r.Items.Add(item);
                r.ContextOfItems = model.GetGeometricRepresentationContextModel();
            });
        }

        public static IfcShapeRepresentation CreateShapeRepresentationSolidBox(this IModel model, Box3d box, IfcPresentationLayerAssignment layer = null)
        {
            // Box creation by extruding box-base along Z-Axis
            var rectProf = model.New<IfcRectangleProfileDef>(p =>
            {
                p.ProfileName = "RectArea";
                p.ProfileType = IfcProfileTypeEnum.AREA;
                p.XDim = box.SizeX;
                p.YDim = box.SizeY;
                //p.Position = model.CreateAxis2Placement2D(V2d.Zero);
            });

            IfcGeometricRepresentationItem item = model.New<IfcExtrudedAreaSolid>(solid =>
            {
                solid.Position = model.CreateAxis2Placement3D(box.Min);
                solid.Depth = box.SizeZ;
                solid.ExtrudedDirection = model.CreateDirection(V3d.ZAxis);
                solid.SweptArea = rectProf;
            });
            layer?.AssignedItems.Add(item);

            return model.New<IfcShapeRepresentation>(s => {
                s.ContextOfItems = model.GetGeometricRepresentationContextModel();
                s.RepresentationType = "SweptSolid";
                s.RepresentationIdentifier = "Body";
                s.Items.Add(item);
            });
        }
        #endregion

        #region Surface
        public static IfcShapeRepresentation CreateShapeRepresentationSurface(this IModel model, Plane3d plane, Polygon2d poly, IfcPresentationLayerAssignment layer = null)
        {
            if (!poly.IsCcw())
            {
                poly.Reverse();
            }

            IfcGeometricRepresentationItem item = model.CreateCurveBoundedPlane(plane, poly);
            layer?.AssignedItems.Add(item);

            // Box creation by extruding box-base along Z-Axis
            return model.New<IfcShapeRepresentation>(r =>
            {
                r.RepresentationIdentifier = "Surface";
                r.RepresentationType = "Surface3D";
                r.Items.Add(item);
                r.ContextOfItems = model.GetGeometricRepresentationContextModel();
            });
        }
        #endregion

        #region PolyMesh
        private static IfcTriangulatedFaceSet CreateTriangulatedFaceSet(IModel model, PolyMesh inputMesh)
        {
            var triangleMesh = inputMesh.TriangulatedCopy();

            return model.New<IfcTriangulatedFaceSet>(tfs =>
            {
                tfs.Closed = true;
                tfs.Coordinates = model.CreateCartesianPointList3D(triangleMesh.PositionArray);

                for (int i = 0; i < triangleMesh.FirstIndexArray.Length - 1; i++)
                {
                    var firstIndex = triangleMesh.FirstIndexArray[i];
                    var values = new long[3].SetByIndex(x => triangleMesh.VertexIndexArray[firstIndex + x]).Select(v => new IfcPositiveInteger(v + 1));   // CAUTION! Indices are 1 based in IFC!
                    tfs.CoordIndex.GetAt(i).AddRange(values);
                }
            });
        }

        private static IfcPolygonalFaceSet CreatePolygonalFaceSet(IModel model, PolyMesh inputMesh)
        {
            // only available in IFC4
            if (model.SchemaVersion != XbimSchemaVersion.Ifc4)
                return null;

            var faces = new List<IfcIndexedPolygonalFace>(inputMesh.Faces.Count());

            foreach (var face in inputMesh.Faces)
            {
                faces.Add(model.New<IfcIndexedPolygonalFace>(f => f.CoordIndex.AddRange(face.VertexIndices.Select(v => new IfcPositiveInteger(v + 1))))); // CAUTION! Indices are 1 based in IFC!
            }

            return model.New<IfcPolygonalFaceSet>(p =>
            {
                p.Closed = true;
                p.Coordinates = model.CreateCartesianPointList3D(inputMesh.PositionArray);
                p.Faces.AddRange(faces);
            });
        }

        public static IfcShapeRepresentation CreateShapeRepresentationTessellation(this IModel model, PolyMesh mesh, IfcPresentationLayerAssignment layer = null, bool triangulated = true)
        {
            IfcGeometricRepresentationItem item = triangulated ? CreateTriangulatedFaceSet(model, mesh) : CreatePolygonalFaceSet(model, mesh);
            layer?.AssignedItems.Add(item);

            return model.New<IfcShapeRepresentation>(s => {
                s.ContextOfItems = model.GetGeometricRepresentationContextModel();
                s.RepresentationType = "Tessellation";
                s.RepresentationIdentifier = "Body";
                s.Items.Add(item);
            });
        }
        #endregion
    }
}