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
using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.MeasureResource;
using Xbim.IO;

namespace Aardvark.Data.Tests.Ifc
{
    internal static class TestUtils
    {
        public static readonly XbimEditorCredentials AardvarkTestCredentials = new() {
            ApplicationDevelopersName = "Aardvark-Developer",
            ApplicationFullName = "IfcExportTest",
            ApplicationIdentifier = "Identifier",
            ApplicationVersion = "1.0",
            EditorsFamilyName = "Family",
            EditorsGivenName = "Name",
            EditorsOrganisationName = "Organisation"
        };

        /// <summary>
        /// Path of a fresh temporary .ifc file, the caller is responsible for deleting it.
        /// </summary>
        public static string TempIfcPath()
            => Path.Combine(Path.GetTempPath(), Path.ChangeExtension(Path.GetRandomFileName(), ".ifc"));

        /// <summary>
        /// Path inside the test work directory, exported models are kept there so they can be inspected in a BIM viewer.
        /// </summary>
        public static string OutputPath(string fileName)
        {
            var dir = Path.Combine(TestContext.CurrentContext.WorkDirectory, "ifc-output");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, fileName);
        }

        /// <summary>
        /// Extracts an embedded test file to a temporary location, runs the action on it and cleans up afterwards.
        /// </summary>
        public static void LoadEmbeddedData(string inputString, Action<string> action)
        {
            // necessary to run tests on github build servers
            var asm = Assembly.GetExecutingAssembly();
            var name = Regex.Replace(asm.ManifestModule.Name, @"\.(exe|dll)$", "", RegexOptions.IgnoreCase);
            var path = Regex.Replace(inputString, @"(\\|\/)", ".");
            using var stream = asm.GetManifestResourceStream(name + "." + path) ?? throw new Exception($"Cannot open resource stream with name {path}");
            var filePath = TempIfcPath();
            try
            {
                using (var file = File.Create(filePath))
                    stream.CopyTo(file);
                action.Invoke(filePath);
            }
            finally
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
        }

        /// <summary>
        /// Creates an in-memory store with a minimal project and site.
        /// </summary>
        public static IfcStore CreateModel(XbimSchemaVersion schema = XbimSchemaVersion.Ifc4)
        {
            var model = IfcStore.Create(AardvarkTestCredentials, schema, XbimStoreType.InMemoryModel);
            model.CreateMinimalProject();
            return model;
        }

        /// <summary>
        /// Every parsed product must carry a mesh with faces, a material and a transformation, and the hierarchy must start at the project.
        /// </summary>
        public static void AssertConsistent(IFCData parsed)
        {
            Assert.IsNotNull(parsed.Hierarchy);
            Assert.IsInstanceOf<IIfcProject>(parsed.Hierarchy.MetaData);
            Assert.IsNotNull(parsed.Project);

            foreach (var content in parsed.Content.Values)
            {
                Assert.IsNotNull(content.IFCObject);
                Assert.IsNotNull(content.Mesh, $"{content.Name} has no mesh");
                Assert.IsNotNull(content.Mesh.PositionArray, $"{content.Name} has no positions");
                Assert.IsNotNull(content.Material, $"{content.Name} has no material");
                Assert.IsTrue(parsed.Materials.ContainsKey(content.Material.Name), $"material {content.Material.Name} is not listed");
                Assert.IsFalse(double.IsNaN(content.Trafo.Forward.M00), $"{content.Name} has an invalid transformation");
            }
        }

        /// <summary>
        /// The node and all its descendants.
        /// </summary>
        public static IEnumerable<IIFCNode> Flatten(IIFCNode node)
            => [node, .. node.SubNodes.SelectMany(Flatten)];

        /// <summary>
        /// Bounding box of the mesh in model coordinates, i.e. with the instance transformation applied.
        /// </summary>
        public static Box3d WorldBounds(IFCContent content)
            => new(content.Mesh.PositionArray.Select(p => content.Trafo.Forward.TransformPos(p)));

