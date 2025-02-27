
using System.Collections.Generic;
using System.Linq;
using Aardvark.Base;
using Aardvark.Geometry;

using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.MeasureResource;

namespace Aardvark.Data.Ifc
{
    public static class ShapeRepresentationExt
    {
        #region SurveyPoints (broken)
        public static IIfcShapeRepresentation CreateShapeRepSurveyPoints(this IModel model, IIfcPresentationLayerWithStyle layer, params V2d[] points)
        {
            // Set of Survey points 2D https://ifc43-docs.standards.buildingsmart.org/IFC/RELEASE/IFC4x3/HTML/concepts/Product_Shape/Product_Geometric_Representation/Annotation_Geometry/Set_Of_Survey_Points/content.html
            IIfcGeometricRepresentationItem item = model.CreateCartesianPointList2D(points).AssignLayer(layer);

            return model.Factory().ShapeRepresentation(r =>
            {
                r.RepresentationIdentifier = "Annotation";
                r.RepresentationType = "Point";
                r.Items.Add(item);
                r.ContextOfItems = model.GetGeometricRepresentationContextPlan();
            });
        }

        public static IIfcShapeRepresentation CreateShapeRepresentationSurveyPoints(this IModel model, params V2d[] points)
            => CreateShapeRepSurveyPoints(model, null, points);

        public static IIfcShapeRepresentation CreateShapeRepresentationSurveyPoints(this IModel model, IIfcPresentationLayerWithStyle layer, params V3d[] points)
        {
            // Set of Survey points 3D https://ifc43-docs.standards.buildingsmart.org/IFC/RELEASE/IFC4x3/HTML/concepts/Product_Shape/Product_Geometric_Representation/Annotation_Geometry/Set_Of_Survey_Points/content.html
            IIfcGeometricRepresentationItem item = model.CreateCartesianPointList3D(points).AssignLayer(layer);

            return model.Factory().ShapeRepresentation(r =>
            {
                r.RepresentationIdentifier = "Annotation";
                r.RepresentationType = "Point";
                r.Items.Add(item);
                r.ContextOfItems = model.GetGeometricRepresentationContextModel();
            });
        }

        public static IIfcShapeRepresentation CreateShapeRepresentationSurveyPoints(this IModel model, params V3d[] points)
            => CreateShapeRepresentationSurveyPoints(model, null, points);

        #endregion

        #region 3D Annotations
        public static IIfcShapeRepresentation CreateShapeRepresentationAnnotation3d(this IIfcRepresentationItem item)
        {
            return item.Model.Factory().ShapeRepresentation(r =>
            {
                r.RepresentationIdentifier = "Annotation";
                r.RepresentationType = "GeometricSet";
                r.Items.Add(item);
                r.ContextOfItems = item.Model.GetGeometricRepresentationContextModel();
            });
        }

        public static IIfcShapeRepresentation CreateShapeRepresentationAnnotation3dPoint(this IModel model, V3d point, IIfcPresentationLayerWithStyle layer = null)
        {
            var content = model.CreatePoint(point);
            layer?.AssignedItems.Add(content);

            return content.CreateShapeRepresentationAnnotation3d();
        }

        public static IIfcShapeRepresentation CreateShapeRepresentationAnnotation3dCurve(this IModel model, V3d[] points, IIfcPresentationLayerWithStyle layer = null)
        {
            IIfcGeometricRepresentationItem content = model.CreatePolyLine(points);
            layer?.AssignedItems.Add(content);

            return content.CreateShapeRepresentationAnnotation3d();
        }

        public static IIfcShapeRepresentation CreateShapeRepresentationAnnotation3dCross(this IModel model, V3d origin, V3d normal, double angleInDegree, double scale, IIfcPresentationLayerWithStyle layer = null)
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

            IIfcGeometricRepresentationItem content = model.CreatePolyLine(crossPoints);
            layer?.AssignedItems.Add(content);

            return content.CreateShapeRepresentationAnnotation3d();
        }

        public static IIfcShapeRepresentation CreateShapeRepresentationAnnotation3dSurface(this IModel model, Plane3d plane, Polygon2d poly, IIfcPresentationLayerWithStyle layer = null)
        {
            IIfcGeometricRepresentationItem content = model.CreateCurveBoundedPlane(plane, poly);
            layer?.AssignedItems.Add(content);

            return content.CreateShapeRepresentationAnnotation3d();
        }
        #endregion

