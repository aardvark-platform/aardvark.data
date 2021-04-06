using Aardvark.Base;
using Aardvark.Geometry;
using Aardvark.VRVis;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Aardvark.Data.Collada
{
    public static partial class ColladaImporter
    {
        static Dictionary<string, Symbol> s_colladaSemantics = new Dictionary<string, Symbol>() 
            {
                {"POSITION", PolyMesh.Property.Positions},
                {"NORMAL", PolyMesh.Property.Normals},
                {"TEXCOORD", PolyMesh.Property.DiffuseColorCoordinates},
                {"TANGENT", PolyMesh.Property.Tangents},
                {"COLOR", PolyMesh.Property.Colors},
            };


        static class Property
        {
            public const string MaterialName = "ColladaMaterialName";
        }



        private static void ProcessInputs(InputLocal[] inputs, PolyMesh mesh, mesh m)
        {
            var multiAttIndices = new Dictionary<Symbol, int>();
    
            foreach (var input in inputs)
            {
                var sem = s_colladaSemantics[input.semantic];
                var sourceId = input.source.Substring(1);
                var arr = m.source.FirstOrDefault(s => s.id.Equals(sourceId));

                int ind = 0;
                if (multiAttIndices.TryGetValue(sem, out ind))
                {
                    if (ind == 0) // one occurance so far -> rename orig from XXX to XXX0
                    {
                        var temp = mesh.VertexAttributes[sem];
                        mesh.VertexAttributes.Remove(sem);
                        mesh.VertexAttributes.Add((sem.ToString() + "0").ToSymbol(), temp);
                    }
                    ind++;
                    sem = (sem.ToString() + ind).ToSymbol();
                }

                multiAttIndices[sem] = ind;

                var fArray = arr.Item as float_array;

                Array data;
                var stride = arr.technique_common.accessor.stride;

                switch (stride)
                {
                    case 1:
                        data = fArray.Values;
                        break;
                    case 2:
                        data = fArray.Values.UnsafeCoerce<V2d>();
                        break;
                    case 3:
                        var result = fArray.Values.UnsafeCoerce<V3d>();
                        data = result;
                        break;
                    case 4:
                        data = fArray.Values.UnsafeCoerce<V4d>();
                        break;
                    case 9:
                        data = fArray.Values.UnsafeCoerce<M33d>();
                        break;
                    case 16:
                        data = fArray.Values.UnsafeCoerce<M44d>();
                        break;
                    default:
                        throw new NotImplementedException();
                }
                if (sem == PolyMesh.Property.Positions) mesh.PositionArray = data.UnsafeCoerce<V3d>();
                else mesh.VertexAttributes.Add(sem, data);

            }
        }

        private static C4f ToColor(common_color_or_texture_typeColor col)
        {
            var channels = col._Text_.Split(' ').Select(s => float.Parse(s, CultureInfo.InvariantCulture)).ToArray();

            return new C4f(channels[0], channels[1], channels[2], channels.Length > 3 ? channels[3] : 1.0f);
        }


        public static Dictionary<string, List<PolyMesh>> GetGeometries(COLLADA collada)
        {
            var geometries = collada.Items.OfType<library_geometries>().Where(x => x.geometry != null).SelectMany(gl => gl.geometry);
            var result = new Dictionary<string, List<PolyMesh>>();

            foreach (var g in geometries)
            {
                var m = g.Item as mesh;

                if (m != null && m.Items != null)
                {
                    var meshList = new List<PolyMesh>();
                    foreach (var item in m.Items)
                    {
                        var triangles = item as triangles;
                        var polylist = item as polylist;
                        var polygons = item as polygons;
                        var lines = item as lines;

                        var count = triangles != null ? triangles.count :
                                    polylist != null ? polylist.count :
                                    polygons != null ? polygons.count :
                                    0;

                        if (count < 1)
                        {
                            Report.Warn("skipping empty mesh with {0} primitives", count);
                            continue;
                        }

                        var input = triangles != null ? triangles.input :
                                    polylist != null ? polylist.input :
                                    polygons != null ? polygons.input : 
                                    null;

                        if (input == null)
                        {
                            if (lines != null)
                                Report.Warn("lines currently not supported");
                            else
                                Report.Warn("unsupported input: {0}", item.GetType());

                            continue;
                            //throw new NotImplementedException("since I had no example i could not implement this one properly (haaser@vrvis.at)");
                        }

                        // init mesh from input and sources
                        var mesh = new PolyMesh();
                        ProcessInputs(input.Map(i =>
                            {
                                if (i.semantic == "VERTEX") return m.vertices.input.First();
                                else return new InputLocal() { semantic = i.semantic, source = i.source };
                            }),
                            mesh, m);

                        // convert input semantic to aardvark semantic
                        // if there are multiple attributes of some kind (like TEXCOORDS) convert them to index variant
                        var inputOffsets = new Dictionary<Symbol, ulong>();
                        foreach(var i in input)
                        {
                            var aardSem = s_colladaSemantics[i.semantic == "VERTEX" ? m.vertices.input.First().semantic : i.semantic];
                            var aardSemInd = aardSem; // start with non-indexed form
                            int si = 0;
                            while (inputOffsets.ContainsKey(aardSemInd))
                                aardSemInd = aardSem + (++si).ToString();
                            if (si == 1) // rename first attribute from XXX to XXX0
                            {
                                var temp = inputOffsets[aardSem];
                                inputOffsets.Remove(aardSem);
                                inputOffsets.Add((aardSem.ToString() + "0").ToSymbol(), temp);
                            }
                            inputOffsets.Add(aardSemInd, i.offset);
                        }
                        //var inputOffsets = input.ToDictionary(i => s_colladaSemantics[i.semantic == "VERTEX" ? m.vertices.input.First().semantic : i.semantic], i => (int)i.offset);

                        int[] vertexIndices = null;
                        int[] faceFirstIndices = null;
                        string matName = null;

                        if (triangles != null)
                        {
                            vertexIndices = triangles.p.Trim().Split(' ').Map(str => int.Parse(str));
                            faceFirstIndices = new int[triangles.count + 1].SetByIndex(i => i * 3);
                            matName = triangles.material;
                        }
                        else if (polylist != null)
                        {
                            vertexIndices = polylist.p.Trim().Split(' ').Map(str => int.Parse(str));
                            var first = polylist.vcount.Trim().Split(' ').Map(str => int.Parse(str)).ScanLeft(0, (a, b) => a + b);
                            faceFirstIndices = new int[first.Length + 1].SetByIndex(i => i > 0 ? first[i - 1] : 0);
                            matName = polylist.material;
                        }
                        else if (polygons != null)
                        {
                            if (!polygons.Items.IsEmptyOrNull())
                            {
                                vertexIndices = polygons.Items.OfType<string>().SelectMany(p => p.Trim().Split(' ').Map(str => int.Parse(str))).ToArray();
                                var vcounts = polygons.Items.OfType<string>().Select(p => (p.Trim().Count(c => c == ' ') + 1) / input.Length).ToArray();
                                faceFirstIndices = vcounts.Integrated().ToArray();
                            }
                            else
                            {
                                vertexIndices = new int[mesh.PositionArray.Length].SetByIndex(i => i);
                                faceFirstIndices = new int[] { 0, mesh.PositionArray.Length };
                            }
                            matName = polygons.material;
                        }

                        var indexDict = new Dictionary<Symbol, int[]>();
                        
                        foreach (var kvp in inputOffsets)
                        {
                            var attIndex = new int[vertexIndices.Length / inputOffsets.Count];
                            attIndex.SetByIndex(i => vertexIndices[inputOffsets.Count * i + (int)kvp.Value]);

                            if (kvp.Key == PolyMesh.Property.Positions) mesh.VertexIndexArray = attIndex;
                            else
                            {
                                mesh.FaceVertexAttributes[-kvp.Key] = mesh.VertexAttributes[kvp.Key];
                                mesh.FaceVertexAttributes[kvp.Key] = attIndex;
                                mesh.VertexAttributes.Remove(kvp.Key);
                            }
                        }

                        mesh.FirstIndexArray = faceFirstIndices;
                        mesh[Property.MaterialName] = matName;
                        mesh.Awake(0);
                        meshList.Add(mesh);

                        mesh[PolyMesh.Property.Name] = g.name ?? g.id;
                    }

                    if (meshList.Count > 0)
                    {
                        result.Add("#" + g.id, meshList);
                    }
                }
            }


            return result;
        }

        public static Dictionary<string, ColladaMaterial> GetMaterials(COLLADA collada)
        {
            var materials = collada.Items.OfType<library_materials>().Where(x => x.material != null).SelectMany(ml => ml.material);
            var effects = collada.Items.OfType<library_effects>().Where(x => x.effect != null).SelectMany(el => el.effect).ToDictionary(e => "#" + e.id);
            var images = collada.Items.OfType<library_images>().Where(x => x.image != null).SelectMany(il => il.image).ToDictionary(i => i.id, i => Uri.UnescapeDataString(((string)i.Item).Replace("file:///", "").Replace("file://", "")));//.ToDictionary(i => i.id, i => Uri.UnescapeDataString(((string)i.Item).Replace("file:///", "")));
            
            var result = new Dictionary<string, ColladaMaterial>();

            foreach (var m in materials)
            {
                var id = m.id;
                var effect = effects[m.instance_effect.url];
                var item = effect.Items.OfType<effectFx_profile_abstractProfile_COMMON>().FirstOrDefault();

                if (item == null || item.technique == null)
                {
                    Report.Warn("effect has no profile or no technique");
                    continue;
                }

                var localImages = new Dictionary<string, string>();
                if (item.Items != null)
                    localImages.AddRange(item.Items.OfType<image>().Select(x => KeyValuePairs.Create(x.id, x.Item as string)));
                
                // NOTE: textures sometimes directly reference an image or to an effect.Image
                var parameters = item.Items == null ? new Dictionary<string, common_newparam_type>() : item.Items.OfType<common_newparam_type>().ToDictionary(n => n.sid);
                var textureNames = new Dictionary<string, string>();
                var samplers = new Dictionary<string, string>();

                foreach (var p in parameters.Values)
                {
                    var surface = p.Item as fx_surface_common;
                    var sampler1D = p.Item as fx_sampler1D_common;
                    var sampler2D = p.Item as fx_sampler2D_common;
                    var sampler3D = p.Item as fx_sampler3D_common;

                    if (surface != null)
                    {
                        var texRef = surface.init_from.First().Value;
                        if (localImages.ContainsKey(texRef))
                            textureNames[p.sid] = localImages[texRef];
                        else
                            textureNames[p.sid] = texRef;
                    }
                    else if (sampler1D != null) samplers[p.sid] = sampler1D.source;
                    else if (sampler2D != null) samplers[p.sid] = sampler2D.source;
                    else if (sampler3D != null) samplers[p.sid] = sampler3D.source;
                }

                var mat = new ColladaMaterial();
                mat.Name = m.name ?? m.id;

                var constant = item.technique.Item as effectFx_profile_abstractProfile_COMMONTechniqueConstant;
                var lambert = item.technique.Item as effectFx_profile_abstractProfile_COMMONTechniqueLambert;
                var phong = item.technique.Item as effectFx_profile_abstractProfile_COMMONTechniquePhong;
                var blinn = item.technique.Item as effectFx_profile_abstractProfile_COMMONTechniqueBlinn;

                if (constant == null && lambert == null && phong == null && blinn == null)
                {
                    Report.Warn("not supported effect type");
                    continue;
                }

                var emission = constant != null ? constant.emission :
                               lambert != null ? lambert.emission :
                               phong != null ? phong.emission :
                               blinn != null ? blinn.emission : null;

                var diffuse = lambert != null ? lambert.diffuse :
                               phong != null ? phong.diffuse :
                               blinn != null ? blinn.diffuse : null;
                
                var transparency = constant != null ? constant.transparency :
                               lambert != null ? lambert.transparency :
                               phong != null ? phong.transparency :
                               blinn != null ? blinn.transparency : null;

                var ambient = lambert != null ? lambert.ambient :
                               phong != null ? phong.ambient :
                               blinn != null ? blinn.ambient : null;

                var specular = phong != null ? phong.specular :
                               blinn != null ? blinn.specular : null;

                var shininess = phong != null ? phong.shininess :
                                blinn != null ? blinn.shininess : null;

                if (emission != null)
                {
                    var emissionColor = emission.Item as common_color_or_texture_typeColor;
                    if (emissionColor != null) mat.Emission = ToColor(emissionColor);
                }

                if (ambient != null)
                {
                    var ambientColor = ambient.Item as common_color_or_texture_typeColor;
                    if (ambientColor != null) mat.Ambient = ToColor(ambientColor);
                }

                if (diffuse != null)
                {
                    var diffuseTexture = diffuse.Item as common_color_or_texture_typeTexture;
                    var diffuseColor = diffuse.Item as common_color_or_texture_typeColor;
                    mat.DiffuseColorTexturePath = diffuseTexture.TrySelect(x => { var img = samplers.ContainsKey(x.texture) ? textureNames.Get(samplers[x.texture]) : x.texture; return images.Get(img, img); });
                    if (diffuseColor != null) mat.Diffuse = ToColor(diffuseColor);
                }

                if (specular != null)
                {
                    var specularTexture = specular.Item as common_color_or_texture_typeTexture;
                    var specularColor = specular.Item as common_color_or_texture_typeColor;
                    mat.SpecularColorTexturePath = specularTexture.TrySelect(x => { var img = samplers.ContainsKey(x.texture) ? textureNames.Get(samplers[x.texture]) : x.texture; return images.Get(img, img); });
                    if (specularColor != null) mat.Specular = ToColor(specularColor);
                }

                if (shininess != null)
                {
                    var shininessFloat = shininess.Item as common_float_or_param_typeFloat;
                    if (shininessFloat != null) mat.Shininess = shininessFloat.Value;
                }

                if (transparency != null)
                {
                    var transparencyFloat = transparency.Item as common_float_or_param_typeFloat;
                    if (transparencyFloat != null) mat.Alpha = transparencyFloat.Value;
                }

                result.Add("#" + m.id, mat);
            }

            return result;
        }

        public static Dictionary<string, ColladaLight> GetLights(COLLADA collada)
        {
            var lights = collada.Items.OfType<library_lights>().Where(l => l.light != null).SelectMany(ll => ll.light);

            var result = new Dictionary<string, ColladaLight>();

            foreach (var l in lights)
            {
                if (l.technique_common == null || l.id == null) continue;

                var point = l.technique_common.Item as lightTechnique_commonPoint;
                var spot = l.technique_common.Item as lightTechnique_commonSpot;
                var dir = l.technique_common.Item as lightTechnique_commonDirectional;
                var ambient = l.technique_common.Item as lightTechnique_commonAmbient;

                if (point != null)
                { 
                    result[l.id] = new ColladaPointLight()
                    {
                        Color = point.color.TrySelect(x => new C3f(x.Values[0], x.Values[1], x.Values[2])),
                        Attenuation = new V3d(
                                            point.constant_attenuation.TrySelect(x => x.Value, 1),
                                            point.linear_attenuation.TrySelect(x => x.Value, 0),
                                            point.quadratic_attenuation.TrySelect(x => x.Value, 0))
                    };
                }
                else if (spot != null)
                {
                    result[l.id] = new ColladaSpotLight()
                    {
                        Color = spot.color.TrySelect(x => new C3f(x.Values[0], x.Values[1], x.Values[2])),
                        Attenuation = new V3d(
                                            spot.constant_attenuation.TrySelect(x => x.Value, 1),
                                            spot.linear_attenuation.TrySelect(x => x.Value, 0),
                                            spot.quadratic_attenuation.TrySelect(x => x.Value, 0)),
                        FalloffAngle = spot.falloff_angle.TrySelect(x => x.Value, 180),
                        FalloffExponent = spot.falloff_exponent.TrySelect(x => x.Value, 0)
                    };
                }
                else if (dir != null)
                {
                    result[l.id] = new ColladaDirectionalLight()
                    {
                        Color = dir.color.TrySelect(x => new C3f(x.Values[0], x.Values[1], x.Values[2])),
                    };
                }
                else if (ambient != null)
                {
                    result[l.id] = new ColladaAmbientLight()
                    {
                        Color = ambient.color.TrySelect(x => new C3f(x.Values[0], x.Values[1], x.Values[2])),
                    };
                }
            }

            return result;
        }

        private static ColladaNode ParseNodes(string path, node n, Dictionary<string, List<PolyMesh>> geometries, IDictionary<string, ColladaMaterial> materials, IDictionary<string, ColladaNode> nodes, IDictionary<string, object> everything)
        {
            ColladaNode result = null;
            if (!n.id.IsEmptyOrNull() && nodes.TryGetValue(n.id, out result))
                return result;

            result = new ColladaNode();

            result.Name = n.name ?? n.id;

            var matrix = n.Items != null ? n.Items.OfType<matrix>().SingleOrDefault() : null;
            var trafo = Trafo3d.Identity;
            var hasTrafo = false;

            if (matrix != null)
            {
                var mat = new M44d(matrix.Values);
                trafo = new Trafo3d(mat, mat.Inverse);
                hasTrafo = true;
            }

            if (n.Items != null)
            {
                foreach (var t in n.Items.Reverse())
                {
                    var tra = t as TargetableFloat3;
                    var rot = t as rotate;

                    if (tra != null)
                    {
                        if (tra.sid == "scale")
                            trafo *= Trafo3d.Scale(new V3d(tra.Values));
                        else
                            trafo *= Trafo3d.Translation(new V3d(tra.Values));
                        hasTrafo = true;
                    }
                    else if (rot != null)
                    {
                        trafo *= Trafo3d.RotationInDegrees(new V3d(rot.Values), rot.Values[3]);
                        hasTrafo = true;
                    }
                }
            }

            if (hasTrafo) result.Trafo = trafo;

            if (!n.id.IsEmptyOrNull())
                nodes.Add(n.id, result);             

            //if (n.type == NodeType.NODE)
            {
                if (n.instance_controller != null)
                {
                    foreach (var c in n.instance_controller)
                    {
                        // a skinned mesh
                        var materialAssignment = new Dictionary<string, string>();
                        foreach (var m in c.bind_material.technique_common)
                        {
                            var sem = m.symbol;
                            var target = m.target;

                            materialAssignment[sem] = target;
                        }

                        var ctrl = everything.Get(c.url) as controller;
                        if (ctrl != null)
                        {
                            var skin = ctrl.Item as skin;
                            if (skin != null)
                            {
                                var meshes = geometries.Get(skin.source1);
                                foreach (var mesh in meshes)
                                {
                                    var subName = (string)mesh[Property.MaterialName];
                                    var material = materials[materialAssignment[subName]];

                                    result.Add(new ColladaGeometryNode()
                                    {
                                        Mesh = mesh,
                                        Material = material,
                                        Name = mesh.Get<string>(PolyMesh.Property.Name),
                                    });
                                }
                            }
                        }
                    }
                }
                else if (n.instance_geometry != null)
                {
                    foreach (var c in n.instance_geometry)
                    {
                        var materialAssignment = new Dictionary<string, string>();
                        if (c.bind_material != null)
                        {
                            foreach (var m in c.bind_material.technique_common)
                            {
                                var sem = m.symbol;
                                var target = m.target;

                                materialAssignment[sem] = target;
                            }
                        }

                        var meshes = geometries.Get(c.url);

                        if (meshes != null)
                        {
                            foreach (var mesh in meshes)
                            {
                                var subName = (string)mesh[Property.MaterialName];
                                var mat = subName.TrySelect(sn => materialAssignment.Get(sn, null));
                                var material = mat.TrySelect(m => materials.Get(m, null));

                                result.Add(new ColladaGeometryNode()
                                {
                                    Mesh = mesh,
                                    Material = material,
                                    Name = mesh.Get<string>(PolyMesh.Property.Name) + (subName != null ? ("_" + subName) : ""),
                                });
                            }
                        }
                    }
                }
                else if (n.instance_light != null)
                {
                    Report.Line("TODO: light");
                }
                else if (n.instance_camera != null)
                {
                    Report.Line("TODO: camera");
                    foreach(var c in n.instance_camera)
                    {

                    }
                }
                else if (n.instance_node != null)
                {
                    foreach(var c in n.instance_node)
                    {
                        var i = everything.Get(c.url, null);
                        if (i == null)
                        {
                            Report.Warn("could not find instance");
                            continue;
                        }

                        var inode = i as node;
                        if (i == null)
                        {
                            Report.Warn("not node"); 
                            continue;
                        }

                        result.Add(ParseNodes(path, inode, geometries, materials, nodes, everything));
                    }
                }

                if (n.node1 != null) // subNodes
                {
                    foreach (var c in n.node1)
                    {
                        result.Add(ParseNodes(path, c, geometries, materials, nodes, everything));
                    }
                }  
            }

            return result;
        }

        public static Dictionary<string, object> BuildEntityDict(COLLADA collada)
        {
            // NOTE: SetRange will overwrite duplicate keys
            return new Dictionary<string, object>().SetRange(
                ((object)collada).DepthFirst(n => 
                                        n.GetType().GetProperties().SelectMany(p =>
                                            (p.PropertyType.IsArray ? (Array)p.GetValue(n).TrySelect(x => (Array)x, (Array)new object[0]) : (p.PropertyType.Namespace == "Aardvark.Data.Collada") ? (Array)p.GetValue(n).TrySelect(x => x.IntoArray(), (Array)new object[0]) : (Array)new object[0]).ToArrayOfT<object>().WhereNotNull())
                                        )
                                 .Select(n => 
                                        (n.GetType().GetProperty("id").TrySelect(p => (string)p.GetValue(n)), n))
                                 .Where(kv => !kv.Item1.IsEmptyOrNull())
                                 .Select(kv => KeyValuePairs.Create("#" + kv.Item1, kv.Item2)));
        }

        public static List<ColladaNode> GetSceneTree(COLLADA collada)
        {
            var geometries = GetGeometries(collada);
            var materials = GetMaterials(collada);
            var lights = GetLights(collada);
            var everything = BuildEntityDict(collada);

            var visuals = collada.Items.OfType<library_visual_scenes>().Where(x => x.visual_scene != null).SelectMany(x => x.visual_scene);
            var result = new List<ColladaNode>();
            var nodes = new Dictionary<string, ColladaNode>();

            foreach (var s in visuals)
            {
                foreach (var n in s.node)
                {
                    if (n.type == NodeType.NODE)
                    {
                        result.Add(ParseNodes(collada.Path, n, geometries, materials, nodes, everything));
                    }
                }
            }

            return result;


        }

        public static List<ColladaNode> Load(string path)
        {
            var c = COLLADA.Load(path);
            return GetSceneTree(c);
        }

        public static List<ColladaNode> Load(Stream stream)
        {
            var c = COLLADA.Load(stream);
            return GetSceneTree(c);
        }
    }
}
