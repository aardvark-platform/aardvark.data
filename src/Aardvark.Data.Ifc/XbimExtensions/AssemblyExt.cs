using System.Collections.Generic;
using Aardvark.Base;

using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.Ifc4.Interfaces;

namespace Aardvark.Data.Ifc
{
    /// <summary>
    /// Assemblies of parts: an IfcElementAssembly holds its parts via IfcRelAggregates (see AddParts), parts and mounting slots are
    /// IfcDiscreteAccessory instances of shared types, and elements can be snapped to a position on a curve of another element.
    /// </summary>
    public static class AssemblyExt
    {
        public static IIfcElementAssembly CreateElementAssembly(this IModel model, string name, IIfcObjectPlacement placement, IfcElementAssemblyTypeEnum predefinedType = IfcElementAssemblyTypeEnum.USERDEFINED, string objectType = null)
        {
            return model.Factory().ElementAssembly(a =>
            {
                a.Name = name;
                if (objectType != null)
                    a.ObjectType = objectType;      // the kind of a USERDEFINED assembly
                a.ObjectPlacement = placement;
                a.PredefinedType = predefinedType;
            });
        }

        /// <summary>
        /// Type for parts and mounting slots sharing the geometry of the representation maps (IFC4+, the IFC2x3 type is abstract).
        /// Use USERDEFINED with an elementType for kinds the enumeration does not cover, e.g. "LuminaireHead".
        /// </summary>
        public static IIfcDiscreteAccessoryType CreateDiscreteAccessoryType(this IModel model, IfcDiscreteAccessoryTypeEnum predefinedType, IEnumerable<IIfcRepresentationMap> repMaps, IEnumerable<IIfcPropertySetDefinition> properties = null, string name = "", string elementType = null)
        {
            var type = model.Factory().DiscreteAccessoryType(t =>
            {
                t.Name = (name == "") ? $"DiscreteAccessoryType_{predefinedType}" : name;
                if (model.SchemaVersion != XbimSchemaVersion.Ifc2X3)
                    t.PredefinedType = predefinedType;      // IFC2x3 has no predefined accessory types
                if (elementType != null)
                    t.ElementType = elementType;
            });
            type.AttachGeometriesAndProperties(repMaps, properties);
            return type;
        }

        /// <summary>
        /// A part as a product of its own, so it is selectable, with the geometry of its type.
        /// The part origin is where it mounts on its parent, use parent.CreateRelativePlacement for the placement.
        /// </summary>
        public static IIfcDiscreteAccessory Instantiate(this IIfcDiscreteAccessoryType type, string name, IIfcObjectPlacement placement, Trafo3d trafo)
        {
            var model = type.Model;
            var part = model.Factory().DiscreteAccessory(p => p.Name = name);
            part.CreateAttachInstancedRepresentation(type, placement, trafo);

            if (model.SchemaVersion != XbimSchemaVersion.Ifc2X3)
            {
                part.PredefinedType = type.PredefinedType;
                part.ObjectType = type.ElementType;
            }

            return part;
        }

        /// <summary>
        /// Snaps the related element to a position on a curve of the relating element, e.g. a light onto the axis of a track.
        /// The parameter follows the parameterisation of the curve, for an IfcIndexedPolyCurve that is 0..1 per segment.
        /// </summary>
        public static IIfcRelConnectsElements ConnectAtPointOnCurve(this IIfcElement relating, IIfcElement related, IIfcCurve curve, double parameter, string name = null)
        {
            var factory = relating.Model.Factory();
            var point = factory.PointOnCurve(p =>
            {
                p.BasisCurve = curve;
                p.PointParameter = parameter;
            });
            var geometry = factory.ConnectionPointGeometry(g => g.PointOnRelatingElement = point);

            return factory.RelConnectsElements(rel =>
            {
                if (name != null)
                    rel.Name = name;
                rel.ConnectionGeometry = geometry;
                rel.RelatingElement = relating;
                rel.RelatedElement = related;
            });
        }

        /// <summary>
        /// Elements snapped to a curve of this element, see ConnectAtPointOnCurve, with the curve and the parameter they sit at.
        /// </summary>
        public static IEnumerable<(IIfcElement Element, IIfcCurve Curve, double Parameter)> GetElementsConnectedOnCurve(this IIfcElement relating)
        {
            foreach (var rel in relating.ConnectedTo)
            {
                if ((rel.ConnectionGeometry is IIfcConnectionPointGeometry geometry) && (geometry.PointOnRelatingElement is IIfcPointOnCurve point))
                    yield return (rel.RelatedElement, point.BasisCurve, point.PointParameter);
            }
        }
    }
}
