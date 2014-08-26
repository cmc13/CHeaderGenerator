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
using System.Text;
using CHeaderGenerator.Parser;
using Moq;
using CHeaderGenerator.Parser.C;

namespace CHeaderGeneratorTests.Parser.C
{
    [TestClass]
    public class CLexerTest
    {
        [TestMethod]
        public void TestConstructorWithNullTokenizerThrowsException()
        {
            Assert.That(() => new CLexer(null), Throws.TypeOf(typeof(ArgumentNullException)));
        }

        [TestMethod]
        public void TestHasMoreTokensReturnsValueFromTokenizer()
        {
            var m = new Mock<ITokenizer<TokenType>>();
            m.SetupGet(t => t.HasMoreTokens).Returns(true);

            var l = new CLexer(m.Object);
            Assert.That(l.HasMoreTokens, Is.True);
        }

        [TestMethod]
        public void TestPushToken()
        {
            var token = CToken.Create(new Token<TokenType>());
            var m = new Mock<ITokenizer<TokenType>>();

            var l = new CLexer(m.Object);
            l.PushToken(token);

            var otherToken = l.GetNextToken();
            Assert.That(otherToken, Is.EqualTo(token));
        }

        [TestMethod]
        public void TestSkipToNextLine()
        {
            var m = new Mock<ITokenizer<TokenType>>();
            m.Setup(t => t.SkipToNextLine());

            var l = new CLexer(m.Object);
            l.SkipToNextLine();

            m.Verify(t => t.SkipToNextLine());
        }

        [TestMethod]
        public void TestNextTokenReturnsNull()
        {
            var m = new Mock<ITokenizer<TokenType>>();
            m.Setup(t => t.GetNextToken(It.IsAny<bool>())).Returns((Token<TokenType>)null);

            var l = new CLexer(m.Object);
            var token = l.GetNextToken();

            Assert.That(token, Is.Null);
        }

        [TestMethod]
        public void TestNextTokenReturnsValidToken()
        {
            var m = new Mock<ITokenizer<TokenType>>();
            m.Setup(t => t.GetNextToken(It.IsAny<bool>())).Returns(new Token<TokenType>());

            var l = new CLexer(m.Object);
            var token = l.GetNextToken();

            Assert.That(token, Is.Not.Null);
        }
    }
}
