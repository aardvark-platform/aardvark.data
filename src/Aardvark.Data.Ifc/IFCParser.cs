using Aardvark.Base;
using Aardvark.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xbim.Common;
using Xbim.Common.Geometry;
using Xbim.Common.Metadata;
using Xbim.Common.XbimExtensions;
using Xbim.Geometry.Abstractions;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.UtilityResource;
using Xbim.ModelGeometry.Scene;

namespace Aardvark.Data.Ifc
{
    public class IFCParser
    {
        static readonly XbimColourMap _colourMap = new XbimColourMap();

        /// <summary>
        /// Opens an IFC file, tessellates its geometry if the store does not contain any yet and wraps model, meshes, materials and hierarchy into an IFCData.
        /// </summary>
        /// <param name="filePath">path of the .ifc file</param>
        /// <param name="editor">optional editor credentials used by xBIM for the owner history</param>
        /// <param name="geometryEngine">xBIM geometry engine used for tessellation</param>
        /// <param name="singleThreading">tessellate on a single thread, avoids deadlocks in the xBIM geometry engine</param>
        /// <param name="enableCaching">keep xBIM's inverse and entity caches alive for fast queries on the returned data.
        /// xBIM refuses to open a transaction while the inverse cache is active, so use IFCData.EndCaching or pass false before editing the store.</param>
        public static IFCData PreprocessIFC(string filePath, XbimEditorCredentials editor = null, XGeometryEngineVersion geometryEngine = XGeometryEngineVersion.V6, bool singleThreading = true, bool enableCaching = true)
        {
            var model = IfcStore.Open(filePath, editor);

            // the caches speed up the inverse lookups done by geometry creation, parsing and the hierarchy traversal
            // they are started once and handed to IFCData, which owns them from here on
            IInverseCache cacheInverse = null;
            IEntityCache cacheEntity = null;
            if (enableCaching)
            {
                cacheInverse = model.BeginInverseCaching();
                cacheEntity = model.BeginEntityCaching();
            }

            CreateGeometry(model, geometryEngine, singleThreading);

            foreach (var modelReference in model.ReferencedModels)
            {
                // creates federation geometry contexts if needed
                if (modelReference.Model == null)
                    continue;
                CreateGeometry(modelReference.Model, geometryEngine, singleThreading);
            }

            var (content, materials) = ParseIFC(model);
            var hierarchy = HierarchyExt.CreateHierarchy(model);

            return new IFCData(model, cacheInverse, cacheEntity, content, materials, hierarchy);
        }

        private static void CreateGeometry(IModel model, XGeometryEngineVersion geometryEngine, bool singleThreading)
        {
            if (!model.GeometryStore.IsEmpty)
                return;

            var context = new Xbim3DModelContext(model, engineVersion: geometryEngine);
            if (singleThreading) context.MaxThreads = 1;
            //upgrade to new geometry representation, uses the default 3D model
            context.CreateContext(null, true, false);
        }

