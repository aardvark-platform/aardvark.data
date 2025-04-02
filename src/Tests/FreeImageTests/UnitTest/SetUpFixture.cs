using Aardvark.Base;
using Aardvark.Data;
using NUnit.Framework;

namespace FreeImageNETUnitTest
{
    [SetUpFixture]
    public class SetUpFixture
    {
        [OneTimeSetUp]
        public void Init()
        {
            IntrospectionProperties.CustomEntryAssembly = typeof(SetUpFixture).Assembly;
            Aardvark.Base.Aardvark.Init();
            PixImageFreeImage.Init();
        }

        [OneTimeTearDown]
        public void DeInit()
        {
        }
    }
}
