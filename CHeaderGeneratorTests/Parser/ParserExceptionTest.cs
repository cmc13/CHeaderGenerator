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
    public class ParserExceptionTest
    {
        [TestMethod]
        public void TestParserExceptionMessage()
        {
            var ex = new ParserException("This is the exception.", null, 0, 0);

            Assert.That(ex.Message, Is.EqualTo("This is the exception." + Environment.NewLine
                + "The error occurred at position 0 on line 0."));
        }
    }
}
