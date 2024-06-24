using Aardvark.Base;
using System.Collections.Generic;

namespace Aardvark.Data.Vrml97
{
    /// <summary>
    /// The DefUseResolver is used to resolve all
    /// occurences of DEF and USE in a Vrml97 parse
    /// tree.
    /// If multiple USE statements reference the same
    /// node, then the parse tree becomes a DAG.
    /// 
    /// Example:
    /// 
    /// Parser parser = new Parser("myVrmlFile.wrl");
    /// SymMapBase parseTree = parser.Perform();
    /// 
    /// DefUseResolver resolver = new DefUseResolver();
    /// SymMapBase resolvedParseTree = resolver.Perform(parseTree);
    /// 
    /// </summary>
    internal class DefUseResolver
    {
        public static Vrml97Scene Resolve(Vrml97Scene vrmlParseTree,
            out Dictionary<string, SymMapBase> namedNodes)
            => new DefUseResolver().Perform(vrmlParseTree, out namedNodes);

        /// <summary>
        /// Takes a VRML97 parse tree (see also <seealso cref="Parser"/>)
        /// and resolves all DEF and USE nodes.
        /// </summary>
        /// <param name="root">Parse tree.</param>
        /// <param name="namedNodes"></param>
        /// <returns>Parse tree without DEF and USE nodes.</returns>
        public Vrml97Scene Perform(Vrml97Scene root, out Dictionary<string, SymMapBase> namedNodes)
        {
            root.ParseTree["DefUseResolver.Performed"] = true; // leave hint

            m_defs = new Dictionary<string, SymMapBase>();
            SymMapBaseTraversal trav = new SymMapBaseTraversal();

            trav.PerNameVisitors[Vrml97Sym.USE] = (map, visit) =>
            {
                // Lookup USE name and return associated node.
                var name = map.Get<string>(Vrml97Sym.name);
                SymMapBase node;
                if (!m_defs.TryGetValue(name, out node))
                {
                    Report.Warn($"[Vrml97] DefUseResolver: USE \"{name}\" unknown!");
                    return map; // keep "USE" ref node in SymMap-Tree
                    //throw new Exception("DefUseResolver: USE " + name + ": Unknown!");
                }

                return node;
            };

            trav.PerNameVisitors[Vrml97Sym.DEF] = (map, visit) =>
            {
                // Register name/node pair.
                string defName = map.Get<string>(Vrml97Sym.name);
                SymMapBase node = map.Get<SymMapBase>(Vrml97Sym.node);
                m_defs[defName] = node;

                node[Vrml97Sym.DEFname] = defName;
                return node;
            };

            trav.Traverse(root.ParseTree);
            namedNodes = m_defs;
            return root;
        }

        private Dictionary<string, SymMapBase> m_defs;
    }
}