        #region 2D Annotations
        public static IIfcShapeRepresentation CreateShapeRepresentationAnnotation2D(this IIfcRepresentationItem item)
        {
            return item.Model.Factory().ShapeRepresentation(r =>
            {
                r.RepresentationIdentifier = "Annotation";
                r.RepresentationType = "Annotation2D";
                r.Items.Add(item);
                r.ContextOfItems = item.Model.GetGeometricRepresentationContextPlan();
            });
        }

        public static IIfcShapeRepresentation CreateShapeRepresentationAnnotation2dPoint(this IModel model, V2d point, IIfcPresentationLayerWithStyle layer = null)
        {
            var item = model.CreatePoint(point);
            layer?.AssignedItems.Add(item);

            return item.CreateShapeRepresentationAnnotation2D();
        }

        public static IIfcShapeRepresentation CreateShapeRepresentationAnnotation2dCurve(this IModel model, V2d[] points, IEnumerable<int[]> indices = null, IIfcPresentationLayerWithStyle layer = null)
        {
            IIfcGeometricRepresentationItem item = model.CreateIndexedPolyCurve(points, indices);
            layer?.AssignedItems.Add(item);

            return item.CreateShapeRepresentationAnnotation2D();
        }

        public static IIfcShapeRepresentation CreateShapeRepresentationAnnotation2dText(this IModel model, string label, V2d position, IIfcPresentationLayerWithStyle layer = null)
        {
            // ONLY visible in "BIMVISION"
            IIfcGeometricRepresentationItem item = model.Factory().TextLiteral(l =>
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

        public static IIfcShapeRepresentation CreateShapeRepresentationAnnotation2dArea(this IModel model, Box2d rect, IIfcPresentationLayerWithStyle layer = null)
        {
            IIfcGeometricRepresentationItem item = model.Factory().AnnotationFillArea(l =>
            {
                l.OuterBoundary = model.CreateIndexedPolyCurve(rect.ComputeCornersCCW());
                l.InnerBoundaries.Add(model.CreateIndexedPolyCurve(rect.ShrunkBy(new V2d(0.3)).ComputeCornersCCW()));
            });
            layer?.AssignedItems.Add(item);

            return item.CreateShapeRepresentationAnnotation2D();
        }

        #endregion

        #region RepresentationMap (instancing)
        public static IIfcRepresentationMap CreateRepresentationMap(this IIfcRepresentation item)
        {
            return item.Model.Factory().RepresentationMap(map =>
            {
                map.MappingOrigin = item.Model.CreateAxis2Placement3D(V3d.Zero);
                map.MappedRepresentation = item;
            });
        }

        public static IIfcShapeRepresentation Instantiate(this IIfcRepresentationMap map, Trafo3d trafo)
        {
            var factory = map.Model.Factory();

            var item = factory.MappedItem(m =>
            {
                m.MappingSource = map;
                m.MappingTarget = factory.CartesianTransformationOperator3DnonUniform(x =>
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

            return factory.ShapeRepresentation(r =>
            {
                r.RepresentationIdentifier = "Body";
                r.RepresentationType = "MappedRepresentation";
                r.Items.Add(item);
                r.ContextOfItems = item.Model.GetGeometricRepresentationContextModel();
            });
        }
        #endregion

        #region Lights
        public static IIfcShapeRepresentation CreateShapeRepresentationLighting(this IIfcRepresentationItem item)
        {
            return item.Model.Factory().ShapeRepresentation(r =>
            {
                r.RepresentationIdentifier = "Lighting";
                r.RepresentationType = "LightSource";
                r.Items.Add(item);
                r.ContextOfItems = item.Model.GetGeometricRepresentationContextModel();
            });
        }

        public static IIfcShapeRepresentation CreateShapeRepresentationLightingAmbient(this IModel model, C3d color, string name = null, double? intensity = null, double? ambientIntensity = null, IIfcPresentationLayerAssignment layer = null)
        {

            IIfcGeometricRepresentationItem item = model.CreateLightSourceAmbient(color, name, intensity, ambientIntensity);
            layer?.AssignedItems.Add(item);

            return CreateShapeRepresentationLighting(item);
        }

        public static IIfcShapeRepresentation CreateShapeRepresentationLightingDirectional(this IModel model, C3d color, V3d direction, string name = null, double? intensity = null, double? ambientIntensity = null, IIfcPresentationLayerAssignment layer = null)
        {
            IIfcGeometricRepresentationItem item = model.CreateLightSourceDirectional(color, direction, name, intensity, ambientIntensity);
            layer?.AssignedItems.Add(item);

            return CreateShapeRepresentationLighting(item);
        }

        public static IIfcShapeRepresentation CreateShapeRepresentationLightingPositional(this IModel model, C3d color, V3d position, double radius, double constantAttenuation, double distanceAttenuation, double quadricAttenuation, string name = null, double? intensity = null, double? ambientIntensity = null, IIfcPresentationLayerAssignment layer = null)
        {
            IIfcGeometricRepresentationItem item = model.CreateLightSourcePositional(color, position, radius, constantAttenuation, distanceAttenuation, quadricAttenuation, name, intensity, ambientIntensity);
            layer?.AssignedItems.Add(item);

            return CreateShapeRepresentationLighting(item);
        }

        public static IIfcShapeRepresentation CreateShapeRepresentationLightingSpot(this IModel model, C3d color, V3d position, V3d direction, double radius, double constantAttenuation, double distanceAttenuation, double quadricAttenuation, double spreadAngle, double beamWidthAngle, string name = null, double? intensity = null, double? ambientIntensity = null, double? concentrationExponent = null, IIfcPresentationLayerAssignment layer = null)
        {
            IIfcGeometricRepresentationItem item = model.CreateLightSourceSpot(color, position, radius, constantAttenuation, distanceAttenuation, quadricAttenuation, direction, spreadAngle, beamWidthAngle, name, intensity, ambientIntensity, concentrationExponent);
            layer?.AssignedItems.Add(item);

            return CreateShapeRepresentationLighting(item);
        }

        public static IIfcShapeRepresentation CreateShapeRepresentationLightingGoniometric(this IModel model, C3d color, V3d location, double colourTemperature, double luminousFlux, IIfcLightIntensityDistribution distribution, IfcLightEmissionSourceEnum lightEmissionSource = IfcLightEmissionSourceEnum.NOTDEFINED, IIfcPresentationLayerAssignment layer = null)
        {
            var placement = model.CreateAxis2Placement3D(location);
            IIfcGeometricRepresentationItem item = model.CreateLightSourceGoniometric(color, colourTemperature, luminousFlux, lightEmissionSource, distribution, placement);
            layer?.AssignedItems.Add(item);

            return CreateShapeRepresentationLighting(item);
        }
        #endregion

        #region Box
        public static void Set(this IIfcBoundingBox b, Box3d box)
        {
            b.Corner = b.Model.CreatePoint(box.Min);
            b.XDim = box.SizeX;
            b.YDim = box.SizeY;
            b.ZDim = box.SizeZ;
        }

        public static Box3d ToBox3d(this IIfcBoundingBox box)
        {
            var min = box.Corner.ToV3d();
            var max = min + new V3d(box.XDim, box.YDim, box.ZDim);
            return new Box3d(min, max);
        }

        public static IIfcShapeRepresentation CreateShapeRepresentationBoundingBox(this IModel model, Box3d box, IIfcPresentationLayerAssignment layer = null)
        {
            var factory = model.Factory();

            IIfcGeometricRepresentationItem item = factory.BoundingBox(b => b.Set(box));
            layer?.AssignedItems.Add(item);

            return factory.ShapeRepresentation (r =>
            {
                r.RepresentationIdentifier = "Box";
                r.RepresentationType = "BoundingBox";
                r.Items.Add(item);
                r.ContextOfItems = model.GetGeometricRepresentationContextModel();
            });
        }

        public static IIfcShapeRepresentation CreateShapeRepresentationSolidBox(this IModel model, Box3d box, IIfcPresentationLayerAssignment layer = null)
        {
            var factory = model.Factory();

            // Box creation by extruding box-base along Z-Axis
            var rectProf = factory.RectangleProfileDef (p =>
            {
                p.ProfileName = "RectArea";
                p.ProfileType = IfcProfileTypeEnum.AREA;
                p.XDim = box.SizeX;
                p.YDim = box.SizeY;
                p.Position = model.CreateAxis2Placement2D(box.Min.XY);
            });

            IIfcGeometricRepresentationItem item = factory.ExtrudedAreaSolid(solid =>
            {
                solid.Position = model.CreateAxis2Placement3D(box.Min);
                solid.Depth = box.SizeZ;
                solid.ExtrudedDirection = model.CreateDirection(V3d.ZAxis);
                solid.SweptArea = rectProf;
            });
            layer?.AssignedItems.Add(item);

            return factory.ShapeRepresentation(s => {
                s.ContextOfItems = model.GetGeometricRepresentationContextModel();
                s.RepresentationType = "SweptSolid";
                s.RepresentationIdentifier = "Body";
                s.Items.Add(item);
            });
        }
        #endregion

        #region Surface
        public static IIfcShapeRepresentation CreateShapeRepresentationSurface(this IModel model, Plane3d plane, Polygon2d poly, IIfcPresentationLayerAssignment layer = null)
        {
            if (!poly.IsCcw())
            {
                poly.Reverse();
            }

            IIfcGeometricRepresentationItem item = model.CreateCurveBoundedPlane(plane, poly);
            layer?.AssignedItems.Add(item);

            // Box creation by extruding box-base along Z-Axis
            return model.Factory().ShapeRepresentation(r =>
            {
                r.RepresentationIdentifier = "Surface";
                r.RepresentationType = "Surface3D";
                r.Items.Add(item);
                r.ContextOfItems = model.GetGeometricRepresentationContextModel();
            });
        }
        #endregion

        #region PolyMesh
        
        #region FaceSets
        public static IIfcConnectedFaceSet CreateConnectedFaceSet(IModel model, PolyMesh inputMesh)
        {
            // deprecated only use for IFC2x3!
            var factory = model.Factory();

            var triangleMesh = inputMesh.TriangulatedCopy();

            return factory.ConnectedFaceSet(faceSet =>
            {
                var points = triangleMesh.PositionArray.Map(p => model.CreatePoint(p));

                var indexes = new List<int[]>();
                for (int i = 0; i < triangleMesh.FirstIndexArray.Length - 1; i++)
                {
                    var firstIndex = triangleMesh.FirstIndexArray[i];
                    var values = new int[3].SetByIndex(x => triangleMesh.VertexIndexArray[firstIndex + x]);
                    indexes.Add(values);
                }

                foreach (var t in indexes)
                {
                    // Create face loop by given boundary points
                    var polyLoop = factory.PolyLoop(poly => poly.Polygon.AddRange(t.Select(k => points[k])));

                    // Create bounds
                    var bound = factory.FaceOuterBound(bound => bound.Bound = polyLoop);

                    // Create face
                    var face = factory.Face(face => face.Bounds.Add(bound));
                    
                    // Add face to outer shell
                    faceSet.CfsFaces.Add(face);
                }
            });
        }

        private static IIfcTriangulatedFaceSet CreateTriangulatedFaceSet(IModel model, PolyMesh inputMesh)
        {
            // only available in IFC4+
            if (model.SchemaVersion == XbimSchemaVersion.Ifc2X3)
                return null;

            var triangleMesh = inputMesh.TriangulatedCopy();
            
            return model.TriangulatedFaceSetFactory(tfs =>
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

        private static IIfcPolygonalFaceSet CreatePolygonalFaceSet(IModel model, PolyMesh inputMesh)
        {
            // only available in IFC4+
            if (model.SchemaVersion == XbimSchemaVersion.Ifc2X3)
                return null;

            var faces = new List<IIfcIndexedPolygonalFace>(inputMesh.Faces.Count());

            foreach (var face in inputMesh.Faces)
            {
                faces.Add(model.IndexedPolygonalFaceFactory(f => f.CoordIndex.AddRange(face.VertexIndices.Select(v => new IfcPositiveInteger(v + 1))))); // CAUTION! Indices are 1 based in IFC!
            }

            return model.PolygonalFaceSetFactory(p =>
            {
                p.Closed = true;
                p.Coordinates = model.CreateCartesianPointList3D(inputMesh.PositionArray);
                p.Faces.AddRange(faces);
            });
        }
        #endregion

        public static IIfcShapeRepresentation CreateShapeRepresentationFaceBasedSurface(this IModel model, PolyMesh mesh, IIfcPresentationLayerAssignment layer = null)
        {
            IIfcGeometricRepresentationItem item = model.Factory().FaceBasedSurfaceModel(fbs => fbs.FbsmFaces.Add(CreateConnectedFaceSet(model, mesh)));
            layer?.AssignedItems.Add(item);

            return model.Factory().ShapeRepresentation(s => {
                s.ContextOfItems = model.GetGeometricRepresentationContextModel();
                s.RepresentationType = "SurfaceModel";
                s.RepresentationIdentifier = "Body";
                s.Items.Add(item);
            });
        }

        public static IIfcShapeRepresentation CreateShapeRepresentationTessellation(this IModel model, PolyMesh mesh, IIfcPresentationLayerAssignment layer = null, bool triangulated = true)
        {
            IIfcGeometricRepresentationItem item = triangulated ? CreateTriangulatedFaceSet(model, mesh) : CreatePolygonalFaceSet(model, mesh);
            layer?.AssignedItems.Add(item);

            return model.Factory().ShapeRepresentation(s => {
                s.ContextOfItems = model.GetGeometricRepresentationContextModel();
                s.RepresentationType = "Tessellation";
                s.RepresentationIdentifier = "Body";
                s.Items.Add(item);
            });
        }

        public static IIfcShapeRepresentation CreateShapeRepresentationCompatible(this IModel model, PolyMesh mesh, IIfcPresentationLayerAssignment layer = null, bool triangulated = true)
        {
            if (model.SchemaVersion == XbimSchemaVersion.Ifc2X3)
            {
                return model.CreateShapeRepresentationFaceBasedSurface(mesh, layer);    // fallback for Ifc2x3
            }
            else
            {
                return model.CreateShapeRepresentationTessellation(mesh, layer, triangulated);  // only supported for Ifc4 and newer
            }
        }

        #region Styled Surfaces
        public static IIfcShapeRepresentation CreateStyleForShape(this IIfcShapeRepresentation shape, PolyMesh mesh, C4d? fallbackColor, IIfcSurfaceStyle surfaceStyle = null, IIfcPresentationLayerAssignment layer = null)
        {
            // style shape (should only hold one item)
            foreach (var item in shape.Items.ToArray())
            {
                // apply specific surfaceStyle / otherwise use mesh color / apply layer style / fallback-color / otherwise no styling
                if (surfaceStyle != null)
                {
                    item.CreateStyleItem(surfaceStyle);
                }
                else if (shape.Model.TryCreateSurfaceStyle(mesh, out var meshColor))
                {
                    item.CreateStyleItem(meshColor);
                }
                else if (layer.TryCreateSurfaceStyle(out var layerStyle))
                {
                    //item.CreateStyleItem(layerStyle);
                    // styling is applied via layer -> styled item not necessary
                }
                else if (fallbackColor != null)
                {
                    // caching / re-using of default_surfaces
                    var fallbackName = "Default_Surface_" + fallbackColor.ToString();
                    var defaultSurface = shape.Model.Instances.OfType<IIfcSurfaceStyle>().Where(x => x.Name == fallbackName).FirstOrDefault();
                    var style = defaultSurface ?? shape.Model.CreateSurfaceStyle(fallbackColor.Value, fallbackName);
                    var res = item.CreateStyleItem(style);
                }
            }

            return shape;
        }

        public static IIfcShapeRepresentation CreateShapeRepresentationFaceBasedSurfaceStyled(this IModel model, PolyMesh mesh, C4d? fallbackColor, IIfcSurfaceStyle surfaceStyle = null, IIfcPresentationLayerAssignment layer = null)
        {
            var shape = model.CreateShapeRepresentationFaceBasedSurface(mesh, layer);
            return shape.CreateStyleForShape(mesh, fallbackColor, surfaceStyle, layer);
        }

        public static IIfcShapeRepresentation CreateShapeRepresentationTessellationStyled(this IModel model, PolyMesh mesh, C4d? fallbackColor, IIfcSurfaceStyle surfaceStyle = null, IIfcPresentationLayerAssignment layer = null, bool triangulated = true)
        {
            var shape = model.CreateShapeRepresentationTessellation(mesh, layer, triangulated);
            return shape.CreateStyleForShape(mesh, fallbackColor, surfaceStyle, layer);
        }

        public static IIfcShapeRepresentation CreateShapeRepresentationCompatibleStyled(this IModel model, PolyMesh mesh, C4d? fallbackColor, IIfcSurfaceStyle surfaceStyle = null, IIfcPresentationLayerAssignment layer = null, bool triangulated = true)
        {
            if (model.SchemaVersion == XbimSchemaVersion.Ifc2X3)
            {
                return model.CreateShapeRepresentationFaceBasedSurfaceStyled(mesh, fallbackColor, surfaceStyle, layer);    // fallback for Ifc2x3
            }
            else
            {
                return model.CreateShapeRepresentationTessellationStyled(mesh, fallbackColor, surfaceStyle, layer, triangulated);  // only supported for Ifc4 and newer
            }
        }
        #endregion

        #endregion
    }
}