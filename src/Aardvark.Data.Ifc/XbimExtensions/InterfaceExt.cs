using System;
using System.Linq;
using Xbim.Ifc4.Interfaces;

namespace Aardvark.Data.Ifc
{
    public static class InterfaceExt
    {
        public static IIfcRelDefinesByType AddDefiningType(this IIfcObject obj, IIfcTypeObject theType)
        {
            return (obj, theType) switch
            {
                (Xbim.Ifc2x3.Kernel.IfcObject o, Xbim.Ifc2x3.Kernel.IfcTypeObject t) => o.AddDefiningType(t),
                (Xbim.Ifc4.Kernel.IfcObject o, Xbim.Ifc4.Kernel.IfcTypeObject t) => o.AddDefiningType(t),
                (Xbim.Ifc4x3.Kernel.IfcObject o, Xbim.Ifc4x3.Kernel.IfcTypeObject t) => o.AddDefiningType(t),
                _ => throw new NotSupportedException($"Schema {obj.Model.SchemaVersion} does not provide AddDefiningType or mixed obj and typeobject!")
            };
        }

        public static void AddPropertySet(this IIfcObject obj, IIfcPropertySet set)
        {
            switch (obj, set)
            {
                case (Xbim.Ifc2x3.Kernel.IfcObject o, Xbim.Ifc2x3.Kernel.IfcPropertySet t):
                    o.AddPropertySet(t); break;
                case (Xbim.Ifc4.Kernel.IfcObject o, Xbim.Ifc4.Kernel.IfcPropertySet t):
                    o.AddPropertySet(t); break;
                case (Xbim.Ifc4x3.Kernel.IfcObject o, Xbim.Ifc4x3.Kernel.IfcPropertySet t):
                    o.AddPropertySet(t); break;
                default: throw new NotSupportedException($"Schema {obj.Model.SchemaVersion} does not provide AddPropertySet or mixed obj and typeobject!");
            };
        }

        public static void AddQuantity(this IIfcObject obj, string qsetName, IIfcPhysicalQuantity set)
        {
            switch (obj, set)
            {
                case (Xbim.Ifc2x3.Kernel.IfcObject o, Xbim.Ifc2x3.QuantityResource.IfcPhysicalQuantity v):
                    o.AddQuantity(qsetName, v); break;
                case (Xbim.Ifc4.Kernel.IfcObject o, Xbim.Ifc4.QuantityResource.IfcPhysicalQuantity v):
                    o.AddQuantity(qsetName, v); break;
                case (Xbim.Ifc4x3.Kernel.IfcObject o, Xbim.Ifc4x3.QuantityResource.IfcPhysicalQuantity v):
                    o.AddQuantity(qsetName, v); break;
                default: throw new NotSupportedException($"Schema {obj.Model.SchemaVersion} does not provide AddQuantity or mixed obj and typeobject!");
            };
        }

        public static void AddElement(this IIfcSpatialStructureElement spat, IIfcProduct product)
        {
            var spatialStructure = spat.ContainsElements.FirstOrDefault();
            if (spatialStructure == null) //none defined create the relationship
            {
                var relSe = spat.Model.Factory().RelContainedInSpatialStructure(relSe =>
                {
                    relSe.RelatingStructure = spat;
                    relSe.RelatedElements.Add(product);

                });
            }
            else
                spatialStructure.RelatedElements.Add(product);
        }

        public static void AddSite(this IIfcProject proj, IIfcSite site)
        {
            switch (proj, site)
            {
                case (Xbim.Ifc2x3.Kernel.IfcProject p, Xbim.Ifc2x3.ProductExtension.IfcSite s):
                    p.AddSite(s); break;
                case (Xbim.Ifc4.Kernel.IfcProject p, Xbim.Ifc4.ProductExtension.IfcSite s):
                    p.AddSite(s); break;
                case (Xbim.Ifc4x3.Kernel.IfcProject p, Xbim.Ifc4x3.ProductExtension.IfcSite s):
                    p.AddSite(s); break;
                default: throw new NotSupportedException($"Schema {proj.Model.SchemaVersion} does not provide AddPropertySet or mixed obj and typeobject!");
            };
        }

        public static void SetOrChangeSiUnit(this IIfcUnitAssignment ua, IfcUnitEnum unitType, IfcSIUnitName siUnitName, IfcSIPrefix? siUnitPrefix)
        {
            var si = ua.Units.OfType<IIfcSIUnit>().FirstOrDefault(u => u.UnitType == unitType);
            if (si != null)
            {
                si.Prefix = siUnitPrefix;
                si.Name = siUnitName;
            }
            else
            {
                ua.Units.Add(ua.Model.Factory().SIUnit(s =>
                {
                    s.UnitType = unitType;
                    s.Name = siUnitName;
                    s.Prefix = siUnitPrefix;
                }));
            }
        }
    }
}