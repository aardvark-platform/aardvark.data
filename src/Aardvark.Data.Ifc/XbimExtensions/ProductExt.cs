using System;
using System.Collections.Generic;
using System.Linq;
using Aardvark.Base;
using Aardvark.Geometry;

using Xbim.Common;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.GeometricConstraintResource;
using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.PresentationAppearanceResource;
using Xbim.Ifc4.PresentationOrganizationResource;
using Xbim.Ifc4.ProductExtension;
using Xbim.Ifc4.RepresentationResource;
using Xbim.Ifc4.SharedBldgElements;
using Xbim.Ifc4.ProfileResource;
using Xbim.Ifc4.MaterialResource;
using Xbim.Ifc4.GeometricModelResource;

namespace Aardvark.Data.Ifc
{
    public static class ProductExt
    {
        public static T CreateTypeProduct<T>(this IModel model, IEnumerable<IfcRepresentationMap> repMaps, IEnumerable<IfcPropertySetDefinition> properties, string name = "") where T : IIfcTypeProduct, IInstantiableEntity
        {
            return model.New<T>(l =>
            {
                //l.PredefinedType; // Has to be set afterwards!
                l.Name = name;
                if (!repMaps.IsEmptyOrNull()) l.RepresentationMaps.AddRange(repMaps);    // shared geometries
                if (!properties.IsEmptyOrNull()) l.HasPropertySets.AddRange(properties); // shared properties
            });
        }

        public static T LinkToType<T>(this T obj, IIfcTypeObject objType) where T : IIfcObject
        {
            obj.AddDefiningType(objType);
            return obj;
        }

        public static I Instantiate<T, I>(this T objectType, string name, IfcObjectPlacement placement, Trafo3d trafo) where T : IfcElementType where I : IIfcProduct, IInstantiableEntity
        {
            // create instance and transform all representations by global trafo
            var instance = objectType.Model.New<I>(t =>
            {
                t.Name = name;
                t.ObjectPlacement = placement;
                t.Representation = objectType.Model.New<IfcProductDefinitionShape>(r =>
                     t.Representation = objectType.Model.New<IfcProductDefinitionShape>(r =>
                    r.Representations.AddRange(objectType.RepresentationMaps.Select(m => m.Instantiate(trafo)))));
            });

            return instance.LinkToType(objectType);
        }

        public static I Instantiate<T, I>(this T objectType, string name, IfcObjectPlacement placement, Dictionary<IfcRepresentationMap, Trafo3d> trafos) where T : IfcElementType where I : IIfcProduct, IInstantiableEntity
        {
            // create instance and transform indevidual representations
            var instance = objectType.Model.New<I>(t =>
            {
                t.Name = name;
                t.ObjectPlacement = placement;
                t.Representation = objectType.Model.New<IfcProductDefinitionShape>(r =>
                    r.Representations.AddRange(objectType.RepresentationMaps.Select(m =>
                        trafos.TryGetValue(m, out Trafo3d trafo) ? m.Instantiate(trafo) : m.Instantiate(Trafo3d.Identity))));
            });

            return instance.LinkToType(objectType);
        }


        public static IfcBuildingElementProxyType CreateProxyType(this IModel model, IfcBuildingElementProxyTypeEnum proxyType, IEnumerable<IfcRepresentationMap> repMaps, IEnumerable<IfcPropertySetDefinition> properties, string name = "")
        {
            var asdf = new EntityCreator(model);
            asdf.Wall(w => w.Name = "");

            var proxy = CreateTypeProduct<IfcBuildingElementProxyType>(model, repMaps, properties, name);
            proxy.PredefinedType = proxyType;
            return proxy;
        }

