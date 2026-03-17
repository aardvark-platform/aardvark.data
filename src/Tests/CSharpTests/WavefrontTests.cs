using Aardvark.Base;
using Aardvark.Data.Wavefront;
using Aardvark.Geometry;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Aardvark.Data.Wavefront.Tests
{
    [TestFixture]
    public class WavefrontTests
    {
        static void WithTempDirectory(Action<string> test)
        {
            var root = Path.Combine(Path.GetTempPath(), "aardvark-wavefront-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);

            try
            {
                test(root);
            }
            finally
            {
                try
                {
                    if (Directory.Exists(root))
                        Directory.Delete(root, recursive: true);
                }
                catch
                {
                }
            }
        }

        static string WriteFile(string directory, string fileName, string content, Encoding encoding = null)
        {
            var path = Path.Combine(directory, fileName);
            File.WriteAllText(path, content, encoding ?? new UTF8Encoding(false));
            return path;
        }

        static void AssertCanOpenExclusive(string path)
        {
            Assert.DoesNotThrow(() =>
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                Assert.That(stream.Length, Is.GreaterThanOrEqualTo(0));
            });
        }

        static PolyMesh CreateTriangleMesh(bool withNormals = false, bool withTexCoords = false)
        {
            var mesh = new PolyMesh
            {
                PositionArray = new[]
                {
                    new V3d(0, 0, 0),
                    new V3d(1, 0, 0),
                    new V3d(0, 1, 0),
                },
                FirstIndexArray = new[] { 0, 3 },
                VertexIndexArray = new[] { 0, 1, 2 }
            };

            if (withNormals)
            {
                mesh.VertexAttributes[PolyMesh.Property.Normals] = new[]
                {
                    V3d.ZAxis,
                    V3d.ZAxis,
                    V3d.ZAxis,
                };
            }

            if (withTexCoords)
            {
                mesh.VertexAttributes[PolyMesh.Property.DiffuseColorCoordinates] = new[]
                {
                    new V2d(0, 0),
                    new V2d(1, 0),
                    new V2d(0, 1),
                };
            }

            return mesh;
        }

        static string[] ReadFaceLines(string objPath)
            => File.ReadLines(objPath).Where(l => l.StartsWith("f ")).ToArray();

        [Test]
        public void ObjParser_Load_PointSetNegativeIndexMinus1_ResolvesToLastVertex()
        {
            WithTempDirectory(dir =>
            {
                var path = WriteFile(dir, "points.obj", "v 1 0 0\nv 2 0 0\nv 3 0 0\np -1\n");

                var obj = ObjParser.Load(path);
                var pointSet = obj.PointsSets.Single();

                Assert.AreEqual(2, pointSet.VertexIndices.Single());
            });
        }

        [Test]
        public void ObjParser_Load_PointSetNegativeIndices_ResolveRelativeToEnd()
        {
            WithTempDirectory(dir =>
            {
                var path = WriteFile(dir, "points.obj", "v 1 0 0\nv 2 0 0\nv 3 0 0\nv 4 0 0\np -1 -2 -4\n");

                var obj = ObjParser.Load(path);
                var pointSet = obj.PointsSets.Single();

                CollectionAssert.AreEqual(new[] { 3, 2, 0 }, pointSet.VertexIndices);
            });
        }

        [Test]
        public void ObjParser_Load_VertexColors_ReadsBlueChannelCorrectly()
        {
            WithTempDirectory(dir =>
            {
                var path = WriteFile(dir, "colors.obj", "v 0 0 0 0.1 0.2 0.3\n");

                var obj = ObjParser.Load(path);
                var color = obj.VertexColors.Single();

                Assert.That(color.R, Is.EqualTo(0.1f).Within(1e-6f));
                Assert.That(color.G, Is.EqualTo(0.2f).Within(1e-6f));
                Assert.That(color.B, Is.EqualTo(0.3f).Within(1e-6f));
            });
        }

        [Test]
        public void ObjParser_Load_ContinuedVertexLine_JoinsWithoutLiteralBackslash()
        {
            WithTempDirectory(dir =>
            {
                var path = WriteFile(dir, "continued.obj", "v 1 2 \\\n3\n");

                var obj = ObjParser.Load(path);
                var vertex = ((System.Collections.Generic.List<V4d>)obj.Vertices).Single();

                Assert.AreEqual(1, obj.Vertices.Count);
                Assert.That(vertex.X, Is.EqualTo(1.0).Within(1e-12));
                Assert.That(vertex.Y, Is.EqualTo(2.0).Within(1e-12));
                Assert.That(vertex.Z, Is.EqualTo(3.0).Within(1e-12));
            });
        }

        [Test]
        public void MtlParser_Load_WithExplicitUtf8Encoding_UsesProvidedEncoding()
        {
            WithTempDirectory(dir =>
            {
                var unicode = new UnicodeEncoding(bigEndian: false, byteOrderMark: false);
                var path = WriteFile(dir, "materials.mtl", "newmtl mät\nKd 1 0 0\n", unicode);

                var materials = MtlParser.Load(path, unicode);

                Assert.AreEqual("mät", materials.Single().Name);
            });
        }

        [Test]
        public void MtlParser_Load_FileOverload_ReleasesInputHandle()
        {
            WithTempDirectory(dir =>
            {
                var path = WriteFile(dir, "materials.mtl", "newmtl mat\nKd 1 0 0\n");

                _ = MtlParser.Load(path);

                AssertCanOpenExclusive(path);
            });
        }

        [Test]
        public void ObjParser_Load_FileOverload_ReleasesObjHandle()
        {
            WithTempDirectory(dir =>
            {
                var path = WriteFile(dir, "mesh.obj", "v 0 0 0\nv 1 0 0\nv 0 1 0\nf 1 2 3\n");

                _ = ObjParser.Load(path);

                AssertCanOpenExclusive(path);
            });
        }

        [Test]
        public void ObjParser_Load_WithReferencedMtl_ReleasesReferencedMtlHandle()
        {
            WithTempDirectory(dir =>
            {
                var mtlPath = WriteFile(dir, "mesh.mtl", "newmtl mat\nKd 1 0 0\n");
                var objPath = WriteFile(dir, "mesh.obj", "mtllib mesh.mtl\nusemtl mat\nv 0 0 0\nv 1 0 0\nv 0 1 0\nf 1 2 3\n");

                _ = ObjParser.Load(objPath);

                AssertCanOpenExclusive(mtlPath);
            });
        }

        [Test]
        public void GetFaceSetMeshes_MixedMissingAndResolvedMaterials_EmitsMaterialFaceAttributes()
        {
            WithTempDirectory(dir =>
            {
                var mtlPath = WriteFile(dir, "mesh.mtl", "newmtl red\nKd 1 0 0\n");
                var objPath = WriteFile(dir, "mesh.obj",
                    "mtllib mesh.mtl\n" +
                    "g group0\n" +
                    "v 0 0 0\n" +
                    "v 1 0 0\n" +
                    "v 1 1 0\n" +
                    "v 0 1 0\n" +
                    "f 1 2 3\n" +
                    "usemtl red\n" +
                    "f 1 3 4\n");

                var obj = ObjParser.Load(objPath);
                var mesh = obj.GetFaceSetMeshes().Single();

                Assert.IsTrue(mesh.FaceAttributes.Contains(PolyMesh.Property.Material));
                CollectionAssert.AreEqual(new[] { -1, 0 }, (int[])mesh.FaceAttributes[PolyMesh.Property.Material]);
                Assert.AreEqual("red", ((WavefrontMaterial[])mesh.FaceAttributes[-PolyMesh.Property.Material]).Single().Name);
            });
        }

        [Test]
        public void Exporter_SaveToFile_WithTexcoordsOnly_WritesVSlashVt()
        {
            WithTempDirectory(dir =>
            {
                var path = Path.Combine(dir, "tex-only.obj");
                Exporter.SaveToFile(new[] { CreateTriangleMesh(withTexCoords: true) }, path);

                var faceLine = ReadFaceLines(path).Single();

                Assert.AreEqual("f 1/1 2/2 3/3", faceLine);
            });
        }

        [Test]
        public void Exporter_SaveToFile_WithNormalsOnly_WritesVDoubleSlashVn()
        {
            WithTempDirectory(dir =>
            {
                var path = Path.Combine(dir, "normals-only.obj");
                Exporter.SaveToFile(new[] { CreateTriangleMesh(withNormals: true) }, path);

                var faceLine = ReadFaceLines(path).Single();

                Assert.AreEqual("f 1//1 2//2 3//3", faceLine);
            });
        }

        [Test]
        public void Exporter_SaveToFile_WithTexcoordsAndNormals_WritesVSlashVtSlashVn()
        {
            WithTempDirectory(dir =>
            {
                var path = Path.Combine(dir, "both.obj");
                Exporter.SaveToFile(new[]
                {
                    CreateTriangleMesh(withNormals: true),
                    CreateTriangleMesh(withNormals: true, withTexCoords: true)
                }, path);

                var faceLine = ReadFaceLines(path).Last();

                Assert.AreEqual("f 4/1/4 5/2/5 6/3/6", faceLine);
            });
        }
    }
}
