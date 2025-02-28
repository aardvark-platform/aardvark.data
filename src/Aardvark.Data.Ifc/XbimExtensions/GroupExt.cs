using System.Collections.Generic;
using System.Linq;
using Xbim.Common;
using Xbim.Ifc4.Interfaces;

namespace Aardvark.Data.Ifc
{
    public static class GroupExt
    {

        public static IIfcGroup CreateGroup(this IModel model, string groupName)
            => model.Factory().Group(g => g.Name = groupName);

        public static IIfcGroup CreateGroup(this IModel model, string groupName, IEnumerable<IIfcObjectDefinition> relatedObjects, IfcObjectTypeEnum groupType = IfcObjectTypeEnum.PRODUCT)
        {
            var group = model.CreateGroup(groupName);

            // Link related objects to group via IfcRelAssignsToGroup
            model.Factory().RelAssignsToGroup(rel => {
                rel.RelatingGroup = group;
                rel.RelatedObjects.AddRange(relatedObjects);
                rel.RelatedObjectsType = groupType;
            });

            return group;
        }

        public static IIfcGroup CreateGroup(this IModel model, string groupName, IEnumerable<IIfcGroup> relatedObjects)
            => model.CreateGroup(groupName, relatedObjects, IfcObjectTypeEnum.GROUP);

        public static void AddRelAssignsToGroup(this IIfcObjectDefinition obj, IIfcGroup group, IfcObjectTypeEnum groupEnum = IfcObjectTypeEnum.GROUP)
        {
            // check for existing RelAssignsToGroup for this group
            var grouping = group.IsGroupedBy.FirstOrDefault();

            // create relationship
            grouping ??= group.Model.Factory().RelAssignsToGroup(g => g.RelatingGroup = group);

            // update relationship
            grouping.RelatedObjects.Add(obj);
            grouping.RelatedObjectsType = groupEnum;
        }
    }
}