        public static T CreateElement<T>(this IModel model, string elementName, IfcObjectPlacement placement, PolyMesh mesh, IfcSurfaceStyle surfaceStyle = null, IfcPresentationLayerAssignment layer = null, bool triangulated = true) where T : IfcProduct, IInstantiableEntity
        {
            // create a Definition shape to hold the geometry
            var shape = model.CreateShapeRepresentationTessellation(mesh, layer, triangulated);
            IfcRepresentationItem repItem = shape.Items.First();

            // apply specific surfaceStyle / otherwise use mesh color / apply surface style / fallback-color
            IfcSurfaceStyle surf;

            if (surfaceStyle != null)
            {
                surf = surfaceStyle;
            }
            else if (mesh.HasColors)
            {
                // TODO: add mesh-color cach (could have implications on other re-used objects)
                var col = ((C4b)mesh.VertexAttributes.Get(PolyMesh.Property.Colors).GetValue(0)).ToC4d();
                surf = model.CreateSurfaceStyle(col.RGB, (1.0 - col.A).Clamp(0, 1), "MeshColor");
            }
            else if (layer is IfcPresentationLayerWithStyle a && a.LayerStyles.OfType<IfcSurfaceStyle>().FirstOrDefault() != null)
            {
                surf = a.LayerStyles.OfType<IfcSurfaceStyle>().First();
            }
            else
            {
                // caching / re-using of default_surfaces
                var defaultSurface = model.Instances.OfType<IfcSurfaceStyle>().Where(x => x.Name == "Default_Surface").FirstOrDefault();
                surf = defaultSurface ?? model.CreateSurfaceStyle(C3d.Red, 0.0, "Default_Surface");
            }

            // create visual style (works with 3d-geometry - body)
            repItem.CreateStyleItem(surf);

            var proxy = model.New<T>(c => {
                c.Name = elementName;

                // create a Product Definition and add the model geometry to the cube
                c.Representation = model.New<IfcProductDefinitionShape>(r => r.Representations.Add(shape));

                // now place the object into the model
                c.ObjectPlacement = placement;
            });

            return proxy;
        }

        public static T CreateElement<T>(this IModel model, string elementName, V3d position, PolyMesh mesh, IfcSurfaceStyle surfaceStyle = null, IfcPresentationLayerAssignment layer = null, bool triangulated = true) where T : IfcProduct, IInstantiableEntity
        {
            var placement = model.CreateLocalPlacement(position);
            return model.CreateElement<T>(elementName, placement, mesh, surfaceStyle, layer, triangulated);
        }

        public static T CreateAttachElement<T>(this IfcSpatialStructureElement parent, string elementName, IfcObjectPlacement placement, PolyMesh mesh, IfcSurfaceStyle surfaceStyle = null, IfcPresentationLayerAssignment layer = null, bool triangulated = true) where T : IfcProduct, IInstantiableEntity
        {
            var element = parent.Model.CreateElement<T>(elementName, placement, mesh, surfaceStyle, layer, triangulated);
            parent.AddElement(element);
            return element;
        }

        public static T CreateAttachElement<T>(this IfcSpatialStructureElement parent, string elementName, V3d position, PolyMesh mesh, IfcSurfaceStyle surfaceStyle = null, IfcPresentationLayerAssignment layer = null, bool triangulated = true) where T : IfcProduct, IInstantiableEntity
        {
            var element = parent.Model.CreateElement<T>(elementName, position, mesh, surfaceStyle, layer, triangulated);
            parent.AddElement(element);
            return element;
        }

