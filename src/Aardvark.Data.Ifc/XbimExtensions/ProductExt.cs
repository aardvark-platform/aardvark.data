using System;
using System.Collections.Generic;
using System.Linq;
using Aardvark.Base;
using Aardvark.Geometry;

using Xbim.Common;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.SharedBldgElements;

namespace Aardvark.Data.Ifc
{
    public static class ProductExt
    {
        public static T CreateTypeProduct<T>(this IModel model, IEnumerable<IIfcRepresentationMap> repMaps, IEnumerable<IIfcPropertySetDefinition> properties, string name = "") where T : IIfcTypeProduct, IInstantiableEntity
        {
            return model.New<T>(l =>
            {
                //l.PredefinedType; // Has to be set afterwards!
                l.Name = name;
                if (!repMaps.IsEmptyOrNull()) l.RepresentationMaps.AddRange(repMaps);    // shared geometries
                if (!properties.IsEmptyOrNull()) l.HasPropertySets.AddRange(properties); // shared properties
            });
        }

        public static IfcBuildingElementProxyType CreateProxyType(this IModel model, IfcBuildingElementProxyTypeEnum proxyType, IEnumerable<IIfcRepresentationMap> repMaps, IEnumerable<IIfcPropertySetDefinition> properties, string name = "")
        {
            var proxy = CreateTypeProduct<IfcBuildingElementProxyType>(model, repMaps, properties, name);
            proxy.PredefinedType = proxyType;
            return proxy;
        }

        public static T LinkToType<T>(this T obj, IIfcTypeObject objType) where T : IIfcObject
        {
            obj.AddDefiningType(objType);
            return obj;
        }

        public static I Instantiate<T, I>(this T objectType, string name, IIfcObjectPlacement placement, Trafo3d trafo) where T : IIfcElementType where I : IIfcProduct, IInstantiableEntity
        {
            var factory = objectType.Model.Factory();
            // create instance and transform all representations by global trafo
            var instance = objectType.Model.New<I>(t =>
            {
                t.Name = name;
                t.ObjectPlacement = placement;
                t.Representation = factory.ProductDefinitionShape(r =>
                     t.Representation = factory.ProductDefinitionShape(r =>
                    r.Representations.AddRange(objectType.RepresentationMaps.Select(m => m.Instantiate(trafo)))));
            });

            return instance.LinkToType(objectType);
        }

        public static I Instantiate<T, I>(this T objectType, string name, IIfcObjectPlacement placement, Dictionary<IIfcRepresentationMap, Trafo3d> trafos) where T : IIfcElementType where I : IIfcProduct, IInstantiableEntity
        {
            var factory = objectType.Model.Factory();
            // create instance and transform indevidual representations
            var instance = objectType.Model.New<I>(t =>
            {
                t.Name = name;
                t.ObjectPlacement = placement;
                t.Representation = factory.ProductDefinitionShape(r =>
                    r.Representations.AddRange(objectType.RepresentationMaps.Select(m =>
                        trafos.TryGetValue(m, out Trafo3d trafo) ? m.Instantiate(trafo) : m.Instantiate(Trafo3d.Identity))));
            });

            return instance.LinkToType(objectType);
        }

        public static T CreateElement<T>(this IModel model, string elementName, IIfcObjectPlacement placement, PolyMesh mesh, C4d? fallbackColor, IIfcSurfaceStyle surfaceStyle = null, IIfcPresentationLayerAssignment layer = null, bool triangulated = true) where T : IIfcProduct, IInstantiableEntity
        {
            // create a Definition shape to hold the geometry
            var shape = model.CreateShapeRepresentationTessellationStyled(mesh, fallbackColor, surfaceStyle, layer, triangulated);

            return model.New<T>(c => {
                c.Name = elementName;

                // create a Product Definition and add the model geometry to the cube
                c.Representation = model.Factory().ProductDefinitionShape(r => r.Representations.Add(shape));

                // now place the object into the model
                c.ObjectPlacement = placement;
            });
        }

        public static T CreateElement<T>(this IModel model, string elementName, V3d position, PolyMesh mesh, C4d? fallbackColor = null, IIfcSurfaceStyle surfaceStyle = null, IIfcPresentationLayerAssignment layer = null, bool triangulated = true) where T : IIfcProduct, IInstantiableEntity
        {
            var placement = model.CreateLocalPlacement(position);
            return model.CreateElement<T>(elementName, placement, mesh, fallbackColor, surfaceStyle, layer, triangulated);
        }

        public static T CreateAttachElement<T>(this IIfcSpatialStructureElement parent, string elementName, IIfcObjectPlacement placement, PolyMesh mesh, C4d? fallbackColor = null, IIfcSurfaceStyle surfaceStyle = null, IIfcPresentationLayerAssignment layer = null, bool triangulated = true) where T : IIfcProduct, IInstantiableEntity
        {
            var element = parent.Model.CreateElement<T>(elementName, placement, mesh, fallbackColor, surfaceStyle, layer, triangulated);
            parent.AddElement(element);
            return element;
        }

        public static T CreateAttachElement<T>(this IIfcSpatialStructureElement parent, string elementName, V3d position, PolyMesh mesh, C4d? fallbackColor = null, IIfcSurfaceStyle surfaceStyle = null, IIfcPresentationLayerAssignment layer = null, bool triangulated = true) where T : IIfcProduct, IInstantiableEntity
        {
            var element = parent.Model.CreateElement<T>(elementName, position, mesh, fallbackColor, surfaceStyle, layer, triangulated);
            parent.AddElement(element);
            return element;
        }
    }
}