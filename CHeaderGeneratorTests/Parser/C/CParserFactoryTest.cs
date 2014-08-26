using System;
using System.Linq;
#if !NUNIT
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Category = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;
#else
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
using TestInitialize = NUnit.Framework.SetUpAttribute;
using TestCleanup = NUnit.Framework.TearDownAttribute;
using TestContext = System.Object;
#endif
using Assert = NUnit.Framework.Assert;
using Throws = NUnit.Framework.Throws;
using Is = NUnit.Framework.Is;
using System.IO;
using CHeaderGenerator.Parser.C;

namespace CHeaderGeneratorTests.Parser.C
{
    [TestClass]
    public class CParserFactoryTest
    {
        [TestMethod]
        public void TestCreateParser()
        {
            using (var stream = new MemoryStream())
            {
                var factory = new CParserFactory();

                var parser = factory.CreateParser(stream);
                Assert.That(parser, Is.Not.Null);
            }
        }
    }
}
