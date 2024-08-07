﻿using Aardvark.Base;
using Aardvark.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xbim.Common;
using Xbim.Common.Geometry;
using Xbim.Common.Metadata;
using Xbim.Common.XbimExtensions;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.UtilityResource;
using Xbim.ModelGeometry.Scene;

namespace Aardvark.Data.Ifc
{
    public class IFCParser
    {
        static readonly XbimColourMap _colourMap = new XbimColourMap();

        public static IFCData PreprocessIFC(string filePath, XbimEditorCredentials editor = null)
        {
            Dict<IfcGloballyUniqueId, IFCContent> content = null;
            Dictionary<string, IFCMaterial> materials = null;

            var model = IfcStore.Open(filePath, editor);

            model.BeginInverseCaching();
            model.BeginEntityCaching();

            if (model.GeometryStore.IsEmpty)
            {
                var context = new Xbim3DModelContext(model);
                //upgrade to new geometry representation, uses the default 3D model
                context.CreateContext();    // THIS IS COSTLY!
            }

            foreach (var modelReference in model.ReferencedModels)
            {
                // creates federation geometry contexts if needed
                Report.Line(modelReference.Name);
                if (modelReference.Model == null)
                    continue;
                if (!modelReference.Model.GeometryStore.IsEmpty)
                    continue;
                var context = new Xbim3DModelContext(modelReference.Model);
                //upgrade to new geometry representation, uses the default 3D model
                context.CreateContext();
            }

            var project = model.Instances.OfType<IIfcProject>().First().UnitsInContext;
            double projectScale = 1.0;

            if (project is Xbim.Ifc2x3.MeasureResource.IfcUnitAssignment unitAssignment)
                projectScale = unitAssignment.LengthUnitPower;
            else if (project is Xbim.Ifc4.MeasureResource.IfcUnitAssignment unitAssignment4)
                projectScale = unitAssignment4.LengthUnitPower;
            else
                Report.Line("Cannot retrieve Length Unit of IFC-Project. Use Default Unit (meters)");

            var cacheInverse = model.BeginInverseCaching();
            var cacheEntity = model.BeginEntityCaching();

            (content, materials) = ParseIFC(model);
            var hierarchy = IFCHelper.CreateHierarchy(model);

            return new IFCData(model, cacheInverse, cacheEntity, content, materials, projectScale, hierarchy);
        }

        private static (Dict<IfcGloballyUniqueId, IFCContent>, Dictionary<string, IFCMaterial>) ParseIFC(IModel model)
        {
            var excludedTypes = DefaultExclusions(model);

            var output = new Dict<IfcGloballyUniqueId, IFCContent>(true);

            var materialsByName = new Dictionary<string, IFCMaterial>();
            var materialsByStyleId = new Dictionary<int, IFCMaterial>();
            var nameToStyle = new Dictionary<string, int>();
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
                    var ifcObject = (IIfcObject)model.Instances[shapeInstance.IfcProductLabel];

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
                        materialsByName.Add(m.Name, m);
                        nameToStyle.Add(m.Name, styleId);
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
                            Report.Error("Creating PolyMesh from IFC failed! {0}", e.ToString());
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

        public static HashSet<short> DefaultExclusions(IModel model)
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
                if (e is ArgumentException arg)
                {
                    materials.Add(new IFCMaterial(arg.ParamName, null)); // material name only - no visual description (Null)
                }
                else
                    Report.Error("Associated Material Creation Failed! {0}", e.Message);
            }
        }

        public static IFCMaterial GetMaterial(IIfcMaterial mat)
        {
            var matDefRep = mat.HasRepresentation.FirstOrDefault() ?? throw new ArgumentException("No repesentation", mat.Name.ToString());
            var rep = matDefRep.Representations.First();
            var repItem = (IIfcStyledItem)rep.Items.First();

            IIfcSurfaceStyle surfStyle;
            if (repItem.Styles.First() is Xbim.Ifc4.PresentationAppearanceResource.IfcSurfaceStyle s)
            {
                surfStyle = s;
            }
            else
            {
                var presStyleAss = (IIfcPresentationStyleAssignment)repItem.Styles.First();
                surfStyle = presStyleAss.SurfaceStyles.FirstOrDefault();
            }

            if (surfStyle == null) throw new ArgumentException("No surfStyle", mat.Name.ToString());

            var material = XbimTexture.Create(surfStyle);

            var properties = mat.HasProperties.SelectMany(a => a.Properties);

            Func<string, double> getProperty = (string input) => {
                var thermal = properties.First(x => (string)x.Name == input);
                if (thermal is IIfcPropertySingleValue value)
                {
                    return (double)value.NominalValue.Value;
                }
                else return 0.0;
            };

            var thermal = getProperty("ThermalConductivity");
            var capacity = getProperty("SpecificHeatCapacity");
            var density = getProperty("MassDensity");

            return new IFCMaterial(mat.Name.ToString(), material, thermal, capacity, density);
        }

        public static IFCMaterial GetMaterialByStyle(IModel model, int styleId)
        {
            var sStyle = model.Instances[styleId] as IIfcSurfaceStyle;  
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
