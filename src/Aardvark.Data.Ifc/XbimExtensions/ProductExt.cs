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
            var proxy = CreateTypeProduct<IfcBuildingElementProxyType>(model, repMaps, properties, name);
            proxy.PredefinedType = proxyType;
            return proxy;
        }

        public static T CreateElement<T>(this IModel model, string elementName, IfcObjectPlacement placement, PolyMesh mesh, C4d? fallbackColor, IfcSurfaceStyle surfaceStyle = null, IfcPresentationLayerAssignment layer = null, bool triangulated = true) where T : IfcProduct, IInstantiableEntity
        {
            // create a Definition shape to hold the geometry
            var shape = model.CreateShapeRepresentationTessellationStyled(mesh, fallbackColor, surfaceStyle, layer, triangulated);

            return model.New<T>(c => {
                c.Name = elementName;

                // create a Product Definition and add the model geometry to the cube
                c.Representation = model.New<IfcProductDefinitionShape>(r => r.Representations.Add(shape));

                // now place the object into the model
                c.ObjectPlacement = placement;
            });
        }

        public static T CreateElement<T>(this IModel model, string elementName, V3d position, PolyMesh mesh, C4d? fallbackColor = null, IfcSurfaceStyle surfaceStyle = null, IfcPresentationLayerAssignment layer = null, bool triangulated = true) where T : IfcProduct, IInstantiableEntity
        {
            var placement = model.CreateLocalPlacement(position);
            return model.CreateElement<T>(elementName, placement, mesh, fallbackColor, surfaceStyle, layer, triangulated);
        }

        public static T CreateAttachElement<T>(this IfcSpatialStructureElement parent, string elementName, IfcObjectPlacement placement, PolyMesh mesh, C4d? fallbackColor = null, IfcSurfaceStyle surfaceStyle = null, IfcPresentationLayerAssignment layer = null, bool triangulated = true) where T : IfcProduct, IInstantiableEntity
        {
            var element = parent.Model.CreateElement<T>(elementName, placement, mesh, fallbackColor, surfaceStyle, layer, triangulated);
            parent.AddElement(element);
            return element;
        }

        public static T CreateAttachElement<T>(this IfcSpatialStructureElement parent, string elementName, V3d position, PolyMesh mesh, C4d? fallbackColor = null, IfcSurfaceStyle surfaceStyle = null, IfcPresentationLayerAssignment layer = null, bool triangulated = true) where T : IfcProduct, IInstantiableEntity
        {
            var element = parent.Model.CreateElement<T>(elementName, position, mesh, fallbackColor, surfaceStyle, layer, triangulated);
            parent.AddElement(element);
            return element;
        }

        public static IfcAnnotation CreateAnnotation(this IModel model, string text, IfcObjectPlacement placement, V3d position, IfcPresentationLayerWithStyle layer = null)
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
                        model.CreateShapeRepresentationAnnotation3dSurface(Plane3d.ZPlane, new Polygon2d(box.XY.Translated(position.XY - box.XY.Center).ComputeCornersCCW()), layer),
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