using System.Collections.Generic;
using System.Linq;
using Aardvark.Base;
using Aardvark.Geometry;

using Xbim.Common;
using Xbim.Ifc4.Interfaces;

namespace Aardvark.Data.Ifc
{
    public static class ProductExt
    {
        public static void AttachGeometriesAndProperties(this IIfcTypeProduct product, IEnumerable<IIfcRepresentationMap> repMaps, IEnumerable<IIfcPropertySetDefinition> properties)
        {
            if (!repMaps.IsEmptyOrNull()) product.RepresentationMaps.AddRange(repMaps);    // attach shared geometries
            if (!properties.IsEmptyOrNull()) product.HasPropertySets.AddRange(properties); // attach shared properties
        }

        public static IIfcBuildingElementProxyType CreateProxyType(this IModel model, IfcBuildingElementProxyTypeEnum proxyType, IEnumerable<IIfcRepresentationMap> repMaps, IEnumerable<IIfcPropertySetDefinition> properties, string name = "")
        {
            var proxy = model.Factory().BuildingElementProxyType(p =>
            {
                p.PredefinedType = proxyType;
                p.Name = name;
            });
            proxy.AttachGeometriesAndProperties(repMaps, properties);
            return proxy;
        }

        public static T LinkToType<T>(this T obj, IIfcTypeObject objType) where T : IIfcObject
        {
            obj.AddDefiningType(objType);
            return obj;
        }

        public static IIfcProduct CreateAttachInstancedRepresentation(this IIfcProduct product, IIfcElementType objectType, IIfcObjectPlacement placement, Trafo3d trafo)
        {
            var shapes = objectType.RepresentationMaps.Select(m =>
                m.Instantiate(trafo));

            // attach instanced representation (transformed by global trafo)
            product.ObjectPlacement = placement;
            product.Representation = objectType.Model.Factory().ProductDefinitionShape(r => r.Representations.AddRange(shapes));

            return product.LinkToType(objectType);
        }

        public static IIfcProduct CreateAttachInstancedRepresentation(this IIfcProduct product, IIfcElementType objectType, IIfcObjectPlacement placement, Dictionary<IIfcRepresentationMap, Trafo3d> trafos)
        {
            var shapes = objectType.RepresentationMaps.Select(m =>
            {
                if (trafos.TryGetValue(m, out Trafo3d trafo))
                {
                    return m.Instantiate(trafo);
                }
                else
                {
                    return m.Instantiate(Trafo3d.Identity);
                }
            });

            // attach instanced representation (transformed by indevidual trafos)
            product.ObjectPlacement = placement;
            product.Representation = objectType.Model.Factory().ProductDefinitionShape(r => r.Representations.AddRange(shapes));

            return product.LinkToType(objectType);
        }

        public static IIfcProduct CreateAttachRepresentation(this IIfcProduct product, IIfcObjectPlacement placement, PolyMesh mesh, C4d? fallbackColor, IIfcSurfaceStyle surfaceStyle = null, IIfcPresentationLayerAssignment layer = null, bool triangulated = true)
        {

            var shape = product.Model.CreateShapeRepresentationTessellationStyled(mesh, fallbackColor, surfaceStyle, layer, triangulated);

            // attached creaded shapes
            product.ObjectPlacement = placement;
            product.Representation = product.Model.Factory().ProductDefinitionShape(r => r.Representations.Add(shape));

            return product;
        }

        public static IIfcProduct CreateAttachRepresentation(this IIfcProduct product, V3d position, PolyMesh mesh, C4d? fallbackColor = null, IIfcSurfaceStyle surfaceStyle = null, IIfcPresentationLayerAssignment layer = null, bool triangulated = true)
        {
            var placement = product.Model.CreateLocalPlacement(position);
            product.CreateAttachRepresentation(placement, mesh, fallbackColor, surfaceStyle, layer, triangulated);

            return product;
        }
    }
}