using Aardvark.Base;
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
        }

        [OneTimeTearDown]
        public void DeInit()
        {
        }
    }
}
