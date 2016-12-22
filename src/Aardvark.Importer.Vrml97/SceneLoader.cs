using Aardvark.Base;
using System;
using System.Collections.Generic;
using System.IO;
using Aardvark.Data.Vrml97;

namespace Aardvark.Importer.Vrml97
{
	public class SceneLoader
	{
        Dictionary<SymMapBase, VrmlEntity> m_entities = new Dictionary<SymMapBase, VrmlEntity>();
        HashSet<SymMapBase> m_traversedNodes = new HashSet<SymMapBase>();

        Dictionary<Type, int> m_unnamedNumber = new Dictionary<Type, int>();

        static readonly SymMapBase s_emptyMap = new SymMapBase();

        public static VrmlScene Load(string filename)
        {
            Report.BeginTimed("parsing vrml");
            var vrmlParseTree = Vrml97Scene.FromFile(filename, true, false, false);
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

            trav.PerNameVisitors["Transform"] = (map, visit) =>
            {
                if ((visit & SymMapBaseTraversal.Visit.Pre) != 0)
                {
                    VrmlTransform t = GetEntity<VrmlTransform>(map);

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

            trav.PerNameVisitors["Group"] = trav.PerNameVisitors["Collision"] = (map, visit) =>
            {
                if ((visit & SymMapBaseTraversal.Visit.Pre) != 0)
                {
                    VrmlGroup g = GetEntity<VrmlGroup>(map);

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

            trav.PerNameVisitors["Switch"] = (map, visit) =>
            {
                if ((visit & SymMapBaseTraversal.Visit.Pre) != 0)
                {
                    VrmlSwitch switchNode = GetEntity<VrmlSwitch>(map);

                    frameHierarchy.Peek().Add(switchNode);

                    frameHierarchy.Push(switchNode);
                }
                if ((visit & SymMapBaseTraversal.Visit.Post) != 0)
                {
                    frameHierarchy.Pop();//.RemoveDuplicatedChildren();
                }

                return returnFunc(map, visit);
            };

            trav.PerNameVisitors["Shape"] = (map, visit) =>
            {
                if ((visit & SymMapBaseTraversal.Visit.Pre) != 0)
                {
                    if (currentShape != null)
                        throw new Exception("invalid node placement: nested shape");

                    currentShape = GetEntity<VrmlShape>(map);

                    frameHierarchy.Peek().Add(currentShape);
                }
                if ((visit & SymMapBaseTraversal.Visit.Post) != 0)
                {
                    currentShape = null;
                }

                return returnFunc(map, visit);
            };

            trav.PerNameVisitors["IndexedFaceSet"] = (map, visit) =>
            {
                if ((visit & SymMapBaseTraversal.Visit.Pre) != 0)
                {
                    var mesh = GetEntity<VrmlMesh>(map);

                    if (currentShape == null)
                        throw new Exception("invalid node placement");

                    currentShape.Geometry = mesh;
                }

                return returnFunc(map, visit);
            };

            trav.PerNameVisitors["Box"] = (map, visit) =>
            {
                if ((visit & SymMapBaseTraversal.Visit.Pre) != 0)
                {
                    var box = GetEntity<VrmlBox>(map);
                    
                    if (currentShape == null)
                        throw new Exception("invalid node placement");
                    
                    currentShape.Geometry = box;
                }

                return returnFunc(map, visit);
            };

            trav.PerNameVisitors["Sphere"] = (map, visit) =>
            {
                if ((visit & SymMapBaseTraversal.Visit.Pre) != 0)
                {
                    var sphere = GetEntity<VrmlSphere>(map);

                    if (currentShape == null)
                        throw new Exception("invalid node placement");

                    currentShape.Geometry = sphere;
                }

                return returnFunc(map, visit);
            };

            trav.PerNameVisitors["Cone"] = (map, visit) =>
            {
                if ((visit & SymMapBaseTraversal.Visit.Pre) != 0)
                {
                    var cone = GetEntity<VrmlCone>(map);

                    if (currentShape == null)
                        throw new Exception("invalid node placement");

                    currentShape.Geometry = cone;
                }

                return returnFunc(map, visit);
            };


            trav.PerNameVisitors["Cylinder"] = (map, visit) =>
            {
                if ((visit & SymMapBaseTraversal.Visit.Pre) != 0)
                {
                    var cylinder = GetEntity<VrmlCylinder>(map);

                    if (currentShape == null)
                        throw new Exception("invalid node placement");

                    currentShape.Geometry = cylinder;
                }

                return returnFunc(map, visit);
            };

            trav.PerNameVisitors["PointLight"] =  (map, visit) =>
            {
                if ((visit & SymMapBaseTraversal.Visit.Pre) != 0)
                {
                    var light = GetEntity<VrmlPointLight>(map);
                    frameHierarchy.Peek().Add(light);
                }

                return returnFunc(map, visit);
            };

            trav.PerNameVisitors["SpotLight"] = (map, visit) =>
            {
                if ((visit & SymMapBaseTraversal.Visit.Pre) != 0)
                {
                    var light = GetEntity<VrmlSpotLight>(map);
                    frameHierarchy.Peek().Add(light);
                }

                return returnFunc(map, visit);
            };

            trav.PerNameVisitors["DirectionalLight"] = (map, visit) =>
            {
                if ((visit & SymMapBaseTraversal.Visit.Pre) != 0)
                {
                    var light = GetEntity<VrmlDirectionalLight>(map);
                    frameHierarchy.Peek().Add(light);
                }

                return returnFunc(map, visit);
            };

            #region Appearance
            
            trav.PerNameVisitors["Appearance"] = (map, visit) =>
            {
                if ((visit & SymMapBaseTraversal.Visit.Pre) != 0)
                {
                    VrmlAppearance appear = GetEntity<VrmlAppearance>(map);

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
                    VrmlMaterial mat = GetEntity<VrmlMaterial>(map);

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

                    VrmlTexture tex = GetEntity<VrmlTexture>(map);

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

                    VrmlInline inl = GetEntity<VrmlInline>(map);

                    frameHierarchy.Peek().Add(inl);
                }

                return returnFunc(map, visit);
            };

            trav.PerNameVisitors["TextureTransform"] = (map, visit) =>
            {
                if ((visit & SymMapBaseTraversal.Visit.Pre) != 0)
                {
                    VrmlTextureTransform tex = GetEntity<VrmlTextureTransform>(map);

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
                    scene.Routes.Add(GetEntity<VrmlRoute>(map));
                }

                return returnFunc(map, visit);
            };

            trav.PerNameVisitors["TimeSensor"] = (map, visit) =>
            {
                if ((visit & SymMapBaseTraversal.Visit.Pre) != 0)
                {
                    scene.TimeSensors.Add(GetEntity<VrmlTimeSensor>(map));
                }

                return returnFunc(map, visit);
            };

            trav.PerNameVisitors["OrientationInterpolator"] = (map, visit) =>
            {
                if ((visit & SymMapBaseTraversal.Visit.Pre) != 0)
                {
                    scene.OrientationInterpolators.Add(GetEntity<VrmlOrientationInterpolator>(map));
                }

                return returnFunc(map, visit);
            };

            trav.PerNameVisitors["PositionInterpolator"] = (map, visit) =>
            {
                if ((visit & SymMapBaseTraversal.Visit.Pre) != 0)
                {
                    scene.PositionInterpolators.Add(GetEntity<VrmlPositionInterpolator>(map));
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

        T GetEntity<T>(SymMapBase map) where T: VrmlEntity, new()
        {
            VrmlEntity entity;
            if (!m_entities.TryGetValue(map, out entity))
            {
                entity = new T();
                entity.Name = GetName(map, typeof(T));
                entity.Init(map);
                m_entities[map] = entity;
            }
            else if (!(entity is T))
            {
                throw new Exception(String.Format("invalid cast: {1} is {2}", typeof(T).Name, entity.GetType().Name));
            }

            return (T)entity;
        }
	}
}
