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

        public static void AddRelAggregates(this IIfcObjectDefinition obj, IIfcObjectDefinition relatingObject)
        {
            // check for existing RelAggregates for this parent
            var decomposition = relatingObject.IsDecomposedBy.FirstOrDefault();

            // create relationship
            decomposition ??= obj.Model.Factory().RelAggregates(relSub => relSub.RelatingObject = relatingObject);

            // update relationship
            decomposition.RelatedObjects.Add(obj);
        }
    }
}