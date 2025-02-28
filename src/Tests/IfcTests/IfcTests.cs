using Aardvark.Base;
using Aardvark.Data.Ifc;
using Aardvark.Geometry;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

using Xbim.Common;
using Xbim.Common.Enumerations;
using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.MeasureResource;
using Xbim.IO;

namespace Aardvark.Data.Tests.Ifc
{
    [TestFixture]
    public static class LoadingTest
    {
        private static void LoadEmbeddedData(string inputString, Action<string> action)
        {
            // necessary to run tests on github build servers
            var asm = Assembly.GetExecutingAssembly();
            var name = Regex.Replace(asm.ManifestModule.Name, @"\.(exe|dll)$", "", RegexOptions.IgnoreCase);
            var path = Regex.Replace(inputString, @"(\\|\/)", ".");
            using var stream = asm.GetManifestResourceStream(name + "." + path) ?? throw new Exception($"Cannot open resource stream with name {path}");
            var filePath = Path.ChangeExtension(Path.GetRandomFileName(), ".ifc");
            try
            {
                using var memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);
                var data = memoryStream.ToArray();
                File.WriteAllBytes(filePath, data);
                action.Invoke(filePath);
            }
            finally
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
        }

        [Test]
        public static void LoadPrimitive()
        {
            LoadEmbeddedData(@"data\surface-model.ifc", (filePath) => {
                var parsed = IFCParser.PreprocessIFC(filePath);
                Assert.AreEqual(1, parsed.Materials.Count);
            });
        }

        [Test]
        public static void LoadWall()
        {
            LoadEmbeddedData(@"data\wall.ifc", (filePath) => {
                var parsed = IFCParser.PreprocessIFC(filePath);
                Assert.AreEqual(4, parsed.Materials.Count);
            });
        }

        [Test]
        public static void LoadSlab()
        {
            LoadEmbeddedData(@"data\slab.ifc", (filePath) => {
                var parsed = IFCParser.PreprocessIFC(filePath);
                Assert.AreEqual(2, parsed.Materials.Count);
            });
        }

        [Test]
        public static void MultiThreadKiller()
        {
            // NOTE: In IFCParser -> change MaxThreads!
            //  var context = new Xbim3DModelContext(model);
            //  context.MaxThreads = 1; <- SINGLE-Thread works!

            LoadEmbeddedData(@"data\simple_scene.ifc", (filePath) => {
                var parsed = IFCParser.PreprocessIFC(filePath, geometryEngine: Xbim.Geometry.Abstractions.XGeometryEngineVersion.V5, singleThreading: false);
                Assert.AreEqual(5, parsed.Materials.Count);
            });

            LoadEmbeddedData(@"data\simple_scene.ifc", (filePath) => {
                var parsed = IFCParser.PreprocessIFC(filePath, geometryEngine: Xbim.Geometry.Abstractions.XGeometryEngineVersion.V5, singleThreading: true);
                Assert.AreEqual(5, parsed.Materials.Count);
            });

            LoadEmbeddedData(@"data\simple_scene.ifc", (filePath) => {
                var parsed = IFCParser.PreprocessIFC(filePath, geometryEngine: Xbim.Geometry.Abstractions.XGeometryEngineVersion.V6, singleThreading: false);
                Assert.AreEqual(5, parsed.Materials.Count);
            });

            LoadEmbeddedData(@"data\simple_scene.ifc", (filePath) => {
                var parsed = IFCParser.PreprocessIFC(filePath, geometryEngine: Xbim.Geometry.Abstractions.XGeometryEngineVersion.V6, singleThreading: true);
                Assert.AreEqual(5, parsed.Materials.Count);
            });
        }

        [Test]
        public static void LoadMaterial()
        {
            LoadEmbeddedData(@"data\test_Material.ifc", (filePath) => {
                var parsed = IFCParser.PreprocessIFC(filePath, geometryEngine: Xbim.Geometry.Abstractions.XGeometryEngineVersion.V6, singleThreading: false);
                var carbonMaterial = parsed.Materials["Carbon"];
                Assert.IsTrue(carbonMaterial.ThermalConductivity.ApproximateEquals(100.0) && carbonMaterial.MassDensity.ApproximateEquals(1234.0));
            });
        }

