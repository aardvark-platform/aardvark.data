﻿using Aardvark.Base;
using System;
using System.Collections.Generic;
using System.IO;

namespace Aardvark.Data.Vrml97
{
	public class SceneLoader
	{
        Dictionary<SymMapBase, VrmlNode> m_nodes = new Dictionary<SymMapBase, VrmlNode>();
        HashSet<SymMapBase> m_traversedNodes = new HashSet<SymMapBase>();

        Dictionary<Type, int> m_unnamedNumber = new Dictionary<Type, int>();

        static readonly SymMapBase s_emptyMap = new SymMapBase();

        public static VrmlScene Load(string filename)
        {
            Report.BeginTimed("parsing vrml");
            var vrmlParseTree = Vrml97Scene.FromFile(filename);
            Report.End();
            Report.BeginTimed("creating scene graph");
            var scene = new SceneLoader().Perform(vrmlParseTree);
            Report.End();
            return scene;
        }

        public static VrmlScene Load(Vrml97Scene vrmlParseTree)
		{
            return new SceneLoader().Perform(vrmlParseTree);
		}

        public static VrmlScene Load(Vrml97Scene vrmlParseTree, out Dictionary<SymMapBase, VrmlNode> nodeMap)
        {
            var loader = new SceneLoader();
            var res = loader.Perform(vrmlParseTree);
            nodeMap = loader.m_nodes;
            return res;
        }

