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
    }

    [TestFixture]
    public static class ExportTest
    {
        private static XbimEditorCredentials AardvarkTestCredentials()
        {
            return new XbimEditorCredentials
            {
                ApplicationDevelopersName = "Aardvark-Developer",
                ApplicationFullName = "IfcExportTest",
                ApplicationIdentifier = "Identifier",
                ApplicationVersion = "1.0",
                EditorsFamilyName = "Family",
                EditorsGivenName = "Name",
                EditorsOrganisationName = "Organisation"
            };
        }

        private static IEnumerable<ValidationResult> ValidateModel(IEntityCollection instances)
        {
            var validator = new Validator()
            {
                CreateEntityHierarchy = true,
                ValidateLevel = ValidationFlags.All
            };

            return validator.Validate(instances);
        }

        [Test]
        public static void GridTest()
        {
            // Create a new IFC file
            using (var model = IfcStore.Create(AardvarkTestCredentials(), XbimSchemaVersion.Ifc4, XbimStoreType.InMemoryModel))
            {
                using (var txn = model.BeginTransaction("Create Grid with Annotations"))
                {
                    // there should always be one project in the model
                    var project = model.New<IfcProject>(p => p.Name = "Proj");

                    // our shortcut to define basic default units
                    project.Initialize(ProjectUnits.SIUnitsUK);

                    // add site
                    var site = model.New<IfcSite>(w => w.Name = "Site");
                    project.AddSite(site);

                    var textStyle = model.CreateTextStyle(100, C3f.Red, C3f.Blue, "myFont");
                    var curveStyle = model.CreateCurveStyle(C3d.Magenta, 100.0, 10, 20);
                    var areastyle = model.CreateFillAreaStyle(C3d.Pink, 45.0, 100, curveStyle);

                    var surfStyleOrange = model.CreateSurfaceStyle(C3d.Orange);
                    var surfStyleGreen = model.CreateSurfaceStyle(C3d.Green);

                    var gridLayer = model.CreateLayer("Grid placed obj");
                    var localLayer = model.CreateLayer("Local placed obj");

                    var annotationLayer = model.CreateLayerWithStyle("Anotation-Layer with Styles", [curveStyle, areastyle, textStyle]);

                    var mesh = PolyMeshPrimitives.Box(new Box3d(V3d.Zero, new V3d(100, 100, 1000.0)), C4b.Yellow);

                    // Create grid axes
                    var uAxes = new[] { "O", "Y", "C" };
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

                            IfcVirtualGridIntersection intersection = model.New<IfcVirtualGridIntersection>(i =>
                            {
                                i.IntersectingAxes.Add(uAxis);
                                i.IntersectingAxes.Add(vAxis);
                                i.OffsetDistances.AddRange([new IfcLengthMeasure(0.0), new IfcLengthMeasure(0.0)]);
                            });

                            var gridPlacement = model.New<IfcGridPlacement>(p => p.PlacementLocation = intersection);
                            site.CreateAttachElement<IfcColumn>("col_grid", gridPlacement, mesh, surfStyleOrange, gridLayer);

                            // in this example this should match with IfcGridPalcement
                            var position = new V3d(_row % vAxes.Length, _col % uAxes.Length, 0.0) * offset;
                            site.CreateAttachElement<IfcColumn>("col_calc", position, mesh, surfStyleGreen, localLayer);

                            var annotation = model.CreateAnnotation($"{uAxis.AxisTag} / {vAxis.AxisTag}", gridPlacement, position, annotationLayer);

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

                    // TODO.. why does axis do not hold any intersections?
                    //var placements = grid.UAxes.SelectMany(axis => axis.HasIntersections.Select(i => model.New<IfcGridPlacement>(p => p.PlacementLocation = i)));

                    //foreach (var placement in placements)
                    //{
                    //    site.CreateAttachElement<IfcColumn>("col", defaultLayer, placement, defaultStyle, mesh);
                    //}

                    //var res = annotationLayer.ValidateClause(IfcPresentationLayerWithStyle.IfcPresentationLayerWithStyleClause.ApplicableOnlyToItems);

                    // Add the grid to the IFC file
                    txn.Commit();
                }

                Assert.IsEmpty(ValidateModel(model.Instances));
                
                // Save the IFC file
                //model.SaveAs("grid_with_annotations.ifc");
            }
        }

        [Test]
        public static void CreateBIMScene() //string projectName, string siteName, string buildingName, XbimEditorCredentials credentials)
        {

            using (var model = IfcStore.Create(AardvarkTestCredentials(), XbimSchemaVersion.Ifc4, XbimStoreType.InMemoryModel))
            {
                using (var txnInit = model.BeginTransaction("Init Project"))
                {
                    // there should always be one project in the model
                    var project = model.New<IfcProject>(p => p.Name = "Proj");
                    // our shortcut to define basic default units
                    project.Initialize(ProjectUnits.SIUnitsUK);

                    // add site
                    var site = model.New<IfcSite>(w => w.Name = "Site");
                    project.AddSite(site);

                    // add building
                    var building = model.New<IfcBuilding>(b => b.Name = "Building");
                    site.AddBuilding(building);

                    txnInit.Commit();
                }

                Assert.IsEmpty(ValidateModel(model.Instances));

                using (var txn1 = model.BeginTransaction("Create Test wall"))
                {
                    var building = model.Instances.OfType<IfcBuilding>().First();

                    //create simple object and use lambda initializer to set the name
                    var wall = model.New<IfcWall>(w =>
                    {
                        w.Name = "The very first wall";
                        w.ObjectPlacement = model.CreateLocalPlacement(new V3d(10.0, 5.0, 0.0));  // can be applied later on...
                    });

                    building.AddElement(wall);

                    var wall2 = model.New<IfcWall>(w => w.Name = "The second wall");
                    building.AddElement(wall2);

                    var prop = new Dictionary<string, object>
                        {
                            { "p1", "A" },
                            { "p2", 123.15 },
                            { "p3", false }
                        };

                    // re-used set
                    var setA = model.CreatePropertySet("SetA", prop);
                    wall2.AddPropertySet(setA);

                    // remove whole set and clean up
                    wall.PurgePropertySet("SetA");

                    // unique set
                    wall.CreateAttachPropertySet("SetB", prop);

                    // create prop
                    wall.SetPropertySingleValue("SetC", "my_prop1", new IfcText("start"));
                    wall.SetPropertySingleValue("SetC", "my_prop2", new IfcText("untouched"));
                    wall.SetPropertySingleValue("SetC", "my_prop3", new IfcText("remove-me-later"));

                    // update prop
                    wall.SetPropertySingleValue("SetC", "my_prop1", new IfcText("override"));

                    // removal prop
                    wall.PurgePropertySingleValue("SetC", "my_prop3");

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

                    txn1.Commit();
                }

                Assert.IsEmpty(ValidateModel(model.Instances));

                using (var txn2 = model.BeginTransaction("Create Geometries"))
                {
                    var building = model.Instances.OfType<IfcBuilding>().First();

                    var yellowStyle = model.CreateSurfaceStyle(C3d.Yellow);

                    var layer = model.CreateLayerWithStyle("Layer green styled", [model.CreateSurfaceStyle(C3d.Green)]);

                    var mesh = PolyMeshPrimitives.PlaneXY(new V2d(1000.0));
                    var wall = building.CreateAttachElement<IfcWall>("Mesh1a", new V3d(0, 0, 0), mesh, null, null, true);                    // Red (create default-material)
                    var wall1 = building.CreateAttachElement<IfcWall>("Mesh1b", new V3d(1000, 0, 0), mesh, yellowStyle, layer, true);        // Yellow from style
                    var wall2 = building.CreateAttachElement<IfcWall>("Mesh1c", new V3d(2000, 0, 0), mesh, null, layer, true);               // Green from layer

                    var mesh2 = PolyMeshPrimitives.Sphere(10, 500, C4b.Blue);
                    var window = building.CreateAttachElement<IfcWindow>("Mesh2a", new V3d(-1000, 0, 0), mesh2, null, layer, false);         // Blue from mesh
                    var window2 = building.CreateAttachElement<IfcWindow>("Mesh2b", new V3d(-2000, 0, 0), mesh2, yellowStyle, layer, false); // Yellow from style

                    var mesh3 = PolyMeshPrimitives.Box(new Box3d(V3d.Zero, new V3d(500.0, 1500.0, 500.0)), C4b.Brown);
                    var door = building.CreateAttachElement<IfcDoor>("Mesh3a", new V3d(4000, 0, 0), mesh3, yellowStyle, layer);

                    var light = model.CreateLightAmbient("MyFirstLight", C3d.Red, model.CreateLocalPlacement(new V3d(100.0, 500, 1000)), layer);
                    building.AddElement(light);

                    txn2.Commit();
                }

                Assert.IsEmpty(ValidateModel(model.Instances));

                using (var txn3 = model.BeginTransaction("Create Slab with Material"))
                {
                    var building = model.Instances.OfType<IfcBuilding>().First();

                    // MATERIAL
                    var material = model.New<IfcMaterial>(m => m.Name = "Carbon");
                    material.CreateAttachPsetMaterialCommon(98.7654, .5, 1234);
                    material.CreateAttachPsetMaterialThermal(100, 500);
                    material.CreateAttachPresentation(C3d.Magenta);

                    var slab = building.CreateAttachSlab("Mesh4", null, material);

                    txn3.Commit();
                }

                Assert.IsEmpty(ValidateModel(model.Instances));

                // Save the IFC file
                //model.SaveAs("HiliteTestBIMScene.ifc");
            }
        }
    }
}