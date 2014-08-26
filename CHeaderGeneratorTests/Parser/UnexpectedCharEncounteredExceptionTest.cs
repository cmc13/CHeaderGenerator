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
using CHeaderGenerator.Parser;

namespace CHeaderGeneratorTests.Parser
{
    [TestClass]
    public class UnexpectedCharEncounteredExceptionTest
    {
        [TestMethod]
        public void TestDefaultConstructorForUnexpectedCharEncounteredException()
        {
            Assert.That(() => new UnexpectedCharEncounteredException(), Throws.Nothing);
        }
    }
}
