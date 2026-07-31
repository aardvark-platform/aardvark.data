using Aardvark.Base;
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

        #endregion

        [Test]
        public static void IndexedLineSetValidCases()
        {
            LoadEmbeddedData(@"data\IndexedLineSet_Cases.wrl", (filePath) => {

                var scene = LoadScene(filePath);
                var ls = GetLineSets(scene);

                // 28 Shapes carrying an IndexedLineSet (cases 10 and 11 have two each)
                Assert.That(ls.Count, Is.EqualTo(28));

                // 1) single 2-point polyline (the only case in "00 Tracks")
                Assert.That(ls[0].VertexArray, Is.EqualTo(new[] { V3f.OOO, V3f.OIO }));
                Assert.That(ls[0].VertexIndexArray, Is.EqualTo(new[] { 0, 1 }));
                Assert.That(ls[0].FirstIndexArray, Is.EqualTo(new[] { 0, 2 }));
                Assert.That(ls[0].PolyLineCount, Is.EqualTo(1));
                Assert.That(ls[0].ColorArray, Is.Null);

                // 2) no trailing -1
                Assert.That(ls[1].VertexIndexArray, Is.EqualTo(new[] { 0, 1, 2 }));
                Assert.That(ls[1].FirstIndexArray, Is.EqualTo(new[] { 0, 3 }));

                // 3) several polylines, shared vertex, closed loop
                Assert.That(ls[2].VertexIndexArray, Is.EqualTo(new[] { 0, 1, 2, 3, 0, 4, 5 }));
                Assert.That(ls[2].FirstIndexArray, Is.EqualTo(new[] { 0, 5, 7 }));
                Assert.That(ls[2].PolyLineCount, Is.EqualTo(2));

                // 4) empty and 1-vertex polylines are dropped
                Assert.That(ls[3].VertexIndexArray, Is.EqualTo(new[] { 0, 1 }));
                Assert.That(ls[3].FirstIndexArray, Is.EqualTo(new[] { 0, 2 }));

                // 5) per-vertex colors, no colorIndex -> coordIndex doubles as color index
                Assert.That(ls[4].PerVertexColors, Is.True);
                Assert.That(ls[4].ColorArray.Length, Is.EqualTo(3));
                Assert.That(ls[4].ColorIndexArray, Is.Null);

                // 6) per-vertex colorIndex, -1 markers stripped -> parallel to VertexIndexArray
                Assert.That(ls[5].VertexIndexArray, Is.EqualTo(new[] { 0, 1, 2, 3 }));
                Assert.That(ls[5].FirstIndexArray, Is.EqualTo(new[] { 0, 2, 4 }));
                Assert.That(ls[5].ColorIndexArray, Is.EqualTo(new[] { 0, 1, 1, 0 }));
                Assert.That(ls[5].ColorIndexArray.Length, Is.EqualTo(ls[5].VertexIndexArray.Length));

                // 7) one color per polyline, no colorIndex
                Assert.That(ls[6].PerVertexColors, Is.False);
                Assert.That(ls[6].ColorArray.Length, Is.EqualTo(2));
                Assert.That(ls[6].ColorIndexArray, Is.Null);
                Assert.That(ls[6].PolyLineCount, Is.EqualTo(2));

                // 8) per-polyline colorIndex with a dropped empty polyline in between
                //    -> surviving polylines keep the colors of their original ordinals
                Assert.That(ls[7].PolyLineCount, Is.EqualTo(2));
                Assert.That(ls[7].ColorIndexArray, Is.EqualTo(new[] { 0, 2 }));

                // 9) empty coordIndex -> node is ignored, nothing populated
                Assert.That(ls[8].VertexArray, Is.Null);
                Assert.That(ls[8].PolyLineCount, Is.EqualTo(0));

                // 10) two line sets sharing one Coordinate node via DEF/USE
                Assert.That(ls[9].VertexArray, Is.EqualTo(ls[10].VertexArray));
                Assert.That(ls[9].VertexIndexArray, Is.EqualTo(new[] { 0, 1 }));
                Assert.That(ls[10].VertexIndexArray, Is.EqualTo(new[] { 2, 3 }));

                // 11) whole geometry reused via DEF/USE
                Assert.That(ls[12].VertexIndexArray, Is.EqualTo(ls[11].VertexIndexArray));

                // 12) DEF name ends up in Name
                Assert.That(ls[13].Name, Is.EqualTo("MyPolyline"));

                // 13) non-monotone index order is preserved
                Assert.That(ls[14].VertexIndexArray, Is.EqualTo(new[] { 5, 0, 3, 1, 4, 2 }));

                // 14) repeated vertex (zero-length segment) is kept
                Assert.That(ls[15].VertexIndexArray, Is.EqualTo(new[] { 0, 1, 1, 2 }));

                // 15) many polylines
                Assert.That(ls[16].FirstIndexArray, Is.EqualTo(new[] { 0, 2, 4, 6, 8 }));
                Assert.That(ls[16].PolyLineCount, Is.EqualTo(4));

                // 16) leading -1 dropped silently
                Assert.That(ls[17].VertexIndexArray, Is.EqualTo(new[] { 0, 1 }));
                Assert.That(ls[17].PolyLineCount, Is.EqualTo(1));

                // 17) out-of-range index -> middle polyline dropped, per-polyline
                //     colors remapped to the survivors (red + blue, green skipped)
                Assert.That(ls[18].PolyLineCount, Is.EqualTo(2));
                Assert.That(ls[18].VertexIndexArray, Is.EqualTo(new[] { 0, 1, 2, 3 }));
                Assert.That(ls[18].PerVertexColors, Is.False);
                Assert.That(ls[18].ColorIndexArray, Is.EqualTo(new[] { 0, 2 }));

                // 18) colorIndex longer than coordIndex is legal; excess is ignored
                Assert.That(ls[19].ColorIndexArray, Is.EqualTo(new[] { 0, 1 }));

                // 19) per-polyline colorIndex reusing a single color
                Assert.That(ls[20].ColorIndexArray, Is.EqualTo(new[] { 0, 0 }));

                // 20) surplus colors are legal
                Assert.That(ls[21].ColorArray.Length, Is.EqualTo(5));

                // 21) explicit field defaults (color NULL, colorIndex [])
                Assert.That(ls[22].ColorArray, Is.Null);
                Assert.That(ls[22].VertexIndexArray, Is.EqualTo(new[] { 0, 1 }));

                // 22) comma separated tokens
                Assert.That(ls[23].VertexIndexArray, Is.EqualTo(new[] { 0, 1, 2, 0 }));

                // 23) line set inside a full Shape with appearance
                Assert.That(ls[24].FirstIndexArray, Is.EqualTo(new[] { 0, 2, 5 }));

                // 24) 0 vertices -> ignored
                Assert.That(ls[25].VertexArray, Is.Null);

                // 25) all polylines degenerate -> ignored
                Assert.That(ls[26].VertexArray, Is.Null);

                // 26) scientific notation parsed correctly
                Assert.That(ls[27].VertexArray[0], Is.EqualTo(new V3f(-1.5e-3f, 0, 2e2f)));
                Assert.That(ls[27].VertexArray[1], Is.EqualTo(new V3f(1, -2.25f, 0.5f)));
            });
        }

        [Test]
        public static void IndexedLineSetIndexedGeometry()
        {
            LoadEmbeddedData(@"data\IndexedLineSet_Cases.wrl", (filePath) => {

                var scene = LoadScene(filePath);
                var ls = GetLineSets(scene);

                // the colors used in the test scene, converted the same way as VrmlLineSet does
                // NOTE: do not use the C4b constants here, C4b.Green is the X11 green (0, 128, 0) and not pure green
                var red = new C4b(new C3f(1, 0, 0));
                var green = new C4b(new C3f(0, 1, 0));
                var blue = new C4b(new C3f(0, 0, 1));

                // 1) one 2-point polyline -> 1 segment -> 2 expanded vertices, no colors
                var ig = ls[0].GetIndexedGeometry();
                Assert.That(ig.Mode, Is.EqualTo(IndexedGeometryMode.LineList));
                Assert.That(ig.IndexArray, Is.Null); // polylines are expanded, so no indexing is needed
                var pos = (V3f[])ig.IndexedAttributes[DefaultSemantic.Positions];
                Assert.That(pos, Is.EqualTo(new[] { V3f.OOO, V3f.OIO }));
                Assert.That(ig.IndexedAttributes.Count, Is.EqualTo(1)); // positions only

                // 3) closed loop (5 indices -> 4 segments) plus a 2-point polyline (1 segment)
                Assert.That(ls[2].GetSegmentCount(0), Is.EqualTo(4));
                Assert.That(ls[2].GetSegmentCount(1), Is.EqualTo(1));
                Assert.That(ls[2].SegmentCount, Is.EqualTo(5));
                pos = (V3f[])ls[2].GetIndexedGeometry().IndexedAttributes[DefaultSemantic.Positions];
                Assert.That(pos.Length, Is.EqualTo(10)); // 5 segments * 2 vertices
                Assert.That(pos[0], Is.EqualTo(V3f.OOO));
                Assert.That(pos[7], Is.EqualTo(V3f.OOO)); // loop closes back onto vertex 0

                // 5) per-vertex colors without colorIndex -> VertexIndexArray selects the colors
                var col = (C4b[])ls[4].GetIndexedGeometry().IndexedAttributes[DefaultSemantic.Colors];
                Assert.That(col.Length, Is.EqualTo(4)); // 2 segments * 2 vertices
                Assert.That(col, Is.EqualTo(new[] { red, green, green, blue }));

                // 6) per-vertex colors via colorIndex, 2 polylines with swapped colors
                col = (C4b[])ls[5].GetIndexedGeometry().IndexedAttributes[DefaultSemantic.Colors];
                Assert.That(col, Is.EqualTo(new[] { red, green, green, red }));

                // 7) per-polyline colors -> both vertices of a segment share the polyline color
                col = (C4b[])ls[6].GetIndexedGeometry().IndexedAttributes[DefaultSemantic.Colors];
                Assert.That(col, Is.EqualTo(new[] { red, red, blue, blue }));

                // 17) dropped polyline -> surviving segments keep the colors of their original ordinals
                col = (C4b[])ls[18].GetIndexedGeometry().IndexedAttributes[DefaultSemantic.Colors];
                Assert.That(col, Is.EqualTo(new[] { red, red, blue, blue }));

                // 9) ignored line set -> no geometry at all
                Assert.That(ls[8].GetIndexedGeometry(), Is.Null);

                // the line sets are reachable through the VrmlGeometry base class, so consumers do not
                // have to distinguish geometry types, and PrimitivesToMeshes must leave them untouched
                scene.PrimitivesToMeshes();
                var geometries = GetLineSets(scene);
                Assert.That(geometries.Count, Is.EqualTo(28));
                Assert.That(((VrmlGeometry)geometries[0]).GetIndexedGeometry(), Is.Not.Null);
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