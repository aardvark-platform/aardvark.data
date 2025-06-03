using System;
using System.Linq;
using Xbim.Ifc4.Interfaces;

namespace Aardvark.Data.Ifc
{
    public static class InterfaceExt
    {
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
            {
                spatialStructure.RelatedElements.Add(product);
            }
        }
    }
}