        [Test]
        public static void LoadIfc4x3()
        {
            LoadEmbeddedData(@"data\Viadotto Acerno_ifc43.ifc", (filePath) => {
                var parsed = IFCParser.PreprocessIFC(filePath, singleThreading: true);
                Assert.AreEqual(26, parsed.Materials.Count);
            });
        }
    }

    [TestFixture]
    public static class ExportTest
    {
        private static readonly XbimEditorCredentials AardvarkTestCredentials = new() {
            ApplicationDevelopersName = "Aardvark-Developer",
            ApplicationFullName = "IfcExportTest",
            ApplicationIdentifier = "Identifier",
            ApplicationVersion = "1.0",
            EditorsFamilyName = "Family",
            EditorsGivenName = "Name",
            EditorsOrganisationName = "Organisation"
        };
        
        [Test]
        public static void PropertyTest()
        {
            using var model = IfcStore.Create(AardvarkTestCredentials, XbimSchemaVersion.Ifc4, XbimStoreType.InMemoryModel);

            model.CreateMinimalProject();

            using (var txn = model.BeginTransaction("Create Wall Properties"))
            {
                var site = model.Instances.OfType<IIfcSite>().FirstOrDefault();

                //create simple object and use lambda initializer to set the name
                var wall = (Xbim.Ifc4.SharedBldgElements.IfcWall) model.Factory().Wall(w => w.Name = "The very first wall"); // <- remove cast (necessary for PurgePropertySet, SetPropertySingleValue...)
                site.AddElement(wall);

                var prop = new Dictionary<string, object>
                        {
                            { "p1", "A" },
                            { "p2", 123.15 },
                            { "p3", false }
                        };

                // create set
                var setA = model.CreatePropertySet("SetA", prop);

                // attach set
                wall.AddPropertySet(setA);

                // remove whole set and clean up
                wall.PurgePropertySet("SetA");

                // re-use set
                wall.CreateAttachPropertySet("SetB", prop);

                // create single prop
                wall.SetPropertySingleValue("SetC", "my_prop1", new IfcText("start"));
                wall.SetPropertySingleValue("SetC", "my_prop2", new IfcText("untouched"));
                wall.SetPropertySingleValue("SetC", "my_prop3", new IfcText("remove-me-later"));

                // update prop
                wall.SetPropertySingleValue("SetC", "my_prop1", new IfcText("override"));

                // removal prop
                wall.PurgePropertySingleValue("SetC", "my_prop3");

                txn.Commit();
            }

            Assert.IsEmpty(model.ValidateModel());
            Assert.IsTrue(model.Instances.OfType<Xbim.Ifc4.SharedBldgElements.IfcWall>().First().PropertySets.Count() == 2); // Set-C with "untouched" and "overrid" AND Set-B
            model.SaveAs("test_Properties.ifc");
        }

        [Test]
        public static void WallTest()
        {
            using var model = IfcStore.Create(AardvarkTestCredentials, XbimSchemaVersion.Ifc4, XbimStoreType.InMemoryModel);

            model.CreateMinimalProject();
            

            using (var txn = model.BeginTransaction("Create Wall"))
            {
                var factory = model.Factory();

                var site = model.Instances.OfType<IIfcSite>().FirstOrDefault();

                var wall = factory.Wall(w =>
                {
                    w.Name = "Test wall";
                    w.ObjectPlacement = model.CreateLocalPlacement(V3d.Zero);  // can be applied later on...
                });
                site.AddElement(wall);

                var box = new Box3d(V3d.Zero, new V3d(200, 100, 500)); // mm

                //// IfcPresentationLayerAssignment is required for CAD presentation
                //var cadLayer = model.New<IfcPresentationLayerAssignment>(layer =>
                //{
                //    layer.Name = "Building Element";
                //    //layer.AssignedItems.Add(boxShape);
                //});
                var cadLayer = model.CreateLayer("Building Element");

                var boxShape = model.CreateShapeRepresentationSolidBox(box, cadLayer);

                var repItem = boxShape.Items.First(); // retrieve body of shape

                var defaultStyle = model.CreateSurfaceStyle(C3d.Yellow);

                // create visual style
                repItem.CreateStyleItem(defaultStyle);

                wall.Representation =
                    factory.ProductDefinitionShape(definition =>
                    {
                        definition.Name = "ShapeName";
                        definition.Description = "ShapeDescription";
                        definition.Representations.AddRange([
                            boxShape,
                            model.CreateShapeRepresentationBoundingBox(box),
                            model.CreateShapeRepresentationSurface(Plane3d.XPlane, new Polygon2d(box.YZ.Translated(-box.YZ.Center).ComputeCornersCCW())),
                            model.CreateShapeRepresentationSurface(Plane3d.YPlane, new Polygon2d(box.XZ.Translated(-box.XZ.Center).ComputeCornersCCW())),
                            model.CreateShapeRepresentationSurface(Plane3d.ZPlane, new Polygon2d(box.XY.Translated(-box.XY.Center).ComputeCornersCCW())),
                        ]);
                    });

                txn.Commit();
            }

            Assert.IsEmpty(model.ValidateModel());
            model.SaveAs("test_TestWall.ifc");
        }

        [Test]
        public static void WallTest2X3()
        {
            using var model = IfcStore.Create(AardvarkTestCredentials, XbimSchemaVersion.Ifc2X3, XbimStoreType.InMemoryModel);

            var factory = new EntityCreator(model);

            using var txnInit = model.BeginTransaction("Init Project");

            var project = factory.Project(p => p.Name = "TestProject");

            project.Initialize(ProjectUnits.SIUnitsUK);

            // add site
            var site = factory.Site(w => w.Name = "Site1");

            switch (project, site)
            {
                case (Xbim.Ifc2x3.Kernel.IfcProject p, Xbim.Ifc2x3.ProductExtension.IfcSite si):
                    p.AddSite(si); break;
                case (Xbim.Ifc4.Kernel.IfcProject p, Xbim.Ifc4.ProductExtension.IfcSite si):
                    p.AddSite(si); break;
                case (Xbim.Ifc4x3.Kernel.IfcProject p, Xbim.Ifc4x3.ProductExtension.IfcSite si):
                    p.AddSite(si); break;
                default: throw new NotSupportedException($"Schema {model.SchemaVersion} does not provide AddPropertySet or mixed obj and typeobject!");
            };

            var proxy = factory.BuildingElementProxy(w =>
            {
                w.Name = "Proxy";
                w.ObjectPlacement = factory.LocalPlacement(p =>
                {
                    p.RelativePlacement = factory.Axis2Placement3D(a =>
                    {
                        a.Location = factory.CartesianPoint(c => c.SetXYZ(0, 0, 0));
                        a.RefDirection = factory.Direction(rd => rd.SetXYZ(1.0, 0, 0)); // default x-axis
                        a.Axis = factory.Direction(rd => rd.SetXYZ(0, 0, 1.0));         // default z-axis
                    });
                });
            });

            factory.RelContainedInSpatialStructure(relSe => {
                relSe.RelatingStructure = site;
                relSe.RelatedElements.Add(proxy);
            });

            site.AddElement(proxy);

            var rectProf = factory.RectangleProfileDef(p =>
            {
                p.ProfileName = "RectArea";
                p.ProfileType = IfcProfileTypeEnum.AREA;
                p.XDim = new IfcPositiveLengthMeasure(10);
                p.YDim = new IfcPositiveLengthMeasure(10);
                p.Position = factory.Axis2Placement2D(a => {
                    a.Location = factory.CartesianPoint(c => c.SetXY(0, 0));
                    a.RefDirection = factory.Direction(rd => rd.SetXY(1.0, 0)); // default x-axis
                });
            });

            var item = factory.ExtrudedAreaSolid(solid =>
            {
                solid.Position = factory.Axis2Placement3D(a => {
                    a.Location = factory.CartesianPoint(c => c.SetXYZ(0, 0, 0));
                    a.RefDirection = factory.Direction(rd => rd.SetXYZ(1.0, 0, 0)); // default x-axis
                    a.Axis = factory.Direction(rd => rd.SetXYZ(0, 0, 1.0));         // default z-axis
                });
                solid.Depth = new IfcPositiveLengthMeasure(10);
                solid.ExtrudedDirection = factory.Direction(rd => rd.SetXYZ(0, 0, 1.0));
                solid.SweptArea = rectProf;
            });

            var boxShape = factory.ShapeRepresentation(s =>
            {
                s.ContextOfItems = model.Instances.OfType<IIfcGeometricRepresentationContext>().Where(c => c.ContextType == "Model").First();
                s.RepresentationType = "SweptSolid";
                s.RepresentationIdentifier = "Body";
                s.Items.Add(item);
            });

            var style = factory.SurfaceStyle(style =>
            {
                var defaultStyle = factory.SurfaceStyleShading(l =>
                {
                    l.SurfaceColour = factory.ColourRgb(rgb =>
                    {
                        rgb.Red = 1.0;
                        rgb.Green = 0.0;
                        rgb.Blue = 0.0;
                    });
                    l.Transparency = new IfcNormalisedRatioMeasure(0.0); // [0 = opaque .. 1 = transparent]
                });

                style.Side = IfcSurfaceSide.BOTH;
                style.Styles.Add(defaultStyle);
            });

            var s = factory.StyledItem(styleItem => {
                styleItem.Styles.Add(style);    // <---- THIS style is empty in IFC2x3
                styleItem.Item = item;
            });

            proxy.Representation = factory.ProductDefinitionShape(definition =>
            {
                definition.Name = "ShapeName";
                definition.Description = "ShapeDescription";
                definition.Representations.Add(boxShape);
            });

            txnInit.Commit();

            var validator = new Xbim.Common.ExpressValidation.Validator() {
                CreateEntityHierarchy = true,
                ValidateLevel = ValidationFlags.All
            };

            var result = validator.Validate(model.Instances);

            result.ForEach(error =>
            {
                Report.Line(error.Message + " with " + error.Details.Count());
                error.Details.ForEach(detail => Report.Line(detail.IssueSource + " " + detail.IssueType));
            });

            //// OUTPUT: 
            //  0: Entity #49=IFCSTYLEDITEM(#40,(),$); has validation failures. with 1
            //  0: IfcStyledItem.WR11 EntityWhereClauses

            model.SaveAs("test_TestWall2x3.ifc");
            Assert.IsEmpty(result);
        }


        [Test]
        public static void GeometryTest()
        {
            using var model = IfcStore.Create(AardvarkTestCredentials, XbimSchemaVersion.Ifc4, XbimStoreType.InMemoryModel);

            model.CreateMinimalProject();

            using (var txn = model.BeginTransaction("Create Geometries"))
            {
                var factory = model.Factory();
                var site = model.Instances.OfType<IIfcSite>().FirstOrDefault();

                var yellowStyle = model.CreateSurfaceStyle(C3d.Yellow);

                var layer = model.CreateLayerWithStyle("Layer green styled", [model.CreateSurfaceStyle(C3d.Green)]);

                var mesh = PolyMeshPrimitives.PlaneXY(new V2d(1000.0));
                site.AddElement(factory.Wall(c => c.Name = "Mesh1a").CreateAttachRepresentation(new V3d(0, 0, 0), mesh, C4d.Red, null, null, true));
                site.AddElement(factory.Wall(c => c.Name = "Mesh1b").CreateAttachRepresentation(new V3d(1000, 0, 0), mesh, null, yellowStyle, layer, true));
                site.AddElement(factory.Wall(c => c.Name = "Mesh1c").CreateAttachRepresentation(new V3d(2000, 0, 0), mesh, null, null, layer, true));

                var mesh2 = PolyMeshPrimitives.Sphere(10, 500, C4b.Blue);
                site.AddElement(factory.Wall(c => c.Name = "Mesh2a").CreateAttachRepresentation(new V3d(-1000, 0, 0), mesh2, null, null, layer, false));
                site.AddElement(factory.Wall(c => c.Name = "Mesh2b").CreateAttachRepresentation(new V3d(-2000, 0, 0), mesh2, null, yellowStyle, layer, false));

                var mesh3 = PolyMeshPrimitives.Box(new Box3d(V3d.Zero, new V3d(500.0, 1500.0, 500.0)), C4b.Brown);
                site.AddElement(factory.Wall(c => c.Name = "Mesh3a").CreateAttachRepresentation(new V3d(4000, 0, 0), mesh3, null, yellowStyle, layer));

                txn.Commit();
            }

            Assert.IsEmpty(model.ValidateModel());
            model.SaveAs("test_Geometries.ifc");
        }

        [Test]
        public static void LightTest()
        {
            using var model = IfcStore.Create(AardvarkTestCredentials, XbimSchemaVersion.Ifc4, XbimStoreType.InMemoryModel);

            model.CreateMinimalProject();

            using (var txn = model.BeginTransaction("Create Light"))
            {
                var site = model.Instances.OfType<IIfcSite>().FirstOrDefault();

                var layer = model.CreateLayerWithStyle("Layer green styled", [model.CreateSurfaceStyle(C3d.Green)]);

                var shiftVec = new V3d(0, 300, 0);

                var baseShape = model.CreateShapeRepresentationSolidBox(new Box3d(V3d.Zero, new V3d(200, 100, 500)), layer);

                var repMap = baseShape.CreateRepresentationMap();

                var trafo1 = Trafo3d.RotationYInDegrees(-10);
                var trafo2 = Trafo3d.RotationYInDegrees(45);
                var trafo3 = Trafo3d.RotationYInDegrees(90);
                var trafo4 = Trafo3d.RotationYInDegrees(125);
                var trafo5 = Trafo3d.RotationYInDegrees(180);
                var trafo6 = Trafo3d.RotationYInDegrees(270);

                var prop = new Dictionary<string, object>
                        {
                            { "p1", "A" },
                            { "p2", 123.15 },
                            { "p3", false }
                        };

                var generalInfo = model.CreatePropertySet("General Information", prop);

                var lightType = model.CreateLightType(IfcLightFixtureTypeEnum.POINTSOURCE, [repMap], [generalInfo]);
                
                // instantiate via light-type
                var li = lightType.Instantiate("Empty_Light_fromtype", model.CreateLocalPlacement(-shiftVec), Trafo3d.RotationZInDegrees(45), IfcLightFixtureTypeEnum.POINTSOURCE);
                li.AddPropertySet(model.Pset_LightFixtureTypeCommon("reference", 1, 1000, 20, 50, 20, 33, "working", "hanging", "ceiling"));
                li.Qto_LightFixtureBaseQuantities(50);
                site.AddElement(li);

                // attached property sets
                site.AddElement(model.CreateLightEmpty("Empty_Light", model.CreateLocalPlacement(V3d.Zero), repMap.Instantiate(trafo1)).AttachPropertySet(generalInfo));
                site.AddElement(model.CreateLightAmbient("Ambient_Light", C3d.Red, model.CreateLocalPlacement(shiftVec), repMap.Instantiate(trafo2)).AttachPropertySet(generalInfo));

                // properties linked via light-type
                site.AddElement(model.CreateLightDirectional("Directional_Light", C3d.Blue, V3d.ZAxis, model.CreateLocalPlacement(2*shiftVec), repMap.Instantiate(trafo3)).LinkToType(lightType));

                site.AddElement(model.CreateLightPositional("Positional_Light", C3d.Green, new V3d(0, 0, 2000), 150, V3d.Zero, model.CreateLocalPlacement(3 * shiftVec), repMap.Instantiate(trafo4)));
                site.AddElement(model.CreateLightSpot("Spot_Light", C3d.Yellow, new V3d(0, 0, 1000), V3d.ZAxis, 100, V3d.Zero, 50, 20, model.CreateLocalPlacement(4 * shiftVec), repMap.Instantiate(trafo5)));
                
                var dist = model.CreateLightIntensityDistribution(IfcLightDistributionCurveEnum.TYPE_C, []);
                site.AddElement(model.CreateLightGoniometric("Goniometric_Light", C3d.Orange, new V3d(0, 0, 3000), 3500, 1000, dist, model.CreateLocalPlacement(5 * shiftVec), repMap.Instantiate(trafo6)));

                txn.Commit();
            }

            Assert.IsEmpty(model.ValidateModel());
            model.SaveAs("test_Lights.ifc");
        }

        [Test]
        public static void LightTest2x3()
        {
            using var model = IfcStore.Create(AardvarkTestCredentials, XbimSchemaVersion.Ifc2X3, XbimStoreType.InMemoryModel);

            model.CreateMinimalProject();

            using (var txn = model.BeginTransaction("Create Light"))
            {
                var site = model.Instances.OfType<IIfcSite>().FirstOrDefault();

                var layer = model.CreateLayerWithStyle("Layer green styled", [model.CreateSurfaceStyle(C3d.Green)]);

                var shiftVec = new V3d(0, 300, 0);

                var baseShape = model.CreateShapeRepresentationSolidBox(new Box3d(V3d.Zero, new V3d(200, 100, 500)), layer);

                var repMap = baseShape.CreateRepresentationMap();

                var trafo1 = Trafo3d.RotationYInDegrees(-10);
                var trafo2 = Trafo3d.RotationYInDegrees(45);
                var trafo3 = Trafo3d.RotationYInDegrees(90);
                var trafo4 = Trafo3d.RotationYInDegrees(125);
                var trafo5 = Trafo3d.RotationYInDegrees(180);
                var trafo6 = Trafo3d.RotationYInDegrees(270);

                var prop = new Dictionary<string, object> {
                    { "p1", "A" },
                    { "p2", 123.15 },
                    { "p3", false }
                };

                var generalInfo = model.CreatePropertySet("General Information", prop);

                var lightType = model.CreateLightType(IfcLightFixtureTypeEnum.POINTSOURCE, [repMap], [generalInfo]);

                // instantiate via light-type
                var li = lightType.InstantiateIFC2x3("Empty_Light_fromtype", model.CreateLocalPlacement(-shiftVec), Trafo3d.RotationZInDegrees(45), IfcLightFixtureTypeEnum.POINTSOURCE);
                li.AddPropertySet(model.Pset_LightFixtureTypeCommon("reference", 1, 1000, 20, 50, 20, 33, "working", "hanging", "ceiling"));
                li.Qto_LightFixtureBaseQuantities(50);
                site.AddElement(li);

                // attached property sets
                site.AddElement(model.CreateLightEmptyIFC2x3("Empty_Light", model.CreateLocalPlacement(V3d.Zero), repMap.Instantiate(trafo1)).AttachPropertySet(generalInfo));
                site.AddElement(model.CreateLightAmbientIFC2x3("Ambient_Light", C3d.Red, model.CreateLocalPlacement(shiftVec), repMap.Instantiate(trafo2)).AttachPropertySet(generalInfo));

                // properties linked via light-type
                site.AddElement(model.CreateLightDirectionalIFC2x3("Directional_Light", C3d.Blue, V3d.ZAxis, model.CreateLocalPlacement(2 * shiftVec), repMap.Instantiate(trafo3))); 
                // .LinkToType(lightType) <- Only maximum of one relationship to an underlying type (by an IfcRelDefinesByType relationship) should be given for an object instance. In case for IFC2x3 an ObjectType is created by default!

                site.AddElement(model.CreateLightPositionalIFC2x3("Positional_Light", C3d.Green, new V3d(0, 0, 2000), 150, V3d.Zero, model.CreateLocalPlacement(3 * shiftVec), repMap.Instantiate(trafo4)));
                site.AddElement(model.CreateLightSpotIFC2x3("Spot_Light", C3d.Yellow, new V3d(0, 0, 1000), V3d.ZAxis, 100, V3d.Zero, 50, 20, model.CreateLocalPlacement(4 * shiftVec), repMap.Instantiate(trafo5)));

                var dist = model.CreateLightIntensityDistribution(IfcLightDistributionCurveEnum.TYPE_C, []);
                site.AddElement(model.CreateLightGoniometricIFC2x3("Goniometric_Light", C3d.Orange, new V3d(0, 0, 3000), 3500, 1000, dist, model.CreateLocalPlacement(5 * shiftVec), repMap.Instantiate(trafo6)));

                txn.Commit();
            }

            Assert.IsEmpty(model.ValidateModel());
            model.SaveAs("test_Lights2x3.ifc");
        }

        [Test]
        public static void MaterialTest()
        {
            using var model = IfcStore.Create(AardvarkTestCredentials, XbimSchemaVersion.Ifc4, XbimStoreType.InMemoryModel);

            var massDensity = 1234.0;
            var thermalConductivity = 100;

            model.CreateMinimalProject();

            using (var txn = model.BeginTransaction("Create Slab with Material"))
            {
                var factory = model.Factory();

                var site = model.Instances.OfType<IIfcSite>().FirstOrDefault();

                // MATERIAL
                var material = (Xbim.Ifc4.MaterialResource.IfcMaterial) factory.Material(m => m.Name = "Carbon"); // <- remove cast!
                material.CreateAttachPsetMaterialCommon(98.7654, 0.54, massDensity);
                material.CreateAttachPsetMaterialThermal(thermalConductivity, 500, 99, -10);
                material.CreateAttachStyledRepresentation(C3d.Magenta);

                var materialLayerWidth = 300;
                // NOTE: box.SizeZ must be the layer-thickness
                var box = new Box3d(V3d.Zero, new V3d(100.0, 100.0, materialLayerWidth));

                var shape = model.CreateShapeRepresentationSolidBox(box); // extrusion along z-axis

                var slab = factory.Slab(c => {
                    c.Name = "Mesh4";
                    c.Representation = factory.ProductDefinitionShape(r => r.Representations.Add(shape));
                    c.ObjectPlacement = model.CreateLocalPlacement(new V3d(500, 500, 500));
                });
                site.AddElement(slab);

                // Link Material via RelAssociatesMaterial
                factory.RelAssociatesMaterial(mat =>
                {
                    // Material Layer Set Usage (HAS TO BE MANUALLY SYNCHED!)
                    IIfcMaterialLayerSetUsage usage = factory.MaterialLayerSetUsage(u =>
                    {
                        u.DirectionSense = IfcDirectionSenseEnum.NEGATIVE;
                        u.LayerSetDirection = IfcLayerSetDirectionEnum.AXIS3;
                        u.OffsetFromReferenceLine = 0;
                        u.ForLayerSet = factory.MaterialLayerSet(set =>
                        {
                            set.LayerSetName = "Carbon Layer Set";
                            set.MaterialLayers.Add(factory.MaterialLayer(layer =>
                            {
                                layer.Name = "Layer1";
                                layer.Material = material;
                                layer.LayerThickness = materialLayerWidth;
                                layer.IsVentilated = false;
                                layer.Category = "Core";
                            }));
                        });
                    });

                    mat.Name = "RelMat";
                    mat.RelatingMaterial = usage;
                    mat.RelatedObjects.Add(slab);
                });

                txn.Commit();
            }

            Assert.IsEmpty(model.ValidateModel());

            var mat = model.Instances.OfType<IIfcMaterial>().First();

            var myMaterial = IFCParser.GetMaterial(mat);
            Assert.IsTrue(myMaterial.MassDensity == massDensity);
            Assert.IsTrue(myMaterial.ThermalConductivity == thermalConductivity);

            // Cast to IIfcMaterial results into invalid null values for the first 3 properties in .NET8.0 -> resolved with xbim issue #595 in xbim.essentials 6.0.493
            var properties = mat.HasProperties.SelectMany(a => a.Properties).ToArray(); 
            properties.ForEach(Assert.IsNotNull);

            model.SaveAs("test_Material.ifc");
        }

        [Test]
        public static void GridPlacementTest()
        {
            using var model = IfcStore.Create(AardvarkTestCredentials, XbimSchemaVersion.Ifc4, XbimStoreType.InMemoryModel);
            model.CreateMinimalProject();

            var mesh = PolyMeshPrimitives.Box(new Box3d(V3d.Zero, new V3d(100, 100, 1000.0)), C4b.Yellow);

            using (var txn = model.BeginTransaction("Create Grid and Groups"))
            {
                var factory = model.Factory();

                var site = model.Instances.OfType<IIfcSite>().First();

                var surfStyleGreen = model.CreateSurfaceStyle(C3d.Green);

                var localLayer = model.CreateLayer("Local placed obj");

                // Create grid axes
                var uAxes = new[] { "A", "B", "C" };
                var vAxes = new[] { "1", "2", "3" };

                // Constant offset
                var offset = 1000.0;

                var grid = model.CreateGrid("MainGrid", uAxes, vAxes, offset);
                site.AddElement(grid);

                var groups = new List<IIfcGroup>();
                var _col = -1.0;
                var _row = -1.0;

                // Create intersection points
                foreach (var uAxis in grid.UAxes)
                {
                    _col++;

                    var annotationList = new List<IIfcAnnotation>();

                    foreach (var vAxis in grid.VAxes)
                    {
                        _row++;
                        // create grid intersections
                        // commit before they can be accessed afterwards
                        IIfcVirtualGridIntersection intersection = factory.VirtualGridIntersection(i =>
                        {
                            i.IntersectingAxes.Add(uAxis);
                            i.IntersectingAxes.Add(vAxis);
                            i.OffsetDistances.AddRange([new IfcLengthMeasure(0.0), new IfcLengthMeasure(0.0)]);
                        });

                        // in this example this should match with IfcGridPalcement
                        var position = new V3d(_row % vAxes.Length, _col % uAxes.Length, 0.0) * offset;
                        site.AddElement(factory.Column(c => c.Name = "col_calc").CreateAttachRepresentation(position, mesh, C4d.Red, surfStyleGreen, localLayer));
                    }

                    // create u-groups (holding v-axis entries)
                    var group = model.CreateGroup($"Group {uAxis.AxisTag}", annotationList);
                    groups.Add(group);
                }

                // create grid-group (holding u-axis entries with sub-grouped v-axis entries)
                model.CreateGroup("Grid_1", groups);

                // Add the grid to the IFC file
                txn.Commit();
            }

            Assert.IsEmpty(model.ValidateModel());

            using (var txn2 = model.BeginTransaction("Generate Intersections"))
            {
                var site = model.Instances.OfType<IIfcSite>().First();
                var factory = model.Factory();

                var surfStyleOrange = model.CreateSurfaceStyle(C3d.Orange);

                var gridLayer = model.CreateLayer("Grid placed obj");

                var gridCheck = model.Instances.OfType<IIfcGrid>().First();
                var placements = gridCheck.UAxes.SelectMany(axis => axis.HasIntersections.Select(i => model.Factory().GridPlacement(p => p.PlacementLocation = i))).ToArray();

                foreach (var placement in placements)
                {
                    site.AddElement(factory.Column(c => c.Name = "col_grid").CreateAttachRepresentation(placement, mesh, C4d.Red, surfStyleOrange, gridLayer));
                }
                txn2.Commit();
            }

            Assert.IsEmpty(model.ValidateModel());

            model.SaveAs("test_GridPlacement.ifc");
        }

        [Test]
        public static void AnnotationTest()
        {

            static IIfcAnnotation CreateTestAnnotation(IModel model, string text, IIfcObjectPlacement placement, V3d worldPosition, IIfcPresentationLayerWithStyle layer = null)
            {
                var factory = model.Factory();

                // Anotation-Experiments https://ifc43-docs.standards.buildingsmart.org/IFC/RELEASE/IFC4x3/HTML/lexical/IfcAnnotation.htm
                return factory.Annotation(a =>
                {
                    var box = new Box3d(V3d.Zero, new V3d(200, 100, 500)); // mm

                    a.Name = "Intersection of " + text;
                    a.ObjectPlacement = placement;
                    a.Representation = factory.ProductDefinitionShape(r => {
                        r.Representations.AddRange([
                            model.CreateShapeRepresentationAnnotation2dText(text, worldPosition.XY, layer),
                            model.CreateShapeRepresentationAnnotation2dCurve([worldPosition.XY, (worldPosition.XY + new V2d(500, 750.0)), (worldPosition.XY + new V2d(1000,1000))], [[1,2,3]], layer),
                            model.CreateShapeRepresentationAnnotation3dCurve([worldPosition, (worldPosition + new V3d(500, 750.0, 100)), (worldPosition + new V3d(1000,1000, 200))], layer),
                            model.CreateShapeRepresentationAnnotation3dSurface(Plane3d.ZPlane, new Polygon2d(box.XY.Translated(worldPosition.XY - box.XY.Center).ComputeCornersCCW()), layer),
                            model.CreateShapeRepresentationAnnotation3dCross(worldPosition, V3d.YAxis, 45, 1000.0, layer),
                            //// NOT-displayed in BIMVision
                            //model.CreateShapeRepresentationAnnotation2dPoint(worldPosition.XY, layer),
                            //model.CreateShapeRepresentationAnnotation3dPoint(worldPosition, layer),
                            //model.CreateShapeRepresentationAnnotation2dArea(new Box2d(V2d.Zero, V2d.One*1000.0), layer),

                            //// broken
                            //model.CreateShapeRepresentationSurveyPoints(worldPosition.XY),
                            //model.CreateShapeRepresentationSurveyPoints(worldPosition),
                        ]);
                    });
                });
            }

            using var model = IfcStore.Create(AardvarkTestCredentials, XbimSchemaVersion.Ifc4, XbimStoreType.InMemoryModel);
            
            model.CreateMinimalProject();

            using (var txn = model.BeginTransaction("Create Grid with Annotations"))
            {
                var factory = model.Factory();
                var site = model.Instances.OfType<IIfcSite>().FirstOrDefault();

                var textStyle = model.CreateTextStyle(100, C3f.Red, C3f.Blue, "myFont");
                var curveStyle = model.CreateCurveStyle(C3d.Magenta, 100.0, 10, 20);
                var areastyle = model.CreateFillAreaStyle(C3d.Pink, 45.0, 100, curveStyle);
                var annotationLayer = model.CreateLayerWithStyle("Anotation-Layer with Styles", [curveStyle, areastyle, textStyle]);

                // Create grid axes
                var uAxes = new[] { "A", "B", "C" };
                var vAxes = new[] { "1", "2", "3" };

                // Constant offset
                var offset = 1000.0;

                var grid = model.CreateGrid("MainGrid", uAxes, vAxes, offset);
                site.AddElement(grid);

                var groups = new List<IIfcGroup>();
                var _col = -1.0;
                var _row = -1.0;

                // Create intersection points
                foreach (var uAxis in grid.UAxes)
                {
                    _col++;

                    var annotationList = new List<IIfcAnnotation>();

                    foreach (var vAxis in grid.VAxes)
                    {
                        _row++;

                        var worldPosition = new V3d(_row % vAxes.Length, _col % uAxes.Length, 0.0) * offset;

                        var annotation = CreateTestAnnotation(model, $"{uAxis.AxisTag} / {vAxis.AxisTag}", model.CreateLocalPlacement(V3d.Zero), worldPosition, annotationLayer);

                        var prop = new Dictionary<string, object>
                            {
                                { "row", uAxis.AxisTag },
                                { "col", vAxis.AxisTag },
                                { "x", worldPosition.X },
                                { "y", worldPosition.Y },
                            };

                        annotation.AddPropertySet(model.CreatePropertySet("SetA", prop));

                        annotationList.Add(annotation);

                        site.AddElement(annotation);
                    }

                    // create u-groups (holding v-axis entries)
                    var group = model.CreateGroup($"Group {uAxis.AxisTag}", annotationList);
                    groups.Add(group);
                }

                // create grid-group (holding u-axis entries with sub-grouped v-axis entries)
                model.CreateGroup("Grid_1", groups);

                txn.Commit();
            }

            Assert.IsEmpty(model.ValidateModel());

            // Save the IFC file
            model.SaveAs("test_AnnotationGrid.ifc");
        }

        [Test]
        public static void AardvarkPrimitivesTest()
        {
            using var model = IfcStore.Create(AardvarkTestCredentials, XbimSchemaVersion.Ifc4, XbimStoreType.InMemoryModel);

            model.CreateMinimalProject();

            using (var txn = model.BeginTransaction("Convertsion Test"))
            {
                V3d[] input = [V3d.Zero, V3d.One, new V3d(1,-5,30)];
                var test = model.CreateCartesianPointList3D(input);
                var output = test.ToV3d().ToArray();
                Assert.AreEqual(input, output);

                var inputLine = model.CreatePolyLine(input);
                var outputPoints = inputLine.ToV3d();
                Assert.AreEqual(input, outputPoints);

                var inputBox = Box3d.Unit;
                var ifcBox = model.Factory().BoundingBox(b => b.Set(inputBox));
                var outputBox = ifcBox.ToBox3d();
                Assert.AreEqual(inputBox, outputBox);

                var inputTrafo = Trafo3d.Translation(new V3d(10, 15, 20)) * Trafo3d.RotationXInDegrees(45);
                var ifcTrafo = model.CreateAxis2Placement3D(inputTrafo.Forward.GetModelOrigin(), inputTrafo.Forward.C0.XYZ, inputTrafo.Forward.C2.XYZ);
                var outputTrafo = ifcTrafo.ToTrafo3d();
                var ifcTrafo2 = model.CreateAxis2Placement3D(inputTrafo);
                var outputTrafo2 = ifcTrafo2.ToTrafo3d();
                Assert.IsTrue(inputTrafo.ApproximateEquals(outputTrafo, 0.000001) && inputTrafo.ApproximateEquals(outputTrafo2, 0.000001));

                var inputVec = new V3d(10, 10, 0);
                var ifcVec = model.CreateVector(inputVec);
                var outputVec = ifcVec.ToV3d();
                Assert.AreEqual(inputVec, outputVec);

                var inputColor = C3f.Azure;
                var ifcCol = model.CreateColor(inputColor);
                var outputColor = ifcCol.ToC3f();
                Assert.AreEqual(inputColor, outputColor);
            }

            Assert.IsEmpty(model.ValidateModel());
        }
    }
}