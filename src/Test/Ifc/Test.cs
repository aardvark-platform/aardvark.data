using Aardvark.Base;
using Aardvark.Rendering;
using Aardvark.Data.Ifc;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IfcTest
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
    }
}