        private static (Dict<IfcGloballyUniqueId, IFCContent>, Dictionary<string, IFCMaterial>) ParseIFC(IModel model)
        {
            var excludedTypes = DefaultExclusions(model);

            var output = new Dict<IfcGloballyUniqueId, IFCContent>(true);

            var materialsByName = new Dictionary<string, IFCMaterial>();
            var materialsByStyleId = new Dictionary<int, IFCMaterial>();
            var repeatedShapeGeometries = new Dictionary<int, PolyMesh>();

            using (var geomStore = model.GeometryStore)
            {
                using var geomReader = geomStore.BeginRead();
                //get a list of all the unique style ids then build their style and mesh
                var sstyleIds = geomReader.StyleIds;
                foreach (var styleId in sstyleIds)
                {
                    var material = GetMaterialByStyle(model, styleId);
                    if (!materialsByStyleId.ContainsKey(styleId))
                        materialsByStyleId.Add(styleId, material);
                    if (!materialsByName.ContainsKey(material.Name))
                        materialsByName.Add(material.Name, material);
                }

                var shapeInstances = geomReader.ShapeInstances.Where(s => !excludedTypes.Contains(s.IfcTypeId));

                foreach (var shapeInstance in shapeInstances)
                {
                    if (model.Instances[shapeInstance.IfcProductLabel] is not IIfcObject ifcObject)
                    {
                        Report.Warn("Shape instance refers to missing product #{0}, skipping!", shapeInstance.IfcProductLabel);
                        continue;
                    }

                    // work out style
                    var styleId = shapeInstance.StyleLabel > 0 ? shapeInstance.StyleLabel : shapeInstance.IfcTypeId * -1;

                    // Associated Materials
                    var assocMaterials = ifcObject.HasAssociations.OfType<IIfcRelAssociatesMaterial>();
                    List<IFCMaterial> assocOutput = new List<IFCMaterial>();

                    if (!assocMaterials.IsEmptyOrNull())
                    {
                        var assocMat = new List<IFCMaterial>();

                        foreach (var matRel in assocMaterials)
                            GetAssociatedMaterials(matRel.RelatingMaterial, assocMat);

                        foreach (var m in assocMat)
                        {
                            assocOutput.Add(materialsByName.GetCreate(m.Name, name => m));
                        }
                    }

                    if (!materialsByStyleId.ContainsKey(styleId))
                    {
                        // use default material of prodType
                        var m = GetMaterialByExpressType(model, shapeInstance.IfcTypeId);
                        materialsByStyleId.Add(styleId, m);
                        if (!materialsByName.ContainsKey(m.Name))
                            materialsByName.Add(m.Name, m);
                    }

                    //GET THE ACTUAL GEOMETRY

                    //see if we have already read it
                    if (!repeatedShapeGeometries.TryGetValue(shapeInstance.ShapeGeometryLabel, out PolyMesh polyMesh))
                    {
                        IXbimShapeGeometryData shapeGeom = geomReader.ShapeGeometry(shapeInstance.ShapeGeometryLabel);

                        if (shapeGeom.Format != (byte)XbimGeometryType.PolyhedronBinary)
                            continue;
                        try
                        {
                            polyMesh = CreatePolyMeshFromIFC(shapeGeom.ShapeData);
                        }
                        catch (Exception e)
                        {
                            // skip the instance, a content entry without a mesh is of no use to the caller
                            Report.Error("Creating PolyMesh from IFC failed for product #{0}! {1}", shapeInstance.IfcProductLabel, e.ToString());
                            continue;
                        }

                        if (shapeGeom.ReferenceCount > 1) //only store if we are going to use again
                            repeatedShapeGeometries.Add(shapeInstance.ShapeGeometryLabel, polyMesh);
                    }

                    //var prodTypeName = (model.Metadata.ExpressType(shapeInstance.IfcTypeId)).Name;

                    var guid = ifcObject.GlobalId;
                    IFCMaterial material;

                    if (styleId > 0)
                        material = materialsByStyleId[styleId]; // first try to use style
                    else if (!assocOutput.IsEmptyOrNull())
                        material = assocOutput.First();         // second try to use first Associated material
                    else
                        material = materialsByStyleId[styleId]; // otherwise use Default material

                    if (material == null || material.Name == null) // || material.Texture == null
                        Report.Line("No valid material!");

                    var instanceTrafo = shapeInstance.Transformation.ToTrafo3d();
                    var representationType = shapeInstance.RepresentationType;

                    output.Add(guid, new IFCContent(ifcObject, polyMesh, instanceTrafo, material, representationType));
                }
            }
            return (output, materialsByName);
        }

        private static HashSet<short> DefaultExclusions(IModel model)
        {
            var excludedTypes = new HashSet<short>();
            var exclude = new List<Type>()
                {
                    //typeof(IIfcSpace),
                    typeof(IIfcFeatureElement)
                };

            foreach (var excludedT in exclude)
            {
                ExpressType ifcT;
                if (excludedT.IsInterface && excludedT.Name.StartsWith("IIfc"))
                {
                    var concreteTypename = excludedT.Name.Substring(1).ToUpper();
                    ifcT = model.Metadata.ExpressType(concreteTypename);
                }
                else
                    ifcT = model.Metadata.ExpressType(excludedT);
                if (ifcT == null) // it could be a type that does not belong in the model schema
                    continue;
                foreach (var exIfcType in ifcT.NonAbstractSubTypes)
                {
                    excludedTypes.Add(exIfcType.TypeId);
                }
            }
            return excludedTypes;
        }

