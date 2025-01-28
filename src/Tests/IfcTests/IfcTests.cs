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
using Xbim.Common.ExpressValidation;
using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.Ifc4.GeometricConstraintResource;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.MaterialResource;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.ProductExtension;
using Xbim.Ifc4.RepresentationResource;
using Xbim.Ifc4.SharedBldgElements;
using Xbim.Ifc4.Interfaces;
using Xbim.IO;

namespace Aardvark.Data.Tests.Ifc
{
    [TestFixture]
    public static class LoadingTest
    {
        private static void LoadEmbeddedData(string inputString, Action<string> action)
        {
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
        

        private static void ValidateModel(IEntityCollection instances)
        {
            var validator = new Validator()
            {
                CreateEntityHierarchy = true,
                ValidateLevel = ValidationFlags.All
            };

            var result = validator.Validate(instances);

            result.ForEach(error =>
            {
                Report.Line(error.Message + " with " + error.Details.Count());
                error.Details.ForEach(detail => Report.Line(detail.IssueSource + " " + detail.IssueType));
            });

            Assert.IsEmpty(result);
        }

        private static void InitScene(IfcStore model)
        {
            using var txnInit = model.BeginTransaction("Init Project");
            // there should always be one project in the model
            var project = model.New<IfcProject>(p => p.Name = "Project");
            // our shortcut to define basic default units
            project.Initialize(ProjectUnits.SIUnitsUK);

            // add site
            var site = model.New<IfcSite>(w => w.Name = "Site");
            project.AddSite(site);

            // add building
            var building = model.New<IfcBuilding>(b => b.Name = "Building");
            site.AddBuilding(building);

            txnInit.Commit();

            ValidateModel(model.Instances);
        }

        [Test]
        public static void PropertyTest()
        {
            using var model = IfcStore.Create(AardvarkTestCredentials, XbimSchemaVersion.Ifc4, XbimStoreType.InMemoryModel);

            InitScene(model);

            using (var txn = model.BeginTransaction("Create Wall Properties"))
            {
                var building = model.Instances.OfType<IfcBuilding>().First();

                //create simple object and use lambda initializer to set the name
                var wall = model.New<IfcWall>(w => w.Name = "The very first wall");
                building.AddElement(wall);

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

            ValidateModel(model.Instances);
            Assert.IsTrue(model.Instances.OfType<IfcWall>().First().PropertySets.Count() == 2); // Set-C with "untouched" and "overrid" AND Set-B
            model.SaveAs("test_Properties.ifc");                                                                                    // 
        }

        [Test]
        public static void WallTest()
        {
            using var model = IfcStore.Create(AardvarkTestCredentials, XbimSchemaVersion.Ifc4, XbimStoreType.InMemoryModel);

            InitScene(model);

            using (var txn = model.BeginTransaction("Create Wall"))
            {
                var building = model.Instances.OfType<IfcBuilding>().First();

                var wall = model.New<IfcWall>(w =>
                {
                    w.Name = "Test wall";
                    w.ObjectPlacement = model.CreateLocalPlacement(V3d.Zero);  // can be applied later on...
                });
                building.AddElement(wall);

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

                var defaultStyle = model.CreateSurfaceStyle(C3d.OrangeRed);

                // create visual style
                repItem.CreateStyleItem(defaultStyle);

                wall.Representation =
                    model.New<IfcProductDefinitionShape>(definition =>
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

            ValidateModel(model.Instances);
            model.SaveAs("test_TestWall.ifc");
        }

        [Test]
        public static void GeometryTest()
        {
            using var model = IfcStore.Create(AardvarkTestCredentials, XbimSchemaVersion.Ifc4, XbimStoreType.InMemoryModel);

            InitScene(model);

            using (var txn = model.BeginTransaction("Create Geometries"))
            {
                var building = model.Instances.OfType<IfcBuilding>().First();

                var yellowStyle = model.CreateSurfaceStyle(C3d.Yellow);

                var layer = model.CreateLayerWithStyle("Layer green styled", [model.CreateSurfaceStyle(C3d.Green)]);

                var mesh = PolyMeshPrimitives.PlaneXY(new V2d(1000.0));
                var wall = building.CreateAttachElement<IfcWall>("Mesh1a", new V3d(0, 0, 0), mesh, null, null, true);       // Red (create default-material)
                var wall1 = building.CreateAttachElement<IfcWall>("Mesh1b", new V3d(1000, 0, 0), mesh, yellowStyle, layer, true);           // Yellow from style
                var wall2 = building.CreateAttachElement<IfcWall>("Mesh1c", new V3d(2000, 0, 0), mesh, null, layer, true);      // Green from layer

                var mesh2 = PolyMeshPrimitives.Sphere(10, 500, C4b.Blue);
                var window = building.CreateAttachElement<IfcWindow>("Mesh2a", new V3d(-1000, 0, 0), mesh2, null, layer, false); // Blue from mesh
                var window2 = building.CreateAttachElement<IfcWindow>("Mesh2b", new V3d(-2000, 0, 0), mesh2, yellowStyle, layer, false);    // Yellow from style

                var mesh3 = PolyMeshPrimitives.Box(new Box3d(V3d.Zero, new V3d(500.0, 1500.0, 500.0)), C4b.Brown);
                var door = building.CreateAttachElement<IfcDoor>("Mesh3a", new V3d(4000, 0, 0), mesh3, yellowStyle, layer);

                txn.Commit();
            }

            ValidateModel(model.Instances);
            model.SaveAs("test_Geometries.ifc");
        }

        [Test]
        public static void LightTest()
        {
            using var model = IfcStore.Create(AardvarkTestCredentials, XbimSchemaVersion.Ifc4, XbimStoreType.InMemoryModel);

            InitScene(model);

            using (var txn = model.BeginTransaction("Create Light"))
            {
                var building = model.Instances.OfType<IfcBuilding>().First();

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

                // attached property sets
                building.AddElement(model.CreateLightEmpty("Empty_Light", model.CreateLocalPlacement(V3d.Zero), repMap.Instantiate(trafo1)).AttachPropertySet(generalInfo));
                building.AddElement(model.CreateLightAmbient("Ambient_Light", C3d.Red, model.CreateLocalPlacement(shiftVec), repMap.Instantiate(trafo2)).AttachPropertySet(generalInfo));

                // properties linked via light-type
                building.AddElement(model.CreateLightDirectional("Directional_Light", C3d.Blue, V3d.ZAxis, model.CreateLocalPlacement(2*shiftVec), repMap.Instantiate(trafo3)).LinkToType(lightType));

                building.AddElement(model.CreateLightPositional("Positional_Light", C3d.Green, new V3d(0, 0, 2000), 150, V3d.Zero, model.CreateLocalPlacement(3 * shiftVec), repMap.Instantiate(trafo4)));
                building.AddElement(model.CreateLightSpot("Spot_Light", C3d.Yellow, new V3d(0, 0, 1000), V3d.ZAxis, 100, V3d.Zero, 50, 20, model.CreateLocalPlacement(4 * shiftVec), repMap.Instantiate(trafo5)));
                
                var dist = model.CreateLightIntensityDistribution(IfcLightDistributionCurveEnum.TYPE_C, []);
                building.AddElement(model.CreateLightGoniometric("Gonometric_Light", C3d.Orange, new V3d(0, 0, 3000), 3500, 1000, dist, model.CreateLocalPlacement(5 * shiftVec), repMap.Instantiate(trafo6)));

                txn.Commit();
            }

            ValidateModel(model.Instances);
            model.SaveAs("test_Lights.ifc");
        }

        [Test]
        public static void MaterialTest()
        {
            using var model = IfcStore.Create(AardvarkTestCredentials, XbimSchemaVersion.Ifc4, XbimStoreType.InMemoryModel);

            var massDensity = 1234.0;
            var thermalConductivity = 100;

            InitScene(model);
            using (var txn = model.BeginTransaction("Create Slab with Material"))
            {
                var building = model.Instances.OfType<IfcBuilding>().First();

                // MATERIAL
                var material = model.New<IfcMaterial>(m => m.Name = "Carbon");
                material.CreateAttachPsetMaterialCommon(98.7654, 0.54, massDensity);
                material.CreateAttachPsetMaterialThermal(thermalConductivity, 500, 99, -10);
                material.CreateAttachPresentation(C3d.Magenta);

                var slab = building.CreateAttachSlab("Mesh4", null, material);

                txn.Commit();
            }

            ValidateModel(model.Instances);

            var mat = model.Instances.OfType<IfcMaterial>().First();

            var myMaterial = IFCParser.GetMaterial(mat);
            Assert.IsTrue(myMaterial.MassDensity == massDensity);
            Assert.IsTrue(myMaterial.ThermalConductivity == thermalConductivity);

            // Cast to IIfcMaterial results into invalid null values for the first 3 properties in .NET8.0 -> resolved with xbim issue #595 in xbim.essentials 6.0.493
            var properties = ((Xbim.Ifc4.Interfaces.IIfcMaterial) mat).HasProperties.SelectMany(a => a.Properties).ToArray(); 
            properties.ForEach(Assert.IsNotNull);

            model.SaveAs("test_Material.ifc");
        }

        [Test]
        public static void GridPlacementTest()
        {
            using var model = IfcStore.Create(AardvarkTestCredentials, XbimSchemaVersion.Ifc4, XbimStoreType.InMemoryModel);
            InitScene(model);

            var mesh = PolyMeshPrimitives.Box(new Box3d(V3d.Zero, new V3d(100, 100, 1000.0)), C4b.Yellow);

            using (var txn = model.BeginTransaction("Create Grid and Groups"))
            {
                var site = model.Instances.OfType<IfcSite>().First();

                var surfStyleGreen = model.CreateSurfaceStyle(C3d.Green);

                var localLayer = model.CreateLayer("Local placed obj");

                // Create grid axes
                var uAxes = new[] { "A", "B", "C" };
                var vAxes = new[] { "1", "2", "3" };

                // Constant offset
                var offset = 1000.0;

                var grid = model.CreateGrid("MainGrid", uAxes, vAxes, offset);
                site.AddElement(grid);

                var groups = new List<IfcGroup>();
                var _col = -1.0;
                var _row = -1.0;

                // Create intersection points
                foreach (var uAxis in grid.UAxes)
                {
                    _col++;

                    var annotationList = new List<IfcAnnotation>();

                    foreach (var vAxis in grid.VAxes)
                    {
                        _row++;
                        // create grid intersections
                        // commit before they can be accessed afterwards
                        IfcVirtualGridIntersection intersection = model.New<IfcVirtualGridIntersection>(i =>
                        {
                            i.IntersectingAxes.Add(uAxis);
                            i.IntersectingAxes.Add(vAxis);
                            i.OffsetDistances.AddRange([new IfcLengthMeasure(0.0), new IfcLengthMeasure(0.0)]);
                        });

                        // in this example this should match with IfcGridPalcement
                        var position = new V3d(_row % vAxes.Length, _col % uAxes.Length, 0.0) * offset;
                        site.CreateAttachElement<IfcColumn>("col_calc", position, mesh, surfStyleGreen, localLayer);
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

            ValidateModel(model.Instances);

            using (var txn2 = model.BeginTransaction("Generate Intersections"))
            {
                var site = model.Instances.OfType<IfcSite>().First();

                var surfStyleOrange = model.CreateSurfaceStyle(C3d.Orange);

                var gridLayer = model.CreateLayer("Grid placed obj");

                var gridCheck = model.Instances.OfType<IfcGrid>().First();
                var placements = gridCheck.UAxes.SelectMany(axis => axis.HasIntersections.Select(i => model.New<IfcGridPlacement>(p => p.PlacementLocation = i))).ToArray();

                foreach (var placement in placements)
                {
                    site.CreateAttachElement<IfcColumn>("col_grid", placement, mesh, surfStyleOrange, gridLayer);
                }
                txn2.Commit();
            }

            ValidateModel(model.Instances);

            model.SaveAs("test_GridPlacement.ifc");
        }

        [Test]
        public static void AnnotationTest()
        {
            using var model = IfcStore.Create(AardvarkTestCredentials, XbimSchemaVersion.Ifc4, XbimStoreType.InMemoryModel);
            InitScene(model);

            using (var txn = model.BeginTransaction("Create Grid with Annotations"))
            {
                var site = model.Instances.OfType<IfcSite>().First();

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

                var groups = new List<IfcGroup>();
                var _col = -1.0;
                var _row = -1.0;

                // Create intersection points
                foreach (var uAxis in grid.UAxes)
                {
                    _col++;

                    var annotationList = new List<IfcAnnotation>();

                    foreach (var vAxis in grid.VAxes)
                    {
                        _row++;

                        var position = new V3d(_row % vAxes.Length, _col % uAxes.Length, 0.0) * offset;

                        var annotation = model.CreateAnnotation($"{uAxis.AxisTag} / {vAxis.AxisTag}", model.CreateLocalPlacement(V3d.Zero), position, annotationLayer);

                        var prop = new Dictionary<string, object>
                            {
                                { "row", uAxis.AxisTag },
                                { "col", vAxis.AxisTag },
                                { "x", position.X },
                                { "y", position.Y },
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

            ValidateModel(model.Instances);

            // Save the IFC file
            model.SaveAs("test_AnnotationGrid.ifc");
        }
    }
}