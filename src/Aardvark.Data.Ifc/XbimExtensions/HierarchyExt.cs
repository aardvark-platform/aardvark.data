using System.Linq;
using Aardvark.Base;

using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace Aardvark.Data.Ifc
{
    public static class HierarchyExt
    {
        public static IFCNode CreateHierarchy(IfcStore model)
            => CreateHierarchy(model.GetProject());

        private static IFCNode CreateHierarchy(IIfcObjectDefinition obj)
            => new IFCNode(obj, obj.GetChildren().Select(x => (IIFCNode)CreateHierarchy(x)).ToList());

        public static void PrintHierarchy(string file)
        {
            using var model = IfcStore.Open(file);
            var project = model.Instances.FirstOrDefault<IIfcProject>();
            Report.Line("HIRARCHY of file: {0}\n", file);
            PrintHierarchy(project, 0);
        }

        public static void PrintHierarchy(IIfcObjectDefinition o, int level)
        {
            static string GetIndent(int level)
            {
                var indent = "";
                for (int i = 0; i < level; i++)
                    indent += "  ";
                return indent;
            }

            Report.Line(string.Format("{0}{1} [{2}]", GetIndent(level), o.Name, o.GetType().Name));

            var parent = o.GetParent();
            if (parent != null) Report.Line("parent: " + parent.ToString());

            var children = GeneralExt.GetChildren(o);
            Report.Line("children count: {0}", children.Count());
            children.ForEach(element => Report.Line(string.Format("{0}    ->{1} [{2}]", GetIndent(level), element.Name, element.GetType().Name)));

            var siblings = o.GetSiblings();
            Report.Line("sibling count: {0}", siblings.Count());
            siblings.ForEach(s => Report.Line("siblings: " + s.ToString()));

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
    }
}
