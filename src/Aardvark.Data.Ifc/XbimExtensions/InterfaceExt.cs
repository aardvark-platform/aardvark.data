using System.Collections.Generic;
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

        public static void AddElements(this IIfcSpatialStructureElement spat, IEnumerable<IIfcProduct> products)
        {
            foreach (var product in products)
                spat.AddElement(product);
        }

        /// <summary>
        /// Makes the parts components of the whole (IfcRelAggregates), reusing the existing decomposition of the whole.
        /// Nest the calls to build deeper hierarchies, e.g. fixture -> base -> arm -> head.
        /// </summary>
        public static IIfcRelAggregates AddParts(this IIfcObjectDefinition whole, params IIfcObjectDefinition[] parts)
            => whole.AddParts((string)null, parts);

        public static IIfcRelAggregates AddParts(this IIfcObjectDefinition whole, string name, params IIfcObjectDefinition[] parts)
        {
            var decomposition = whole.IsDecomposedBy.FirstOrDefault() ?? whole.Model.Factory().RelAggregates(rel => rel.RelatingObject = whole);
            if (name != null)
                decomposition.Name = name;
            decomposition.RelatedObjects.AddRange(parts);
            return decomposition;
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