        VrmlScene Perform(Vrml97Scene root)
		{
            SymMapBaseTraversal trav = new SymMapBaseTraversal(SymMapBaseTraversal.Mode.Modifying, SymMapBaseTraversal.Visit.PreAndPost);
            
            var filename = root.ParseTree.Get<string>((Symbol)"filename");
            var path = Path.GetDirectoryName(filename);

            var scene = new VrmlScene() 
            {
                Name = Path.GetFileName(filename),
            };

            Stack<VrmlGroup> frameHierarchy = new Stack<VrmlGroup>(scene.IntoIEnumerable());
            Dictionary<string, SymMapBase> defs = new Dictionary<string,SymMapBase>();

            VrmlShape currentShape = null;

            //trav.PerNameVisitors["DEF"] = (map, visit) =>
            //{
            //    if ((visit & SymMapBaseTraversal.Visit.Pre) != 0)
            //    {
            //        // Register name/node pair.
            //        string defName = map.Get<string>(Vrml97Sym.name);
            //        SymMapBase node = map.Get<SymMapBase>(Vrml97Sym.node);
            //        defs.Add(defName, node);
            //        node["DEFname"] = defName;
            //    }

            //    return map;
            //};

            //trav.PerNameVisitors["USE"] = (map, visit) =>
            //{
            //    if ((visit & SymMapBaseTraversal.Visit.Pre) != 0)
            //    {
            //        // Lookup USE name and return associated node.
            //        var name = map.Get<string>(Vrml97Sym.name);
            //        SymMapBase node;
            //        if (!defs.TryGetValue(name, out node))
            //            throw new Exception("DefUseResolver: USE " + name + ": Unknown!");

            //        // manually handle node (set geometry, apperance, ... or add node to parent), but do not continue traversal of subtree
            //        var visitor = trav.PerNameVisitors.Get(node.TypeName);
            //        visitor(node, SymMapBaseTraversal.Visit.PreAndPost);
            //    }

            //    return map;
            //};

            var returnFunc = new Func<SymMapBase, SymMapBaseTraversal.Visit, SymMapBase>((map, visit) => m_traversedNodes.Add(map) ? map : ((visit & SymMapBaseTraversal.Visit.Post) != 0) ? map : s_emptyMap);
            //var returnFunc = new Func<SymMapBase, SymMapBaseTraversal.Visit, SymMapBase>((map, visit) => map);

            trav.PerNameVisitors["WorldInfo"] = (map, visit) =>
                {
                    if ((visit & SymMapBaseTraversal.Visit.Pre) != 0)
                    {
                        if (scene.Info == null && scene.Title == null)
                        {
                            var info = map.Get<List<string>>((Symbol)"info");
                            scene.Info = info != null ? info.ToArray() : null;
                            scene.Title = map.Get<string>((Symbol)"title");
                        }
                        else
                        {
                            Report.Warn("file contains multiple WorldInfo nodes (ignored)");
                        }
                    }

                    return returnFunc(map, visit);
                };

            trav.PerNameVisitors[Vrml97NodeName.Transform] = (map, visit) =>
            {
                if ((visit & SymMapBaseTraversal.Visit.Pre) != 0)
                {
                    VrmlTransform t = GetNode<VrmlTransform>(map);

                    frameHierarchy.Peek().Add(t);

                    frameHierarchy.Push(t);
                }
                if ((visit & SymMapBaseTraversal.Visit.Post) != 0)
                {
                    frameHierarchy.Peek().RemoveDuplicatedChildren();
                    frameHierarchy.Pop();//.MergeShapesToObject();
                    //frameHierarchy.Peek().RemoveUselessGroups();
                }

                return returnFunc(map, visit);
            };

            trav.PerNameVisitors[Vrml97NodeName.Group] = trav.PerNameVisitors[Vrml97NodeName.Collision] = (map, visit) =>
            {
                if ((visit & SymMapBaseTraversal.Visit.Pre) != 0)
                {
                    VrmlGroup g = GetNode<VrmlGroup>(map);

                    frameHierarchy.Peek().Add(g);

                    frameHierarchy.Push(g);
                }
                if ((visit & SymMapBaseTraversal.Visit.Post) != 0)
                {
                    frameHierarchy.Peek().RemoveDuplicatedChildren();
                    frameHierarchy.Pop();//.MergeShapesToObject();
                    //frameHierarchy.Peek().RemoveUselessGroups();
                }

                return returnFunc(map, visit);
            };

            trav.PerNameVisitors[Vrml97NodeName.Switch] = (map, visit) =>
            {
                if ((visit & SymMapBaseTraversal.Visit.Pre) != 0)
                {
                    VrmlSwitch switchNode = GetNode<VrmlSwitch>(map);

                    frameHierarchy.Peek().Add(switchNode);

                    frameHierarchy.Push(switchNode);
                }
                if ((visit & SymMapBaseTraversal.Visit.Post) != 0)
                {
                    frameHierarchy.Pop();//.RemoveDuplicatedChildren();
                }

                return returnFunc(map, visit);
            };

            trav.PerNameVisitors[Vrml97NodeName.Shape] = (map, visit) =>
            {
                if ((visit & SymMapBaseTraversal.Visit.Pre) != 0)
                {
                    if (currentShape != null)
                        throw new Exception("invalid node placement: nested shape");

                    currentShape = GetNode<VrmlShape>(map);

                    frameHierarchy.Peek().Add(currentShape);
                }
                if ((visit & SymMapBaseTraversal.Visit.Post) != 0)
                {
                    currentShape = null;
                }

                return returnFunc(map, visit);
            };

            trav.PerNameVisitors[Vrml97NodeName.IndexedFaceSet] = (map, visit) =>
            {
                if ((visit & SymMapBaseTraversal.Visit.Pre) != 0)
                {
                    var mesh = GetNode<VrmlMesh>(map);

                    if (currentShape == null)
                        throw new Exception("invalid node placement");

                    currentShape.Geometry = mesh;
                }

                return returnFunc(map, visit);
            };

            trav.PerNameVisitors[Vrml97NodeName.Box] = (map, visit) =>
            {
                if ((visit & SymMapBaseTraversal.Visit.Pre) != 0)
                {
                    var box = GetNode<VrmlBox>(map);
                    
                    if (currentShape == null)
                        throw new Exception("invalid node placement");
                    
                    currentShape.Geometry = box;
                }

                return returnFunc(map, visit);
            };

            trav.PerNameVisitors[Vrml97NodeName.Sphere] = (map, visit) =>
            {
                if ((visit & SymMapBaseTraversal.Visit.Pre) != 0)
                {
                    var sphere = GetNode<VrmlSphere>(map);

                    if (currentShape == null)
                        throw new Exception("invalid node placement");

                    currentShape.Geometry = sphere;
                }

                return returnFunc(map, visit);
            };

            trav.PerNameVisitors[Vrml97NodeName.Cone] = (map, visit) =>
            {
                if ((visit & SymMapBaseTraversal.Visit.Pre) != 0)
                {
                    var cone = GetNode<VrmlCone>(map);

                    if (currentShape == null)
                        throw new Exception("invalid node placement");

                    currentShape.Geometry = cone;
                }

                return returnFunc(map, visit);
            };


            trav.PerNameVisitors[Vrml97NodeName.Cylinder] = (map, visit) =>
            {
                if ((visit & SymMapBaseTraversal.Visit.Pre) != 0)
                {
                    var cylinder = GetNode<VrmlCylinder>(map);

                    if (currentShape == null)
                        throw new Exception("invalid node placement");

                    currentShape.Geometry = cylinder;
                }

                return returnFunc(map, visit);
            };

            trav.PerNameVisitors[Vrml97NodeName.PointLight] =  (map, visit) =>
            {
                if ((visit & SymMapBaseTraversal.Visit.Pre) != 0)
                {
                    var light = GetNode<VrmlPointLight>(map);
                    frameHierarchy.Peek().Add(light);
                }

                return returnFunc(map, visit);
            };

            trav.PerNameVisitors[Vrml97NodeName.SpotLight] = (map, visit) =>
            {
                if ((visit & SymMapBaseTraversal.Visit.Pre) != 0)
                {
                    var light = GetNode<VrmlSpotLight>(map);
                    frameHierarchy.Peek().Add(light);
                }

                return returnFunc(map, visit);
            };

            trav.PerNameVisitors[Vrml97NodeName.DirectionalLight] = (map, visit) =>
            {
                if ((visit & SymMapBaseTraversal.Visit.Pre) != 0)
                {
                    var light = GetNode<VrmlDirectionalLight>(map);
                    frameHierarchy.Peek().Add(light);
                }

                return returnFunc(map, visit);
            };

            #region Appearance
            
            trav.PerNameVisitors["Appearance"] = (map, visit) =>
            {
                if ((visit & SymMapBaseTraversal.Visit.Pre) != 0)
                {
                    VrmlAppearance appear = GetNode<VrmlAppearance>(map);

                    if (currentShape == null)
                        throw new Exception("appearance not child of a shape");

                    currentShape.Appearance = appear;
                }

                return returnFunc(map, visit);
            };

            trav.PerNameVisitors["Material"] = (map, visit) =>
            {
                if ((visit & SymMapBaseTraversal.Visit.Pre) != 0)
                {
                    VrmlMaterial mat = GetNode<VrmlMaterial>(map);

                    if (currentShape == null || currentShape.Appearance == null)
                        throw new Exception("invalid placing of material node");

                    currentShape.Appearance.Material = mat;
                }

                return returnFunc(map, visit);
            };

            trav.PerNameVisitors["ImageTexture"] = trav.PerNameVisitors["PixelTexture"] = (map, visit) =>
            {
                if ((visit & SymMapBaseTraversal.Visit.Pre) != 0)
                {
                    map["path"] = path;

                    VrmlTexture tex = GetNode<VrmlTexture>(map);

                    if (currentShape == null || currentShape.Appearance == null)
                        throw new Exception("invalid placing of node");

                    currentShape.Appearance.Textures = tex;
                }

                return returnFunc(map, visit);
            };

            trav.PerNameVisitors["Inline"] = (map, visit) =>
            {
                if ((visit & SymMapBaseTraversal.Visit.Pre) != 0)
                {
                    //map["path"] = path;

                    VrmlInline inl = GetNode<VrmlInline>(map);

                    frameHierarchy.Peek().Add(inl);
                }

                return returnFunc(map, visit);
            };

            trav.PerNameVisitors["TextureTransform"] = (map, visit) =>
            {
                if ((visit & SymMapBaseTraversal.Visit.Pre) != 0)
                {
                    VrmlTextureTransform tex = GetNode<VrmlTextureTransform>(map);

                    if (currentShape == null || currentShape.Appearance == null)
                        throw new Exception("invalid placing of node");

                    currentShape.Appearance.TextureTrafo = tex;
                }

                return returnFunc(map, visit);
            };

            trav.PerNameVisitors["ROUTE"] = (map, visit) =>
            {
                if ((visit & SymMapBaseTraversal.Visit.Pre) != 0)
                {
                    scene.Routes.Add(GetNode<VrmlRoute>(map));
                }

                return returnFunc(map, visit);
            };

            trav.PerNameVisitors["TimeSensor"] = (map, visit) =>
            {
                if ((visit & SymMapBaseTraversal.Visit.Pre) != 0)
                {
                    scene.TimeSensors.Add(GetNode<VrmlTimeSensor>(map));
                }

                return returnFunc(map, visit);
            };

            trav.PerNameVisitors["OrientationInterpolator"] = (map, visit) =>
            {
                if ((visit & SymMapBaseTraversal.Visit.Pre) != 0)
                {
                    scene.OrientationInterpolators.Add(GetNode<VrmlOrientationInterpolator>(map));
                }

                return returnFunc(map, visit);
            };

            trav.PerNameVisitors["PositionInterpolator"] = (map, visit) =>
            {
                if ((visit & SymMapBaseTraversal.Visit.Pre) != 0)
                {
                    scene.PositionInterpolators.Add(GetNode<VrmlPositionInterpolator>(map));
                }

                return returnFunc(map, visit);
            };

            #endregion

            trav.Traverse(root.ParseTree);
            
            //scene.RemoveUselessGroups();

            return scene;
		}

        string GetName(SymMapBase m, Type type)
        {
            var name = m.Get<string>((Symbol)"DEFname");
            if (name == null)
            {
                int typeCnt = 0;
                m_unnamedNumber.TryGetValue(type, out typeCnt);
                name = string.Format("{0} #{1}", type.Name, typeCnt);
                m["DEFname"] = name;
                m_unnamedNumber[type] = ++typeCnt;
            }
            return name;
        }

        T GetNode<T>(SymMapBase map) where T: VrmlNode, new()
        {
            VrmlNode node;
            if (!m_nodes.TryGetValue(map, out node))
            {
                node = new T();
                node.Name = GetName(map, typeof(T));
                node.Init(map);
                m_nodes[map] = node;
            }
            else if (!(node is T))
            {
                throw new Exception(String.Format("invalid cast: {1} is {2}", typeof(T).Name, node.GetType().Name));
            }

            return (T)node;
        }
	}
}
