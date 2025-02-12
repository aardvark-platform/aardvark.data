using System.Collections.Generic;

using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;


namespace Aardvark.Data.Ifc
{
    public static class GroupExt
    {

        public static IfcGroup CreateGroup(this IModel model, string groupName)
            => model.New<IfcGroup>(g => g.Name = groupName);

        public static IfcGroup CreateGroup(this IModel model, string groupName, IEnumerable<IfcObjectDefinition> relatedObjects, IfcObjectTypeEnum groupType = IfcObjectTypeEnum.PRODUCT)
        {
            var group = model.CreateGroup(groupName);

            // Link related objects to group via IfcRelAssignsToGroup
            model.New<IfcRelAssignsToGroup>(rel => {
                rel.RelatingGroup = group;
                rel.RelatedObjects.AddRange(relatedObjects);
                rel.RelatedObjectsType = groupType;
            });

            return group;
        }

        public static IfcGroup CreateGroup(this IModel model, string groupName, IEnumerable<IfcGroup> relatedObjects)
            => model.CreateGroup(groupName, relatedObjects, IfcObjectTypeEnum.GROUP);
    }
}