        public static void GetAssociatedMaterials(IIfcMaterialSelect matSel, List<IFCMaterial> materials)
        {
            try
            {
                if (matSel is IIfcMaterial material)
                {
                    materials.Add(GetMaterial(material));
                }
                else if (matSel is IIfcMaterialLayer materialLayer)
                {
                    double thickness = materialLayer.LayerThickness;

                    bool isVentilated = ((bool?)materialLayer.IsVentilated) ?? false;
                    if (isVentilated)
                    {
                        var voidMaterial = new IFCMaterial(materialLayer.Name.ToString(), XbimTexture.Create(128, 128, 128, 128));
                        materials.Add(voidMaterial);
                    }
                    else
                    {
                        materials.Add(GetMaterial(materialLayer.Material));
                    }
                }
                else if (matSel is IIfcMaterialProfile profile)
                {
                    var name = profile.Profile.ProfileName.Value;
                    materials.Add(GetMaterial(profile.Material));
                }
                else if (matSel is IIfcMaterialConstituent materialConstituent)
                {
                    var name = materialConstituent.Name.Value;
                    materials.Add(GetMaterial(materialConstituent.Material));
                }
                else if (matSel is IIfcMaterialList materialList)
                {
                    foreach (var item in materialList.Materials) // depricated...in Ifc4
                    {
                        materials.Add(GetMaterial(item));
                    }
                }
                else if (matSel is IIfcMaterialLayerSet materialLayerSet)
                {
                    var setName = materialLayerSet.LayerSetName;
                    foreach (var item in materialLayerSet.MaterialLayers) 
                    {
                        GetAssociatedMaterials(item, materials);
                    }
                }
                else if (matSel is IIfcMaterialLayerSetUsage materialLayerSetUsage)
                {
                    var setName = materialLayerSetUsage.ForLayerSet.LayerSetName;
                    foreach (var item in materialLayerSetUsage.ForLayerSet.MaterialLayers)
                    {
                        GetAssociatedMaterials(item, materials);
                    }
                }
                else if (matSel is IIfcMaterialProfileSet materialProfileSet)
                {
                    var setName = materialProfileSet.Name;
                    foreach (var item in materialProfileSet.MaterialProfiles)
                    {
                        GetAssociatedMaterials(item, materials);
                    }
                }
                else if (matSel is IIfcMaterialProfileSetUsage materialProfileSetUsage)
                {
                    var setName = materialProfileSetUsage.ForProfileSet.Name;
                    foreach (var item in materialProfileSetUsage.ForProfileSet.MaterialProfiles)
                    {
                        GetAssociatedMaterials(item, materials);
                    }
                }
                else if (matSel is IIfcMaterialConstituentSet materialConstituentSet)
                {
                    var setName = materialConstituentSet.Name;
                    foreach (var item in materialConstituentSet.MaterialConstituents)
                    {
                        GetAssociatedMaterials(item, materials);
                    }
                }
                else Report.Warn("Unknown Associated Material Type!");

            }
            catch (ArgumentException e)
            {
                // GetMaterial reports a material without visual representation through the parameter name, keep it as name-only material
                materials.Add(new IFCMaterial(e.ParamName, null));
            }
        }

        public static IFCMaterial GetMaterial(IIfcMaterial mat)
        {
            var name = mat.Name.ToString();
            
            var matDefRep = mat.HasRepresentation.FirstOrDefault() ?? throw new ArgumentException("No repesentation", name);

            IIfcSurfaceStyle GetFirstSurfaceStyle() {
                foreach (var rep in matDefRep.Representations) {
                    foreach (var repItem in rep.Items) {
                        if (repItem is IIfcStyledItem styledItem) {
                            foreach (var style in styledItem.Styles) {
                                if (style is IIfcSurfaceStyle surf) {
                                    return surf;
                                }
                            }
                        }
                    }
                }
                return null;
            }

            var surfStyle = GetFirstSurfaceStyle() ?? throw new ArgumentException("No surfStyle", name);

            var properties = mat.GetPropertiesDict();
            
            return new IFCMaterial(name, 
                XbimTexture.Create(surfStyle), 
                properties.TryGetProperty<double>("ThermalConductivity"),
                properties.TryGetProperty<double>("SpecificHeatCapacity"),
                properties.TryGetProperty<double>("MassDensity")
            );
        }

        public static IFCMaterial GetMaterialByStyle(IModel model, int styleId)
        {
            if (model.Instances[styleId] is not IIfcSurfaceStyle sStyle)
            {
                // the geometry store references a style label that is not a surface style, fall back to xBIM's default colour
                Report.Warn("Style #{0} is not an IfcSurfaceStyle, using default colour!", styleId);
                var fallback = XbimTexture.Create(XbimColour.DefaultColour);
                fallback.DefinedObjectId = styleId;
                return new IFCMaterial("Style-" + styleId, fallback);
            }

            var texture = XbimTexture.Create(sStyle);
            texture.DefinedObjectId = styleId;

            return new IFCMaterial("Style-" + sStyle.Name, texture);
        }

        public static IFCMaterial GetMaterialByExpressType(IModel model, short typeid)
        {
            var prodType = model.Metadata.ExpressType(typeid);
            var v = _colourMap[prodType.Name];
            var texture = XbimTexture.Create(v);

            return new IFCMaterial("DefMat-" + prodType.Name, texture);
        }

