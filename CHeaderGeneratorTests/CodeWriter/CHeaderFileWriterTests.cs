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
using CHeaderGenerator.CodeWriter;
using CHeaderGenerator.Data;
using System.IO;
using System.Text;

namespace CHeaderGeneratorTests.CodeWriter
{
    [TestClass]
    public class CHeaderFileWriterTests
    {
        [TestMethod]
        public void TestWritingHeaderComment()
        {
            var writer = new CHeaderFileWriter() { HeaderComment = "test" };

            using (var stream = new MemoryStream())
            {
                writer.WriteHeaderFile(new CSourceFile(), stream);
                var bytes = stream.ToArray();
                var str = Encoding.ASCII.GetString(bytes);

                Assert.That(str, Is.Not.Null.And.EqualTo("test\r\n\r\n"));
            }
        }

        [TestMethod]
        public void TestWritingIncludeGuard()
        {
            var writer = new CHeaderFileWriter() { IncludeGuard = "test" };

            using (var stream = new MemoryStream())
            {
                writer.WriteHeaderFile(new CSourceFile(), stream);
                var bytes = stream.ToArray();
                var str = Encoding.ASCII.GetString(bytes);

                Assert.That(str, Is.Not.Null.And.EqualTo("#ifndef test\r\n#define test\r\n\r\n\r\n#endif\r\n"));
            }
        }
    }
}