        public static IfcSlab CreateAttachSlab(this IfcSpatialStructureElement parent, string elementName, IfcPresentationLayerAssignment layer, IfcMaterial material)
        {
            var model = parent.Model;

            var box = new Box3d(V3d.Zero, new V3d(100.0, 100.0, 300.0));

            var shape = model.New<IfcShapeRepresentation>(s => {

                var rectProf = model.New<IfcRectangleProfileDef>(p =>
                {
                    p.ProfileName = "RectArea";
                    p.ProfileType = IfcProfileTypeEnum.AREA;
                    p.XDim = box.SizeX;
                    p.YDim = box.SizeY;
                });

                IfcGeometricRepresentationItem item = model.New<IfcExtrudedAreaSolid>(solid =>
                {
                    solid.Position = parent.Model.CreateAxis2Placement3D(box.Min);
                    solid.Depth = box.SizeZ;    // CAUTION: this must be the layer-thickness
                    solid.ExtrudedDirection = parent.Model.CreateDirection(V3d.ZAxis); // CAUTION: this must be the layer-orientation
                    solid.SweptArea = rectProf;
                });
                layer?.AssignedItems.Add(item);

                s.ContextOfItems = model.Instances.OfType<IfcGeometricRepresentationContext>().First();
                s.RepresentationType = "SweptSolid";
                s.RepresentationIdentifier = "Body";
                s.Items.Add(item);
            });

            var slab = model.New<IfcSlab>(c => {
                c.Name = elementName;
                c.Representation = model.New<IfcProductDefinitionShape>(r => r.Representations.Add(shape));
                c.ObjectPlacement = parent.Model.CreateLocalPlacement(new V3d(500, 500, 500));
            });
            parent.AddElement(slab);

            // Link Material via RelAssociatesMaterial
            model.New<IfcRelAssociatesMaterial>(mat =>
            {
                // Material Layer Set Usage (HAS TO BE MANUALLY SYNCHED!)
                IfcMaterialLayerSetUsage usage = model.New<IfcMaterialLayerSetUsage>(u =>
                {
                    u.DirectionSense = IfcDirectionSenseEnum.NEGATIVE;
                    u.LayerSetDirection = IfcLayerSetDirectionEnum.AXIS3;
                    u.OffsetFromReferenceLine = 0;
                    u.ForLayerSet = model.New<IfcMaterialLayerSet>(set =>
                    {
                        set.LayerSetName = "Concrete Layer Set";
                        set.MaterialLayers.Add(model.New<IfcMaterialLayer>(layer =>
                        {
                            layer.Name = "Layer1";
                            layer.Material = material;
                            layer.LayerThickness = box.SizeZ;
                            layer.IsVentilated = false;
                            layer.Category = "Core";
                        }));
                    });
                });

                mat.Name = "RelMat";
                mat.RelatingMaterial = usage;
                mat.RelatedObjects.Add(slab);
            });

            return slab;
        }

        public static IfcAnnotation CreateAnnotation(this IModel model, string text, IfcObjectPlacement placement, V3d position, IfcPresentationLayerWithStyle layer)
        {
            // Anotation-Experiments https://ifc43-docs.standards.buildingsmart.org/IFC/RELEASE/IFC4x3/HTML/lexical/IfcAnnotation.htm
            return model.New<IfcAnnotation>(a =>
            {
                var box = new Box3d(V3d.Zero, new V3d(200, 100, 500)); // mm

                a.Name = "Intersection of " + text;
                a.ObjectPlacement = placement;
                a.Representation = model.New<IfcProductDefinitionShape>(r => {
                    r.Representations.AddRange([
                        model.CreateShapeRepresentationAnnotation2dText(text, position.XY, layer),
                        model.CreateShapeRepresentationAnnotation2dCurve([position.XY, (position.XY + new V2d(500, 750.0)), (position.XY + new V2d(1000,1000))], [[1,2,3]], layer),
                        model.CreateShapeRepresentationAnnotation3dCurve([position, (position + new V3d(500, 750.0, 100)), (position + new V3d(1000,1000, 200))], layer),
                        model.CreateShapeRepresentationAnnotation3dSurface(Plane3d.ZPlane, new Polygon2d(box.XY.Translated(position.XY-box.XY.Center).ComputeCornersCCW()), layer),
                        model.CreateShapeRepresentationAnnotation3dCross(position, V3d.YAxis, 45, 1000.0, layer)
                        //// NOT-displayed in BIMVision
                        //model.CreateShapeRepresentationAnnotation2dPoint(position.XY, layer),
                        //model.CreateShapeRepresentationAnnotation3dPoint(position, layer),
                        //model.CreateShapeRepresentationAnnotation2dArea(new Box2d(V2d.Zero, V2d.One*1000.0), layer),

                        // broken
                        //model.CreateShapeRepresentationSurveyPoints(position.XY),
                        //model.CreateShapeRepresentationSurveyPoints(position),
                    ]);
                });
            });
        }
    }
}