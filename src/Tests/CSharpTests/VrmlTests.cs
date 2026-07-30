using Aardvark.Base;
using NUnit.Framework;
using System.IO;
using System.Linq;

namespace Aardvark.Data.Vrml97
{
    [TestFixture]
    public class VrmlTests
    {
        static readonly string VrmlFilePath = @"C:\- Work\VRVis.Hilite\Bin\Resources\DemoLights";
        //static readonly string VrmlFilePath = @"C:\- Work\Hilite Scenes";

        [Test, Ignore("Requires Data Files")]
        public void ParseTest()
        {
            var extensions = new[] { ".vrml", ".wrl", "*.wrz", ".wrl.gz" }.ToHashSet();
            var files = Directory.GetFiles(VrmlFilePath, "*.*", SearchOption.AllDirectories)
                                 .Where(f => extensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                                 .ToArray();
            foreach (var f in files)
            {
                Report.BeginTimed($"parsing: {f}");
                var vrmlParseTree = Vrml97Scene.FromFile(f);
                Report.End();

                Report.BeginTimed("creating scene graph");
                var vrml = SceneLoader.Load(vrmlParseTree, out var nodeMap);
                Report.End();
            }
        }
    }
}
