using System.Linq;
using Xbim.Ifc4x3.Kernel;
using Xbim.Ifc4x3.ProductExtension;
using Xbim.Ifc4x3.QuantityResource;

namespace Aardvark.Data.Ifc
{
    public static class Ifc4x3Ext
    {
        #region Xbim.Ifc4x3.Kernel.IfcObject

        public static IfcRelDefinesByType AddDefiningType(this IfcObject obj, IfcTypeObject theType)
        {
            var typedefs = obj.Model.Instances.Where<IfcRelDefinesByType>(r => r.RelatingType == theType).ToList();
            var thisTypeDef = typedefs.FirstOrDefault(r => r.RelatedObjects.Contains((obj)));
            if (thisTypeDef != null) return thisTypeDef; // it is already type related
            var anyTypeDef = typedefs.FirstOrDefault(); //take any one of the rels of the type
            if (anyTypeDef != null)
            {
                anyTypeDef.RelatedObjects.Add(obj);
                return anyTypeDef;
            }
            var newdef = obj.Model.Instances.New<IfcRelDefinesByType>(); //create one
            newdef.RelatedObjects.Add(obj);
            newdef.RelatingType = theType;
            return newdef;
        }

        public static void AddPropertySet(this IfcObject obj, IfcPropertySet pSet)
        {
            var relDef = obj.Model.Instances.OfType<IfcRelDefinesByProperties>().FirstOrDefault(r => pSet.Equals(r.RelatingPropertyDefinition));
            if (relDef == null)
            {
                relDef = obj.Model.Instances.New<IfcRelDefinesByProperties>();
                relDef.RelatingPropertyDefinition = pSet;
            }
            relDef.RelatedObjects.Add(obj);
        }

        public static IfcElementQuantity GetElementQuantity(this IfcObject obj, string pSetName, bool caseSensitive = true)
        {
            var qSets = obj.IsDefinedBy.SelectMany(r => r.RelatingPropertyDefinition.PropertySetDefinitions).OfType<IfcElementQuantity>();
            return qSets.FirstOrDefault(qset => string.Compare(pSetName, qset.Name, !caseSensitive) == 0);
        }

        public static IfcElementQuantity AddQuantity(this IfcObject obj, string propertySetName, IfcPhysicalQuantity quantity, string methodOfMeasurement = null)
        {
            var pset = obj.GetElementQuantity(propertySetName);

            if (pset == null)
            {
                pset = obj.Model.Instances.New<IfcElementQuantity>();
                pset.Name = propertySetName;
                var relDef = obj.Model.Instances.New<IfcRelDefinesByProperties>();
                relDef.RelatingPropertyDefinition = pset;
                relDef.RelatedObjects.Add(obj);
            }
            pset.Quantities.Add(quantity);
            if (!string.IsNullOrEmpty(methodOfMeasurement)) pset.MethodOfMeasurement = methodOfMeasurement;
            return pset;
        }

        #endregion

        #region IfcProject

        public static void AddSite(this IfcProject proj, IfcSite site) 
            => proj.AddRelAggregates(site);

        #endregion
    }
}