        private static PolyMesh CreatePolyMeshFromIFC(byte[] mesh)
        {
            var polymesh = new PolyMesh();

            using var ms = new MemoryStream(mesh);
            using (var br = new BinaryReader(ms))
            {
                var version = br.ReadByte(); //stream format version
                var numVertices = br.ReadInt32();
                var numTriangles = br.ReadInt32();

                var uniqueVertices = new V3d[numVertices];

                for (var i = 0; i < numVertices; i++)
                {
                    double x = br.ReadSingle();
                    double y = br.ReadSingle();
                    double z = br.ReadSingle();
                    uniqueVertices[i] = new V3d(x, y, z);
                }

                var numFaces = br.ReadInt32();

                var firstIndexArray = new int[numTriangles + 1];
                firstIndexArray.SetByIndex((x) => x * 3);

                // Vertex
                int faceVertexIndexIter = 0;
                var faceVertexIndexArray = new int[numTriangles * 3];

                // Normals
                var faceVertexNormalIndexIter = 0;
                var faceVertexNormalIndexArray = new int[numTriangles * 3];
                var faceVertexNormalArray = new V3f[numTriangles * 3];

                for (var i = 0; i < numFaces; i++)
                {
                    var numTrianglesInFace = br.ReadInt32();
                    if (numTrianglesInFace == 0) continue;
                    var isPlanar = numTrianglesInFace > 0;
                    numTrianglesInFace = Fun.Abs(numTrianglesInFace);
                    if (isPlanar)
                    {
                        var xNormal = br.ReadPackedNormal().Normal;

                        // Normal  - 1 normal per shape

                        var normal = new V3f(xNormal.X, xNormal.Y, xNormal.Z);

                        if (normal.Abs().AllSmallerOrEqual(1e-5f))
                            Report.Line("normal is invalid!");

                        faceVertexNormalArray[faceVertexNormalIndexIter] = normal;

                        for (var j = 0; j < numTrianglesInFace; j++)
                        {
                            for (int k = 0; k < 3; k++)
                            {
                                int idx = ReadIndex(br, numVertices);

                                // Vertex
                                faceVertexIndexArray[faceVertexIndexIter] = idx;

                                // Normal index - set to only face normal
                                faceVertexNormalIndexArray[faceVertexIndexIter] = faceVertexNormalIndexIter;

                                // Vertex - Iter
                                faceVertexIndexIter++;
                            }
                        }

                        // Face - Iter
                        faceVertexNormalIndexIter++;
                    }
                    else
                    {
                        var uniqueIndices = new Dictionary<int, int>();
                        for (var j = 0; j < numTrianglesInFace; j++)
                        {
                            for (int k = 0; k < 3; k++)
                            {
                                int idx = ReadIndex(br, numVertices);
                                var xNormal = br.ReadPackedNormal().Normal;

                                // Vertex
                                faceVertexIndexArray[faceVertexIndexIter] = idx;

                                var normal = new V3f(xNormal.X, xNormal.Y, xNormal.Z);

                                if (normal.Abs().AllSmallerOrEqual(1e-5f))
                                    Report.Line("normal is invalid!!");

                                // Normal
                                faceVertexNormalArray[faceVertexNormalIndexIter] = normal;
                                faceVertexNormalIndexArray[faceVertexIndexIter] = faceVertexNormalIndexIter;

                                // Iter
                                faceVertexIndexIter++;
                                faceVertexNormalIndexIter++;
                            }
                        }
                    }
                }

                polymesh.PositionArray = uniqueVertices;
                polymesh.FirstIndexArray = firstIndexArray;
                polymesh.VertexIndexArray = faceVertexIndexArray;
                polymesh.FaceVertexAttributes[PolyMesh.Property.Normals] = faceVertexNormalIndexArray;
                polymesh.FaceVertexAttributes[-PolyMesh.Property.Normals] = faceVertexNormalArray.SubRange(0, faceVertexNormalIndexIter).ToArray();
            }
            return polymesh;
        }

        /// <summary>
        /// Reads a packed Xbim Triangle index from a stream
        /// </summary>
        /// <param name="br"></param>
        /// <param name="maxVertexCount">The size of the maximum number of vertices in the stream, i.e. the biggest index value</param>
        /// <returns></returns>
        private static int ReadIndex(BinaryReader br, int maxVertexCount)
        {
            if (maxVertexCount <= 0xFF)
                return br.ReadByte();
            if (maxVertexCount <= 0xFFFF)
                return br.ReadUInt16();
            return (int)br.ReadUInt32(); //this should never go over int32
        }
    }
}