        public static void AssertBoxEquals(Box3d expected, Box3d actual, double tolerance)
        {
            Assert.IsTrue(expected.Min.ApproximateEquals(actual.Min, tolerance), $"min {actual.Min} differs from {expected.Min}");
            Assert.IsTrue(expected.Max.ApproximateEquals(actual.Max, tolerance), $"max {actual.Max} differs from {expected.Max}");
        }
    }

    [TestFixture]
    public static class LoadingTest
    {
        [Test]
        public static void LoadPrimitive()
        {
            TestUtils.LoadEmbeddedData(@"data\surface-model.ifc", (filePath) => {
                using var parsed = IFCParser.PreprocessIFC(filePath);
                TestUtils.AssertConsistent(parsed);
                Assert.AreEqual(1, parsed.Materials.Count);
                Assert.AreEqual(1, parsed.Content.Count);
                Assert.AreEqual(0.001, parsed.UnitScale);
                Assert.IsInstanceOf<IIfcBuildingElementProxy>(parsed.Content.Values.Single().IFCObject);
            });
        }

        [Test]
        public static void LoadWall()
        {
            TestUtils.LoadEmbeddedData(@"data\wall.ifc", (filePath) => {
                using var parsed = IFCParser.PreprocessIFC(filePath);
                TestUtils.AssertConsistent(parsed);
                Assert.AreEqual(4, parsed.Materials.Count);
                Assert.AreEqual(0.001, parsed.UnitScale);

                // the wall has no surface style, so the first layer of its material layer set is used
                // the layer materials carry no representation and are listed by name only
                var wall = parsed.Content.Values.Single();
                Assert.IsInstanceOf<IIfcWall>(wall.IFCObject);
                Assert.Greater(wall.Mesh.FaceCount, 0);
                Assert.AreEqual("Masonry - Brick - Brown", wall.Material.Name);
                Assert.IsNull(wall.Material.Texture);
                Assert.IsTrue(parsed.Materials.ContainsKey("DefMat-IfcWall"));
                Assert.IsTrue(parsed.Materials.ContainsKey("Air Infiltration Barrier"));
                Assert.IsTrue(parsed.Materials.ContainsKey("Masonry"));

                // project -> building -> wall
                var building = parsed.Hierarchy.SubNodes.Single();
                Assert.IsInstanceOf<IIfcBuilding>(building.MetaData);
                Assert.AreSame(wall.IFCObject, building.SubNodes.Single().MetaData);

                // 5000 x 270 x 2000 mm extrusion
                TestUtils.AssertBoxEquals(new Box3d(V3d.Zero, new V3d(5000, 270, 2000)), TestUtils.WorldBounds(wall), 1e-3);
            });
        }

        [Test]
        public static void LoadSlab()
        {
            TestUtils.LoadEmbeddedData(@"data\slab.ifc", (filePath) => {
                using var parsed = IFCParser.PreprocessIFC(filePath);
                TestUtils.AssertConsistent(parsed);
                Assert.AreEqual(2, parsed.Materials.Count);
                Assert.AreEqual(1, parsed.Content.Count);
            });
        }

        [Test]
        public static void MultiThreadKiller()
        {
            // NOTE: In IFCParser -> change MaxThreads!
            //  var context = new Xbim3DModelContext(model);
            //  context.MaxThreads = 1; <- SINGLE-Thread works!

            TestUtils.LoadEmbeddedData(@"data\simple_scene.ifc", (filePath) => {
                using var parsed = IFCParser.PreprocessIFC(filePath, geometryEngine: Xbim.Geometry.Abstractions.XGeometryEngineVersion.V5, singleThreading: false);
                Assert.AreEqual(5, parsed.Materials.Count);
                Assert.AreEqual(1, parsed.UnitScale);
            });

            TestUtils.LoadEmbeddedData(@"data\simple_scene.ifc", (filePath) => {
                using var parsed = IFCParser.PreprocessIFC(filePath, geometryEngine: Xbim.Geometry.Abstractions.XGeometryEngineVersion.V5, singleThreading: true);
                Assert.AreEqual(5, parsed.Materials.Count);
            });

            TestUtils.LoadEmbeddedData(@"data\simple_scene.ifc", (filePath) => {
                using var parsed = IFCParser.PreprocessIFC(filePath, geometryEngine: Xbim.Geometry.Abstractions.XGeometryEngineVersion.V6, singleThreading: false);
                Assert.AreEqual(5, parsed.Materials.Count);
            });

            TestUtils.LoadEmbeddedData(@"data\simple_scene.ifc", (filePath) => {
                using var parsed = IFCParser.PreprocessIFC(filePath, geometryEngine: Xbim.Geometry.Abstractions.XGeometryEngineVersion.V6, singleThreading: true);
                Assert.AreEqual(5, parsed.Materials.Count);
            });
        }

        [Test]
        public static void LoadScene()
        {
            TestUtils.LoadEmbeddedData(@"data\simple_scene.ifc", (filePath) => {
                using var parsed = IFCParser.PreprocessIFC(filePath);
                TestUtils.AssertConsistent(parsed);

                // slab, covering and wall
                Assert.AreEqual(3, parsed.Content.Count);
                var slab = parsed.Content.Values.Single(c => c.IFCObject is IIfcSlab);
                Assert.IsTrue(parsed.Content.Values.Any(c => c.IFCObject is IIfcCovering));
                Assert.IsTrue(parsed.Content.Values.Any(c => c.IFCObject is IIfcWall));

                // properties of the product and of its material survive the import
                var loadBearing = slab.IFCObject.GetProperty("LoadBearing");
                Assert.IsNotNull(loadBearing);
                Assert.IsTrue(loadBearing.TryGetSimpleValue(out bool isLoadBearing) && isLoadBearing);

                var concrete = parsed.Materials["Stahlbeton Decke"];
                Assert.AreEqual(2400.0, concrete.MassDensity, 1e-9);
                Assert.AreEqual(2.5, concrete.ThermalConductivity, 1e-9);
                Assert.AreEqual(1000.0, concrete.SpecificHeatCapacity, 1e-9);

                // the spatial structure and all products with geometry show up in the hierarchy
                // the file references entities that were stripped from it, so only the presence is checked, not the exact tree shape
                var nodes = TestUtils.Flatten(parsed.Hierarchy).Select(n => n.MetaData).ToArray();
                Assert.IsTrue(nodes.Any(n => n is IIfcSite));
                Assert.IsTrue(nodes.Any(n => n is IIfcBuilding));
                Assert.IsTrue(nodes.Any(n => n is IIfcBuildingStorey));
                foreach (var content in parsed.Content.Values)
                    Assert.IsTrue(nodes.Contains(content.IFCObject), $"{content.Name} is missing in the hierarchy");
            });
        }

        [Test]
        public static void LoadMaterial()
        {
            TestUtils.LoadEmbeddedData(@"data\test_Material.ifc", (filePath) => {
                using var parsed = IFCParser.PreprocessIFC(filePath, geometryEngine: Xbim.Geometry.Abstractions.XGeometryEngineVersion.V6, singleThreading: false);
                var carbonMaterial = parsed.Materials["Carbon"];
                Assert.IsTrue(carbonMaterial.ThermalConductivity.ApproximateEquals(100.0) && carbonMaterial.MassDensity.ApproximateEquals(1234.0));
                Assert.AreEqual(.001, parsed.UnitScale);
            });
        }

        [Test]
        public static void LoadIfc4x3()
        {
            TestUtils.LoadEmbeddedData(@"data\Viadotto Acerno_ifc43.ifc", (filePath) => {
                using var parsed = IFCParser.PreprocessIFC(filePath, singleThreading: true);
                TestUtils.AssertConsistent(parsed);
                Assert.AreEqual(26, parsed.Materials.Count);
                Assert.Greater(parsed.Content.Count, 0);
            });
        }

        // NOTE: the edit tests use slab.ifc (schema IFC4), wall.ifc is IFC4x1 and xBIM's EntityCreator only supports Ifc2X3, Ifc4 and Ifc4x3

        [Test]
        public static void EditAfterImportWithoutCaching()
        {
            // xBIM refuses to open a transaction while its inverse cache is alive, so loading without caching must allow edits right away
            var outPath = TestUtils.TempIfcPath();
            try
            {
                TestUtils.LoadEmbeddedData(@"data\slab.ifc", (filePath) => {
                    // editor credentials are needed for the owner history xBIM attaches to every new IfcRoot entity
                    using var parsed = IFCParser.PreprocessIFC(filePath, editor: TestUtils.AardvarkTestCredentials, enableCaching: false);
                    Assert.IsFalse(parsed.IsCaching);

                    // only failures introduced by the edit count
                    var failuresBefore = parsed.IfcStore.ValidateModel().Count();

                    using (var txn = parsed.IfcStore.BeginTransaction("Edit imported model"))
                    {
                        var slab = parsed.IfcStore.Instances.OfType<IIfcSlab>().First();
                        slab.CreateAttachPropertySet("Edited", new Dictionary<string, object> { { "touched", true } });
                        txn.Commit();
                    }

                    var failuresAfter = parsed.IfcStore.ValidateModel().ToArray();
                    Assert.AreEqual(failuresBefore, failuresAfter.Length, "editing introduced validation failures: " + string.Join("; ", failuresAfter.Select(r => r.Message)));
                    parsed.IfcStore.SaveAs(outPath);
                });

                using var reloaded = IfcStore.Open(outPath);
                var pset = reloaded.Instances.OfType<IIfcSlab>().First().GetPropertySet("Edited");
                Assert.IsNotNull(pset);
                Assert.AreEqual("touched", pset.HasProperties.Single().Name.ToString());
            }
            finally
            {
                if (File.Exists(outPath))
                    File.Delete(outPath);
            }
        }

        [Test]
        public static void EndCachingAllowsEditing()
        {
            TestUtils.LoadEmbeddedData(@"data\slab.ifc", (filePath) => {
                using var parsed = IFCParser.PreprocessIFC(filePath, editor: TestUtils.AardvarkTestCredentials);
                Assert.IsTrue(parsed.IsCaching);

                parsed.EndCaching();
                Assert.IsFalse(parsed.IsCaching);

                using (var txn = parsed.IfcStore.BeginTransaction("Edit after caching"))
                {
                    var slab = parsed.IfcStore.Instances.OfType<IIfcSlab>().First();
                    slab.CreateAttachPropertySet("Edited", new Dictionary<string, object> { { "touched", true } });
                    txn.Commit();
                }

                Assert.IsNotNull(parsed.IfcStore.Instances.OfType<IIfcSlab>().First().GetPropertySet("Edited"));
            });
        }
    }

    [TestFixture]
    public static class RoundTripTest
    {
        [Test]
        public static void ExportThenImport()
        {
            var path = TestUtils.TempIfcPath();
            var box = new Box3d(V3d.Zero, new V3d(500, 300, 200));
            var offset = new V3d(1000, 2000, 300);

            try
            {
                using (var model = TestUtils.CreateModel())
                {
                    using (var txn = model.BeginTransaction("Create round trip scene"))
                    {
                        var factory = model.Factory();
                        var testSite = model.Instances.OfType<IIfcSite>().First();
                        var mesh = PolyMeshPrimitives.Box(box, C4b.Red);
                        var redStyle = model.CreateSurfaceStyle(C3d.Red, name: "RoundTripRed");

                        testSite.AddElement(factory.Wall(w => w.Name = "RoundTripWall").CreateAttachRepresentation(V3d.Zero, mesh, null, redStyle));
                        testSite.AddElement(factory.Slab(s => s.Name = "RoundTripSlab").CreateAttachRepresentation(offset, mesh, null, redStyle));

                        txn.Commit();
                    }

                    Assert.IsEmpty(model.ValidateModel());
                    model.SaveAs(path);
                }

                using var parsed = IFCParser.PreprocessIFC(path);
                TestUtils.AssertConsistent(parsed);

                // CreateMinimalProject defaults to centimetres
                Assert.AreEqual(0.01, parsed.UnitScale, 1e-12);
                Assert.AreEqual(2, parsed.Content.Count);

                var wall = parsed.Content.Values.Single(c => c.IFCObject is IIfcWall);
                var slab = parsed.Content.Values.Single(c => c.IFCObject is IIfcSlab);
                Assert.AreEqual("RoundTripWall", wall.Name);
                Assert.AreEqual("RoundTripSlab", slab.Name);
                Assert.Greater(wall.Mesh.FaceCount, 0);
                Assert.Greater(slab.Mesh.FaceCount, 0);

                // the placement is part of the instance transformation, so both world boxes must match the exported ones
                TestUtils.AssertBoxEquals(box, TestUtils.WorldBounds(wall), 1e-3);
                TestUtils.AssertBoxEquals(box.Translated(offset), TestUtils.WorldBounds(slab), 1e-3);

                // both products share the explicit surface style, which comes back as one style material
                Assert.AreEqual(1, parsed.Materials.Count);
                Assert.AreEqual("Style-RoundTripRed", wall.Material.Name);
                Assert.AreSame(wall.Material, slab.Material);
                Assert.IsNotNull(wall.Material.Texture);

                // project -> site -> wall, slab
                var site = parsed.Hierarchy.SubNodes.Single();
                Assert.IsInstanceOf<IIfcSite>(site.MetaData);
                Assert.AreEqual(2, site.SubNodes.Count);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }
    }

    [TestFixture]
    public static class ConversionTest
    {
        [Test]
        public static void Axis2Placement2DIsRotationNotReflection()
        {
            using var model = TestUtils.CreateModel();
            using var txn = model.BeginTransaction("Create placement");

            // x axis points along +y, so the frame is rotated by 90 degrees counter clockwise
            var placement = model.CreateAxis2Placement2D(new V2d(10, 20), new V2d(0, 1));
            var trafo = placement.ToTrafo2D();

            Assert.IsTrue(trafo.Forward.TransformPos(new V2d(1, 0)).ApproximateEquals(new V2d(10, 21), 1e-9));
            Assert.IsTrue(trafo.Forward.TransformPos(new V2d(0, 1)).ApproximateEquals(new V2d(9, 20), 1e-9));

            // a lifted 2d placement must stay right handed
            var trafo3d = ((IIfcAxis2Placement)placement).ToTrafo3d();
            Assert.IsTrue(trafo3d.Forward.C2.XYZ.ApproximateEquals(V3d.ZAxis, 1e-9));
        }

        [Test]
        public static void Axis2Placement3DDefaults()
        {
            using var model = TestUtils.CreateModel();
            using var txn = model.BeginTransaction("Create placements");
            var factory = model.Factory();

            // only the axis is given, the reference direction defaults per IFC to +X or to +Y when the axis is parallel to +X
            var axisOnly = factory.Axis2Placement3D(a =>
            {
                a.Location = model.CreatePoint(new V3d(1, 2, 3));
                a.Axis = model.CreateDirection(V3d.XAxis);
            });
            var trafo = axisOnly.ToTrafo3d();
            Assert.IsTrue(trafo.Forward.C2.XYZ.ApproximateEquals(V3d.XAxis, 1e-9));
            Assert.IsTrue(trafo.Forward.C0.XYZ.ApproximateEquals(V3d.YAxis, 1e-9));
            Assert.IsTrue(trafo.Forward.C1.XYZ.ApproximateEquals(V3d.ZAxis, 1e-9));
            Assert.IsTrue(trafo.Forward.C3.XYZ.ApproximateEquals(new V3d(1, 2, 3), 1e-9));

            // only the reference direction is given, the axis defaults to +Z
            var refOnly = factory.Axis2Placement3D(a =>
            {
                a.Location = model.CreatePoint(V3d.Zero);
                a.RefDirection = model.CreateDirection(V3d.YAxis);
            });
            var trafoRef = refOnly.ToTrafo3d();
            Assert.IsTrue(trafoRef.Forward.C0.XYZ.ApproximateEquals(V3d.YAxis, 1e-9));
            Assert.IsTrue(trafoRef.Forward.C1.XYZ.ApproximateEquals(-V3d.XAxis, 1e-9));
            Assert.IsTrue(trafoRef.Forward.C2.XYZ.ApproximateEquals(V3d.ZAxis, 1e-9));

            // a reference direction that is not orthogonal to the axis gets projected
            var skewed = model.CreateAxis2Placement3D(V3d.Zero, new V3d(1, 0, 1), V3d.ZAxis);
            var trafoSkewed = skewed.ToTrafo3d();
            Assert.IsTrue(trafoSkewed.Forward.C0.XYZ.ApproximateEquals(V3d.XAxis, 1e-9));
            Assert.IsTrue(trafoSkewed.Forward.C2.XYZ.ApproximateEquals(V3d.ZAxis, 1e-9));
        }

        [Test]
        public static void SimpleValueConversion()
        {
            Assert.IsTrue(new IfcInteger(42).TryGetSimpleValue(out int i) && i == 42);
            Assert.IsTrue(new IfcInteger(42).TryGetSimpleValue(out long l) && l == 42);
            Assert.IsTrue(new IfcReal(2.5).TryGetSimpleValue(out double d) && d == 2.5);
            Assert.IsTrue(new IfcBoolean(true).TryGetSimpleValue(out bool b) && b);
            Assert.IsTrue(new IfcLengthMeasure(12.5).TryGetSimpleValue(out double length) && length == 12.5);
        }

        [Test]
        public static void AardvarkPrimitivesTest()
        {
            using var model = TestUtils.CreateModel();

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

    [TestFixture]
    public static class ExportTest
    {
        [Test]
        public static void PropertyTest()
        {
            using var model = TestUtils.CreateModel();

            using (var txn = model.BeginTransaction("Create Wall Properties"))
            {
                var site = model.Instances.OfType<IIfcSite>().FirstOrDefault();

                // everything below works on the cross-schema interface, no cast to a concrete schema type needed
                var wall = model.Factory().Wall(w => w.Name = "The very first wall");
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

            var result = model.Instances.OfType<IIfcWall>().First();
            Assert.IsNull(result.GetPropertySet("SetA"));
            Assert.IsNotNull(result.GetPropertySet("SetB"));
            Assert.AreEqual(3, result.GetProperties("SetB").Count());
            Assert.IsTrue(result.GetProperties("SetB").Single(p => p.Name == (IfcIdentifier)"p2").TryGetSimpleValue(out double p2) && p2.ApproximateEquals(123.15));

            var setC = result.GetPropertySet("SetC");
            Assert.IsNotNull(setC);
            Assert.AreEqual(2, setC.HasProperties.Count);
            Assert.AreEqual("override", (string)(IfcText)result.GetPropertySingleValue("SetC", "my_prop1").NominalValue);
            Assert.IsNull(result.GetPropertySingleValue("SetC", "my_prop3"));

            // purged properties and sets must be gone from the model, not only detached
            Assert.IsFalse(model.Instances.OfType<IIfcPropertySet>().Any(p => p.Name == "SetA"));
            Assert.AreEqual(5, model.Instances.OfType<IIfcPropertySingleValue>().Count());

            model.SaveAs(TestUtils.OutputPath("test_Properties.ifc"));
        }

        [Test]
        public static void PropertyTest2x3()
        {
            // property access through the cross-schema interfaces has to work for IFC2x3 as well
            using var model = TestUtils.CreateModel(XbimSchemaVersion.Ifc2X3);

            using (var txn = model.BeginTransaction("Create Wall Properties"))
            {
                var site = model.Instances.OfType<IIfcSite>().FirstOrDefault();
                var wall = model.Factory().Wall(w => w.Name = "2x3 wall");
                site.AddElement(wall);
                wall.CreateAttachPropertySet("SetA", new Dictionary<string, object> { { "p1", "A" }, { "p2", 123.15 }, { "p3", false } });
                txn.Commit();
            }

            Assert.IsEmpty(model.ValidateModel());

            var result = model.Instances.OfType<IIfcWall>().First();
            Assert.AreEqual(3, result.GetProperties().Count());
            Assert.AreEqual(3, result.GetPropertiesDict("SetA").Count);
            Assert.IsTrue(result.GetProperties("SetA").Single(p => p.Name == (IfcIdentifier)"p2").TryGetSimpleValue(out double p2) && p2.ApproximateEquals(123.15));
            Assert.IsTrue(result.GetProperties("SetA").Single(p => p.Name == (IfcIdentifier)"p3").TryGetSimpleValue(out bool p3) && !p3);
        }

        [Test]
        public static void WallTest()
        {
            using var model = TestUtils.CreateModel();

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

                // IfcPresentationLayerAssignment is required for CAD presentation
                var cadLayer = model.CreateLayer("Building Element");

                var boxShape = model.CreateShapeRepresentationSolidBox(box, cadLayer);

                var repItem = boxShape.Items.First(); // retrieve body of shape

                var defaultStyle = model.CreateSurfaceStyle(C3d.Yellow);

                // create visual style
                repItem.CreateStyleItem(defaultStyle);

                // the surface helper must not flip the caller's polygon
                var polygon = new Polygon2d(Enumerable.Reverse(box.XY.Translated(-box.XY.Center).ComputeCornersCCW()).ToArray());
                var firstPoint = polygon[0];

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
                            model.CreateShapeRepresentationSurface(Plane3d.ZPlane, polygon),
                        ]);
                    });

                Assert.AreEqual(firstPoint, polygon[0]);

                txn.Commit();
            }

            Assert.IsEmpty(model.ValidateModel());
            model.SaveAs(TestUtils.OutputPath("test_TestWall.ifc"));
        }

        [Test]
        public static void WallTest2X3()
        {
            using var model = IfcStore.Create(TestUtils.AardvarkTestCredentials, XbimSchemaVersion.Ifc2X3, XbimStoreType.InMemoryModel);

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

            model.SaveAs(TestUtils.OutputPath("test_TestWall2x3.ifc"));
            //Assert.IsEmpty(result);   // TODO.. check with later version of xBIM Essentials
        }

        [Test]
        public static void GeometryTest()
        {
            using var model = TestUtils.CreateModel();

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

            // one representation per wall, IFC4 uses tessellated face sets
            var walls = model.Instances.OfType<IIfcWall>().ToArray();
            Assert.AreEqual(6, walls.Length);
            foreach (var wall in walls)
            {
                var body = wall.Representation.Representations.OfType<IIfcShapeRepresentation>().Single();
                Assert.AreEqual("Tessellation", body.RepresentationType.ToString());
                Assert.IsInstanceOf<IIfcTessellatedFaceSet>(body.Items.Single());
            }

            model.SaveAs(TestUtils.OutputPath("test_Geometries.ifc"));
        }

        [Test]
        public static void LightTest()
        {
            using var model = TestUtils.CreateModel();

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

            // the instantiated light got the quantity set through the cross-schema interface
            var typed = model.Instances.OfType<IIfcLightFixture>().First(l => l.Name == "Empty_Light_fromtype");
            Assert.IsTrue(typed.GetQuantities().OfType<IIfcQuantityWeight>().Single().TryGetSimpleValue(out double weight) && weight == 50);

            model.SaveAs(TestUtils.OutputPath("test_Lights.ifc"));
        }

        [Test]
        public static void LightTest2x3()
        {
            using var model = TestUtils.CreateModel(XbimSchemaVersion.Ifc2X3);

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
            model.SaveAs(TestUtils.OutputPath("test_Lights2x3.ifc"));
        }

        [Test]
        public static void MaterialTest()
        {
            using var model = TestUtils.CreateModel();

            var massDensity = 1234.0;
            var thermalConductivity = 100;

            using (var txn = model.BeginTransaction("Create Slab with Material"))
            {
                var factory = model.Factory();

                var site = model.Instances.OfType<IIfcSite>().FirstOrDefault();

                // MATERIAL, property sets are attached through the cross-schema interface
                var material = factory.Material(m => m.Name = "Carbon");
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
            Assert.AreEqual(2, mat.HasProperties.Count());

            var myMaterial = IFCParser.GetMaterial(mat);
            Assert.IsTrue(myMaterial.MassDensity == massDensity);
            Assert.IsTrue(myMaterial.ThermalConductivity == thermalConductivity);
            Assert.IsTrue(myMaterial.SpecificHeatCapacity == 500);
            Assert.IsNotNull(myMaterial.Texture);

            // Cast to IIfcMaterial results into invalid null values for the first 3 properties in .NET8.0 -> resolved with xbim issue #595 in xbim.essentials 6.0.493
            var properties = mat.HasProperties.SelectMany(a => a.Properties).ToArray();
            Assert.AreEqual(7, properties.Length);
            properties.ForEach(Assert.IsNotNull);

            // the associated material is found through the layer set usage
            var collected = new List<IFCMaterial>();
            IFCParser.GetAssociatedMaterials(model.Instances.OfType<IIfcRelAssociatesMaterial>().First().RelatingMaterial, collected);
            Assert.AreEqual(1, collected.Count);
            Assert.AreEqual("Carbon", collected[0].Name);

            model.SaveAs(TestUtils.OutputPath("test_Material.ifc"));
        }

        [Test]
        public static void GridPlacementTest()
        {
            using var model = TestUtils.CreateModel();

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

            // 3 x 3 columns placed locally and the same number placed on the grid intersections
            Assert.AreEqual(9, model.Instances.OfType<IIfcColumn>().Count(c => c.Name == "col_calc"));
            Assert.AreEqual(9, model.Instances.OfType<IIfcColumn>().Count(c => c.Name == "col_grid"));

            model.SaveAs(TestUtils.OutputPath("test_GridPlacement.ifc"));
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

            using var model = TestUtils.CreateModel();

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
            model.SaveAs(TestUtils.OutputPath("test_AnnotationGrid.ifc"));
        }

        [Test]
        public static void AxisRepresentation()
        {
            using var model = TestUtils.CreateModel();

            IIfcBuildingElementProxy withBody;
            IIfcBuildingElementProxy axisOnly;
            IIfcBuildingElementProxy withGap;

            using (var txn = model.BeginTransaction("Create products with axis"))
            {
                var factory = model.Factory();
                var site = model.Instances.OfType<IIfcSite>().First();

                // track along x, 2 m long, 50 mm cross section, axis runs through the centre of the body and sticks out the far end so it is visible
                var length = 2000.0;
                var body = PolyMeshPrimitives.Box(new Box3d(new V3d(0, -25, -25), new V3d(length, 25, 25)), C4b.Gray);

                withBody = factory.BuildingElementProxy(p => p.Name = "Track with body");
                withBody.CreateAttachRepresentation(model.CreateLocalPlacement(V3d.Zero), body, C4d.Gray)
                        .AddRepresentation(model.CreateIndexedPolyCurve([V3d.Zero, new V3d(2 * length, 0, 0)]).CreateShapeRepresentationAxis3d());
                site.AddElement(withBody);

                // no body here, so AddRepresentation has to create the product shape itself
                V3d[] bent = [new V3d(0, 500, 0), new V3d(1000, 500, 0), new V3d(1500, 1000, 0), new V3d(1000, 1500, 0)];
                axisOnly = factory.BuildingElementProxy(p =>
                {
                    p.Name = "Track axis only";
                    p.ObjectPlacement = model.CreateLocalPlacement(V3d.Zero);
                });
                axisOnly.AddRepresentation(model.CreateIndexedPolyCurve(bent, [[1, 2], [2, 3, 4]]).CreateShapeRepresentationAxis3d());
                site.AddElement(axisOnly);

                // one axis with a gap: two curves on a shared point list, a line from 1 to 2, then an arc from 3 over 4 to 5
                var gapPoints = model.CreateCartesianPointList3D(
                    new V3d(0, 2500, 0), new V3d(1000, 2500, 0),
                    new V3d(1500, 2500, 0), new V3d(2000, 3000, 0), new V3d(2500, 2500, 0));
                withGap = factory.BuildingElementProxy(p =>
                {
                    p.Name = "Track with gap";
                    p.ObjectPlacement = model.CreateLocalPlacement(V3d.Zero);
                });
                withGap.AddRepresentation(model.CreateShapeRepresentationAxis3d([
                    model.CreateIndexedPolyCurve(gapPoints, [[1, 2]]),
                    model.CreateIndexedPolyCurve(gapPoints, [[3, 4, 5]]),
                ]));
                site.AddElement(withGap);

                txn.Commit();
            }

            Assert.IsEmpty(model.ValidateModel());

            // body stays first, axis is appended behind it
            var withBodyReps = withBody.Representation.Representations.OfType<IIfcShapeRepresentation>().ToArray();
            Assert.AreEqual(2, withBodyReps.Length);
            Assert.AreNotEqual("Axis", withBodyReps[0].RepresentationIdentifier.ToString());
            AssertAxis(withBodyReps[1], expectedSegments: 0);

            // product without a shape got one created
            var axisOnlyReps = axisOnly.Representation.Representations.OfType<IIfcShapeRepresentation>().ToArray();
            Assert.AreEqual(1, axisOnlyReps.Length);
            AssertAxis(axisOnlyReps[0], expectedSegments: 2);

            // gap: one axis representation holding two curves that share their point list
            var withGapReps = withGap.Representation.Representations.OfType<IIfcShapeRepresentation>().ToArray();
            Assert.AreEqual(1, withGapReps.Length);
            Assert.AreEqual("Axis", withGapReps[0].RepresentationIdentifier.ToString());
            Assert.AreEqual("Curve3D", withGapReps[0].RepresentationType.ToString());

            var gapCurves = withGapReps[0].Items.OfType<IIfcIndexedPolyCurve>().ToArray();
            Assert.AreEqual(2, gapCurves.Length);
            Assert.AreSame(gapCurves[0].Points, gapCurves[1].Points);
            Assert.AreEqual(1, gapCurves[0].Segments.Count);
            Assert.AreEqual(1, gapCurves[1].Segments.Count);
            Assert.IsInstanceOf<IfcLineIndex>(gapCurves[0].Segments.First());
            Assert.IsInstanceOf<IfcArcIndex>(gapCurves[1].Segments.First());

            // all axes share one sub-context
            Assert.AreSame(withBodyReps[1].ContextOfItems, axisOnlyReps[0].ContextOfItems);
            Assert.AreSame(withBodyReps[1].ContextOfItems, withGapReps[0].ContextOfItems);
            Assert.AreEqual(1, model.Instances.OfType<IIfcGeometricRepresentationSubContext>().Count(c => c.ContextIdentifier.ToString() == "Axis"));

            model.SaveAs(TestUtils.OutputPath("test_AxisRepresentation.ifc"));
        }

        static void AssertAxis(IIfcShapeRepresentation rep, int expectedSegments)
        {
            Assert.AreEqual("Axis", rep.RepresentationIdentifier.ToString());
            Assert.AreEqual("Curve3D", rep.RepresentationType.ToString());

            var curve = rep.Items.Single() as IIfcIndexedPolyCurve;
            Assert.IsNotNull(curve);
            Assert.IsInstanceOf<IIfcCartesianPointList3D>(curve.Points);
            Assert.AreEqual(expectedSegments, curve.Segments.Count);
        }
    }
}
