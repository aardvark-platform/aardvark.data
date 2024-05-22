using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Aardvark.Base;

namespace Aardvark.Data.Ifc
{
    public static class IFCHelper
    {
        public static void PrintHierarchy(string file)
        {
            using var model = IfcStore.Open(file);
            var project = model.Instances.FirstOrDefault<IIfcProject>();
            Report.Line("HIRARCHY of file: {0}\n", file);
            PrintHierarchy(project, 0);
        }

        public static IEnumerable<IIfcPropertySingleValue> GetProperties(this IIfcObject o)
        {
            return o.IsDefinedBy.Where(r => r.RelatingPropertyDefinition is IIfcPropertySet)
                    .SelectMany(r => ((IIfcPropertySet)r.RelatingPropertyDefinition).HasProperties)
                    .OfType<IIfcPropertySingleValue>();
        }

        public static IEnumerable<IIfcPropertySingleValue> GetProperties(this IIfcObject o, string propertySetName)
        {
            return o.IsDefinedBy.Where(r => r.RelatingPropertyDefinition is IIfcPropertySet)
                    .Where(r => ((IIfcPropertySet)r.RelatingPropertyDefinition).Name == propertySetName)
                    .SelectMany(r => ((IIfcPropertySet)r.RelatingPropertyDefinition).HasProperties)
                    .OfType<IIfcPropertySingleValue>();
        }

        public static Dictionary<string, string> GetPropertiesDict(this IIfcObject o)
        {
            return o.GetProperties().ToDictionaryDistinct((x => x.Name.ToString()), (x => x.NominalValue.ToString()), (x, w) => true);
        }

        public static Dictionary<string, string> GetPropertiesDict(this IIfcObject o, string propertySetName)
        {
            return o.GetProperties(propertySetName).ToDictionaryDistinct((x => x.Name.ToString()), (x => x.NominalValue.ToString()), (x, w) => true);
        }

        public static Dictionary<string, string> GetHilitePropertiesDict(this IIfcObject o)
        {
            return o.GetPropertiesDict("Hilite");
        }

        public static IIfcProject GetProject(this IfcStore model)
        {
            return model.Instances.FirstOrDefault<IIfcProject>();
        }

        public static IIfcObjectDefinition GetParent(this IIfcObjectDefinition o)
        {
            return o.Decomposes.Select(r => r.RelatingObject).FirstOrDefault();
        }

        public static IEnumerable<IIfcObjectDefinition> GetSiblings(this IIfcObjectDefinition o)
        {
            return o.Decomposes.SelectMany(r => r.RelatedObjects).Where(x => !o.Equals(x));
        }

        public static IEnumerable<IIfcObjectDefinition> GetChildren(this IIfcObjectDefinition o)
        {
            var children = o.IsDecomposedBy.SelectMany(r => r.RelatedObjects);

            if ((o as IIfcSpatialStructureElement) != null)
            {
                children = children.Concat(((IIfcSpatialStructureElement)o).ContainsElements.SelectMany(rel => rel.RelatedElements).Cast<IIfcObjectDefinition>());
            }

            return children;
        }

        public static IFCNode CreateHierarchy(IfcStore model)
        {
            return CreateHierarchy(model.GetProject());
        }

        private static IFCNode CreateHierarchy(IIfcObjectDefinition obj)
        {
            return new IFCNode(obj, obj.GetChildren().Select(x => (IIFCNode)CreateHierarchy(x)).ToList());
        }

        public static void PrintHierarchy(IIfcObjectDefinition o, int level)
        {
            Report.Line(string.Format("{0}{1} [{2}]", GetIndent(level), o.Name, o.GetType().Name));

            var parent = o.GetParent();
            if (parent != null) Report.Line("parent: " + parent.ToString());

            var children = GetChildren(o);
            Report.Line("children count: {0}", children.Count());
            children.ForEach(element => Report.Line(string.Format("{0}    ->{1} [{2}]", GetIndent(level), element.Name, element.GetType().Name)));

            var siblings = o.GetSiblings();
            Report.Line("sibling count: {0}", siblings.Count());
            siblings.ForEach(s => Report.Line("siblings: "+s.ToString()));

            Report.Line();
            
            ////only spatial elements can contain building elements
            //var spatialElement = o as IIfcSpatialStructureElement;
            //if (spatialElement != null)
            //{
            //    //using IfcRelContainedInSpatialElement to get contained elements
            //    var containedElements = spatialElement.ContainsElements.SelectMany(rel => rel.RelatedElements);
            //    foreach (var element in containedElements)
            //        Console.WriteLine(string.Format("{0}    ->{1} [{2}]", GetIndent(level), element.Name, element.GetType().Name));
            //}

            //using IfcRelAggregares to get spatial decomposition of spatial structure elements
            foreach (var item in o.IsDecomposedBy.SelectMany(r => r.RelatedObjects))
                PrintHierarchy(item, level + 1);
        }

        private static string GetIndent(int level)
        {
            var indent = "";
            for (int i = 0; i < level; i++)
                indent += "  ";
            return indent;
        }
    }
}
