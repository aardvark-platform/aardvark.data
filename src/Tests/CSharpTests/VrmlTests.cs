using Aardvark.Base;
using Aardvark.Geometry;
using Aardvark.Rendering;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Aardvark.Data.Vrml97
{
    [TestFixture]
    public class VrmlIndexedLineSetTests
    {
        #region helpers

        private static void LoadEmbeddedData(string inputString, Action<string> action)
        {
            // necessary to run tests on github build servers
            var asm = Assembly.GetExecutingAssembly();
            var name = Regex.Replace(asm.ManifestModule.Name, @"\.(exe|dll)$", "", RegexOptions.IgnoreCase);
            var path = Regex.Replace(inputString, @"(\\|\/)", ".");
            using var stream = asm.GetManifestResourceStream(name + "." + path) ?? throw new Exception($"Cannot open resource stream with name {path}");
            var filePath = Path.ChangeExtension(Path.GetRandomFileName(), ".wrl");
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

        private static VrmlScene LoadScene(string filePath)
        {
            Report.BeginTimed($"parsing: {filePath}");
            var vrmlParseTree = Vrml97Scene.FromFile(filePath);
            Report.End();

            Report.BeginTimed("creating scene graph");
            var vrml = SceneLoader.Load(vrmlParseTree, out var nodeMap);
            Report.End();

            return vrml;
        }

        /// <summary>
        /// Collects all VrmlLineSets of the scene in document order.
        /// </summary>
        private static List<VrmlLineSet> GetLineSets(VrmlNode node)
        {
            var result = new List<VrmlLineSet>();
            CollectLineSets(node, result);
            return result;
        }

        private static void CollectLineSets(VrmlNode node, List<VrmlLineSet> result)
        {
            if (node is VrmlShape shape && shape.Geometry is VrmlLineSet ls)
                result.Add(ls);

            if (node is VrmlGroup group)
                foreach (var child in group)
                    CollectLineSets(child, result);
        }

        /// <summary>
        /// Collects all VrmlShapes of the scene in document order.
        /// </summary>
        private static List<VrmlShape> GetShapes(VrmlNode node)
        {
            var result = new List<VrmlShape>();
            CollectShapes(node, result);
            return result;
        }

        private static void CollectShapes(VrmlNode node, List<VrmlShape> result)
        {
            if (node is VrmlShape shape)
                result.Add(shape);

            if (node is VrmlGroup group)
                foreach (var child in group)
                    CollectShapes(child, result);
        }

        /// <summary>
        /// All embedded Vrml97 specification example scenes, as paths accepted by LoadEmbeddedData.
        /// </summary>
        private static IEnumerable<string> Vrml97SpecScenes()
        {
            var asm = Assembly.GetExecutingAssembly();
            var name = Regex.Replace(asm.ManifestModule.Name, @"\.(exe|dll)$", "", RegexOptions.IgnoreCase);
            var prefix = name + ".data.Vrml97Spec.";

            return asm.GetManifestResourceNames()
                      .Where(n => n.StartsWith(prefix) && n.EndsWith(".wrl"))
                      .Select(n => @"data\Vrml97Spec\" + n.Substring(prefix.Length))
                      .OrderBy(n => n);
        }

        #endregion

        // the colours used in the test scene, converted the same way as PolyLineSetFromVrml97 does
        // NOTE: do not use the C4b constants here, C4b.Green is the X11 green (0, 128, 0) and not pure green
        private static readonly C4b s_red = new C4b(new C3f(1, 0, 0));
        private static readonly C4b s_green = new C4b(new C3f(0, 1, 0));
        private static readonly C4b s_blue = new C4b(new C3f(0, 0, 1));
        private static readonly C4b s_yellow = new C4b(new C3f(1, 1, 0));

        private static C4b[] LineColors(VrmlLineSet ls) => (C4b[])ls.LineSet.LineAttributes[PolyLineSet.Property.Colors];

        private static C4b[] LineVertexColors(VrmlLineSet ls) => (C4b[])ls.LineSet.LineVertexAttributes[PolyLineSet.Property.Colors];

        [Test]
        public static void IndexedLineSetValidCases()
        {
            LoadEmbeddedData(@"data\IndexedLineSet_Cases.wrl", (filePath) => {

                var scene = LoadScene(filePath);
                var ls = GetLineSets(scene);

                // 32 Shapes carrying an IndexedLineSet (cases 10 and 11 have two each)
                Assert.That(ls.Count, Is.EqualTo(32));

                // 1) single 2-point polyline (the only case in "00 Tracks"): double vertices, no colour attributes
                var set = ls[0].LineSet;
                Assert.That(set.VertexArray, Is.EqualTo(new[] { V3d.OOO, V3d.OIO }));
                Assert.That(set.VertexIndexArray, Is.EqualTo(new[] { 0, 1 }));
                Assert.That(set.FirstIndexArray, Is.EqualTo(new[] { 0, 2 }));
                Assert.That(set.PolyLineCount, Is.EqualTo(1));
                Assert.That(set.SegmentCount, Is.EqualTo(1));
                Assert.That(set.LineAttributes.Count, Is.EqualTo(0));
                Assert.That(set.LineVertexAttributes.Count, Is.EqualTo(0));

                // 2) no trailing -1
                Assert.That(ls[1].LineSet.VertexIndexArray, Is.EqualTo(new[] { 0, 1, 2 }));
                Assert.That(ls[1].LineSet.FirstIndexArray, Is.EqualTo(new[] { 0, 3 }));

                // 3) several polylines, shared vertex, closed loop
                set = ls[2].LineSet;
                Assert.That(set.VertexIndexArray, Is.EqualTo(new[] { 0, 1, 2, 3, 0, 4, 5 }));
                Assert.That(set.FirstIndexArray, Is.EqualTo(new[] { 0, 5, 7 }));
                Assert.That(set.PolyLineCount, Is.EqualTo(2));
                Assert.That(set.GetSegmentCount(0), Is.EqualTo(4));
                Assert.That(set.GetSegmentCount(1), Is.EqualTo(1));
                Assert.That(set.SegmentCount, Is.EqualTo(5));
                Assert.That(set.GetPolyLine(0).Last(), Is.EqualTo(V3d.OOO)); // loop closes back onto vertex 0

                // 4) empty and 1-vertex polylines are dropped
                Assert.That(ls[3].LineSet.VertexIndexArray, Is.EqualTo(new[] { 0, 1 }));
                Assert.That(ls[3].LineSet.FirstIndexArray, Is.EqualTo(new[] { 0, 2 }));

                // 5) per-vertex colors, no colorIndex -> coordIndex selects the colors, one per polyline vertex
                Assert.That(ls[4].LineSet.LineAttributes.Count, Is.EqualTo(0));
                Assert.That(LineVertexColors(ls[4]), Is.EqualTo(new[] { s_red, s_green, s_blue }));

                // 6) per-vertex colorIndex, -1 markers stripped -> parallel to VertexIndexArray
                set = ls[5].LineSet;
                Assert.That(set.VertexIndexArray, Is.EqualTo(new[] { 0, 1, 2, 3 }));
                Assert.That(set.FirstIndexArray, Is.EqualTo(new[] { 0, 2, 4 }));
                Assert.That(LineVertexColors(ls[5]), Is.EqualTo(new[] { s_red, s_green, s_green, s_red }));
                Assert.That(LineVertexColors(ls[5]).Length, Is.EqualTo(set.VertexIndexCount));

                // 7) one color per polyline, no colorIndex
                Assert.That(ls[6].LineSet.PolyLineCount, Is.EqualTo(2));
                Assert.That(ls[6].LineSet.LineVertexAttributes.Count, Is.EqualTo(0));
                Assert.That(LineColors(ls[6]), Is.EqualTo(new[] { s_red, s_blue }));

                // 8) per-polyline colorIndex with a dropped empty polyline in between
                //    -> surviving polylines keep the colors of their original ordinals
                Assert.That(ls[7].LineSet.PolyLineCount, Is.EqualTo(2));
                Assert.That(LineColors(ls[7]), Is.EqualTo(new[] { s_red, s_blue }));

                // 9) empty coordIndex -> node is ignored, nothing populated
                Assert.That(ls[8].LineSet, Is.Null);

                // 10) two line sets sharing one Coordinate node via DEF/USE
                Assert.That(ls[9].LineSet.VertexArray, Is.EqualTo(ls[10].LineSet.VertexArray));
                Assert.That(ls[9].LineSet.VertexIndexArray, Is.EqualTo(new[] { 0, 1 }));
                Assert.That(ls[10].LineSet.VertexIndexArray, Is.EqualTo(new[] { 2, 3 }));

                // 11) whole geometry reused via DEF/USE -> one node instance, one line set
                Assert.That(ls[12], Is.SameAs(ls[11]));
                Assert.That(ls[12].LineSet, Is.SameAs(ls[11].LineSet));

                // 12) DEF name ends up in Name
                Assert.That(ls[13].Name, Is.EqualTo("MyPolyline"));

                // 13) non-monotone index order is preserved
                Assert.That(ls[14].LineSet.VertexIndexArray, Is.EqualTo(new[] { 5, 0, 3, 1, 4, 2 }));

                // 14) repeated vertex (zero-length segment) is kept
                Assert.That(ls[15].LineSet.VertexIndexArray, Is.EqualTo(new[] { 0, 1, 1, 2 }));

                // 15) many polylines
                Assert.That(ls[16].LineSet.FirstIndexArray, Is.EqualTo(new[] { 0, 2, 4, 6, 8 }));
                Assert.That(ls[16].LineSet.PolyLineCount, Is.EqualTo(4));

                // 16) leading -1 dropped silently
                Assert.That(ls[17].LineSet.VertexIndexArray, Is.EqualTo(new[] { 0, 1 }));
                Assert.That(ls[17].LineSet.PolyLineCount, Is.EqualTo(1));

                // 17) out-of-range index -> middle polyline dropped, per-polyline
                //     colors remapped to the survivors (red + blue, green skipped)
                Assert.That(ls[18].LineSet.PolyLineCount, Is.EqualTo(2));
                Assert.That(ls[18].LineSet.VertexIndexArray, Is.EqualTo(new[] { 0, 1, 2, 3 }));
                Assert.That(LineColors(ls[18]), Is.EqualTo(new[] { s_red, s_blue }));

                // 18) colorIndex longer than coordIndex is legal; excess is ignored
                Assert.That(LineVertexColors(ls[19]), Is.EqualTo(new[] { s_red, s_green }));

                // 19) per-polyline colorIndex reusing a single color
                Assert.That(LineColors(ls[20]), Is.EqualTo(new[] { s_yellow, s_yellow }));

                // 20) surplus colors are legal
                Assert.That(LineColors(ls[21]), Is.EqualTo(new[] { s_red }));

                // 21) explicit field defaults (color NULL, colorIndex [])
                Assert.That(ls[22].LineSet.LineAttributes.Count, Is.EqualTo(0));
                Assert.That(ls[22].LineSet.LineVertexAttributes.Count, Is.EqualTo(0));
                Assert.That(ls[22].LineSet.VertexIndexArray, Is.EqualTo(new[] { 0, 1 }));

                // 22) comma separated tokens
                Assert.That(ls[23].LineSet.VertexIndexArray, Is.EqualTo(new[] { 0, 1, 2, 0 }));

                // 23) line set inside a full Shape with appearance
                Assert.That(ls[24].LineSet.FirstIndexArray, Is.EqualTo(new[] { 0, 2, 5 }));

                // 24) 0 vertices -> ignored
                Assert.That(ls[25].LineSet, Is.Null);

                // 25) all polylines degenerate -> ignored
                Assert.That(ls[26].LineSet, Is.Null);

                // 26) scientific notation parsed correctly, floats widened to double as they are
                Assert.That(ls[27].LineSet.VertexArray[0], Is.EqualTo(new V3f(-1.5e-3f, 0, 2e2f).ToV3d()));
                Assert.That(ls[27].LineSet.VertexArray[1], Is.EqualTo(new V3f(1, -2.25f, 0.5f).ToV3d()));

                // 27/28) hex indices must yield exactly the same line set as the decimal twin
                Assert.That(ls[28].LineSet.VertexIndexArray, Is.EqualTo(new[] { 0, 1, 2, 3, 10, 11, 12 }));
                Assert.That(ls[28].LineSet.VertexIndexArray, Is.EqualTo(ls[29].LineSet.VertexIndexArray));
                Assert.That(ls[28].LineSet.FirstIndexArray, Is.EqualTo(ls[29].LineSet.FirstIndexArray));

                // 29) hex colorIndex, upper and lower case prefix and digits
                Assert.That(LineColors(ls[30]), Is.EqualTo(new[] { s_blue, s_red }));

                // 30) a vertex shared by two polylines of different colours stays shared in the line set
                Assert.That(ls[31].LineSet.VertexCount, Is.EqualTo(3));
                Assert.That(ls[31].LineSet.VertexIndexArray, Is.EqualTo(new[] { 0, 1, 1, 2 }));
                Assert.That(LineColors(ls[31]), Is.EqualTo(new[] { s_red, s_blue }));
            });
        }

        [Test]
        public static void IndexedLineSetIndexedGeometry()
        {
            LoadEmbeddedData(@"data\IndexedLineSet_Cases.wrl", (filePath) => {

                var scene = LoadScene(filePath);
                var ls = GetLineSets(scene);

                // 1) one 2-point polyline -> indexed line list over the two vertices, positions only
                var ig = ls[0].GetIndexedGeometry();
                Assert.That(ig.Mode, Is.EqualTo(IndexedGeometryMode.LineList));
                Assert.That((int[])ig.IndexArray, Is.EqualTo(new[] { 0, 1 }));
                Assert.That((V3f[])ig.IndexedAttributes[DefaultSemantic.Positions], Is.EqualTo(new[] { V3f.OOO, V3f.OIO }));
                Assert.That(ig.IndexedAttributes.Count, Is.EqualTo(1));

                // 3) closed loop (5 indices -> 4 segments) plus a 2-point polyline (1 segment): 6 vertices stay shared, 5 segments indexed
                ig = ls[2].GetIndexedGeometry();
                Assert.That(ig.IndexedAttributes[DefaultSemantic.Positions].Length, Is.EqualTo(6));
                Assert.That(((int[])ig.IndexArray).Length, Is.EqualTo(10));
                Assert.That(((int[])ig.IndexArray)[7], Is.EqualTo(0)); // loop closes back onto vertex 0

                // 5) per-vertex colors without colorIndex -> one colour per vertex, nothing to split
                ig = ls[4].GetIndexedGeometry();
                Assert.That(ig.IndexedAttributes[DefaultSemantic.Positions].Length, Is.EqualTo(3));
                Assert.That((C4b[])ig.IndexedAttributes[DefaultSemantic.Colors], Is.EqualTo(new[] { s_red, s_green, s_blue }));

                // 6) per-vertex colors via colorIndex, 2 polylines with swapped colors
                Assert.That((C4b[])ls[5].GetIndexedGeometry().IndexedAttributes[DefaultSemantic.Colors], Is.EqualTo(new[] { s_red, s_green, s_green, s_red }));

                // 7) per-polyline colors -> every vertex of a polyline gets its colour
                Assert.That((C4b[])ls[6].GetIndexedGeometry().IndexedAttributes[DefaultSemantic.Colors], Is.EqualTo(new[] { s_red, s_red, s_blue, s_blue }));

                // 17) dropped polyline -> surviving vertices keep the colors of their original ordinals
                Assert.That((C4b[])ls[18].GetIndexedGeometry().IndexedAttributes[DefaultSemantic.Colors], Is.EqualTo(new[] { s_red, s_red, s_blue, s_blue }));

                // 30) a vertex shared by a red and a blue polyline is split for rendering, the other two stay shared
                ig = ls[31].GetIndexedGeometry();
                var pos = (V3f[])ig.IndexedAttributes[DefaultSemantic.Positions];
                Assert.That(pos, Is.EqualTo(new[] { V3f.OOO, V3f.IOO, new V3f(2, 0, 0), V3f.IOO }));
                Assert.That((int[])ig.IndexArray, Is.EqualTo(new[] { 0, 1, 3, 2 }));
                Assert.That((C4b[])ig.IndexedAttributes[DefaultSemantic.Colors], Is.EqualTo(new[] { s_red, s_red, s_blue, s_blue }));
                Assert.That(ls[31].LineSet.VertexCount, Is.EqualTo(3)); // the line set itself is untouched

                // 9) ignored line set -> no geometry at all
                Assert.That(ls[8].GetIndexedGeometry(), Is.Null);
                Assert.That(((VrmlGeometry)ls[8]).GetIndexedGeometry(), Is.Null);

                // the line sets are reachable through the VrmlGeometry base class, so consumers do not
                // have to distinguish geometry types, and PrimitivesToMeshes must leave them untouched
                scene.PrimitivesToMeshes();
                var geometries = GetLineSets(scene);
                Assert.That(geometries.Count, Is.EqualTo(32));
                Assert.That(((VrmlGeometry)geometries[0]).GetIndexedGeometry(), Is.Not.Null);
            });
        }

        /// <summary>
        /// Smoke test over the official Vrml97 specification examples (see data\Vrml97Spec\README.md).
        /// NOTE: none of them contains an IndexedLineSet, so this covers the loader as a whole
        ///       (Script, Extrusion, Text, LOD, Anchor, sensors, interpolators, PROTOs, ...) and not the line set implementation.
        /// </summary>
        [Test]
        [TestCaseSource(nameof(Vrml97SpecScenes))]
        public static void Vrml97SpecificationExample(string dataPath)
        {
            LoadEmbeddedData(dataPath, (filePath) => {

                var scene = LoadScene(filePath);
                Assert.That(scene, Is.Not.Null);

                // the whole pipeline must survive every scene: meshing the primitives and converting every geometry to a renderable IndexedGeometry
                scene.PrimitivesToMeshes();
                foreach (var shape in GetShapes(scene))
                {
                    Assert.That(() => shape.Geometry?.GetIndexedGeometry(), Throws.Nothing);
                }
            });
        }

        // spec violations - each file contains exactly one IndexedLineSet that MUST throw
        [Test]
        [TestCase(@"data\IndexedLineSet_Invalid_01.wrl")] // coord NULL
        [TestCase(@"data\IndexedLineSet_Invalid_02.wrl")] // negative colorIndex (per-polyline)
        [TestCase(@"data\IndexedLineSet_Invalid_03.wrl")] // colorIndex exceeds color count
        [TestCase(@"data\IndexedLineSet_Invalid_04.wrl")] // colorIndex shorter than coordIndex (per-vertex)
        [TestCase(@"data\IndexedLineSet_Invalid_05.wrl")] // -1 markers misaligned
        [TestCase(@"data\IndexedLineSet_Invalid_06.wrl")] // too few colors (per-vertex)
        [TestCase(@"data\IndexedLineSet_Invalid_07.wrl")] // too few colors (per-polyline)
        [TestCase(@"data\IndexedLineSet_Invalid_08.wrl")] // colorIndex shorter than polyline count
        public static void IndexedLineSetSpecViolations(string dataPath)
        {
            LoadEmbeddedData(dataPath, (filePath) => {
                Assert.That(() => LoadScene(filePath), Throws.Exception);
            });
        }
    }
}