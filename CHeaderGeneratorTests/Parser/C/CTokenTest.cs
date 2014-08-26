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
using System.IO;
using System.Text;
using System.Collections.Generic;
using Moq;
using CHeaderGenerator.Parser.C;

namespace CHeaderGeneratorTests.Parser.C
{
    [TestClass]
    public class CTokenTest
    {
        [TestMethod]
        public void TestCreatePPSymbolToken()
        {
            var token = CToken.Create(new Token<TokenType> { Type = TokenType.PUNCTUATOR, Value = "#" });
            Assert.That(token.Type, Is.EqualTo(CTokenType.PP_SYMBOL));
        }

        [TestMethod]
        public void TestCreateTerminatorToken()
        {
            var token = CToken.Create(new Token<TokenType> { Type = TokenType.PUNCTUATOR, Value = ";" });
            Assert.That(token.Type, Is.EqualTo(CTokenType.TERMINATOR));
        }

        [TestMethod]
        public void TestCreatePunctuatorToken()
        {
            var token = CToken.Create(new Token<TokenType> { Type = TokenType.PUNCTUATOR });
            Assert.That(token.Type, Is.EqualTo(CTokenType.PUNCTUATOR));
        }

        [TestMethod]
        public void TestCreateCharLiteralToken()
        {
            var token = CToken.Create(new Token<TokenType> { Type = TokenType.CHAR_LITERAL });
            Assert.That(token.Type, Is.EqualTo(CTokenType.CHAR_LITERAL));
        }

        [TestMethod]
        public void TestCreateStringLiteralToken()
        {
            var token = CToken.Create(new Token<TokenType> { Type = TokenType.STRING_LITERAL });
            Assert.That(token.Type, Is.EqualTo(CTokenType.STRING_LITERAL));
        }

        [TestMethod]
        public void TestCreateNumericLiteralToken()
        {
            var token = CToken.Create(new Token<TokenType> { Type = TokenType.NUMERIC_LITERAL });
            Assert.That(token.Type, Is.EqualTo(CTokenType.NUMERIC_LITERAL));
        }

        [TestMethod]
        public void TestCreateMainTargetIdentifierToken()
        {
            var token = CToken.Create(new Token<TokenType> { Type = TokenType.SYMBOL, Value = "main" });
            Assert.That(token.Type, Is.EqualTo(CTokenType.TARGET_IDENTIFIER));
        }

        [TestMethod]
        public void TestCreateMainProgramTargetIdentifierToken()
        {
            var token = CToken.Create(new Token<TokenType> { Type = TokenType.SYMBOL, Value = "main_program" });
            Assert.That(token.Type, Is.EqualTo(CTokenType.TARGET_IDENTIFIER));
        }

        [TestMethod]
        public void TestCreateSymbolToken()
        {
            var token = CToken.Create(new Token<TokenType> { Type = TokenType.SYMBOL });
            Assert.That(token.Type, Is.EqualTo(CTokenType.SYMBOL));
        }

        [TestMethod]
        public void TestCreatePPDirectiveToken()
        {
            var token = CToken.Create(new Token<TokenType> { Type = TokenType.SYMBOL, Value = "ifdef" },
                new Token<TokenType> { Type = TokenType.PUNCTUATOR, Value = "#" });
            Assert.That(token.Type, Is.EqualTo(CTokenType.PP_DIRECTIVE));
        }

        [TestMethod]
        public void TestCreateNonPPDirectiveToken()
        {
            var token = CToken.Create(new Token<TokenType> { Type = TokenType.SYMBOL, Value = "ifdef" });
            Assert.That(token.Type, Is.EqualTo(CTokenType.SYMBOL));
        }

        [TestMethod]
        public void TestCreateIfPPDirectiveToken()
        {
            var token = CToken.Create(new Token<TokenType> { Type = TokenType.SYMBOL, Value = "if" },
                new Token<TokenType> { Type = TokenType.PUNCTUATOR, Value = "#" });
            Assert.That(token.Type, Is.EqualTo(CTokenType.PP_DIRECTIVE));
        }

        [TestMethod]
        public void TestCreateIfKeyword()
        {
            var token = CToken.Create(new Token<TokenType> { Type = TokenType.SYMBOL, Value = "if" });
            Assert.That(token.Type, Is.EqualTo(CTokenType.KEYWORD));
        }

        [TestMethod]
        public void TestCreateTypeSpecifier()
        {
            var token = CToken.Create(new Token<TokenType> { Type = TokenType.SYMBOL, Value = "int" });
            Assert.That(token.Type, Is.EqualTo(CTokenType.TYPE_SPECIFIER));
        }

        [TestMethod]
        public void TestCreateStructureSpecifier()
        {
            var token = CToken.Create(new Token<TokenType> { Type = TokenType.SYMBOL, Value = "struct" });
            Assert.That(token.Type, Is.EqualTo(CTokenType.STRUCTURE_SPECIFIER));
        }

        [TestMethod]
        public void TestCreateEnumSpecifier()
        {
            var token = CToken.Create(new Token<TokenType> { Type = TokenType.SYMBOL, Value = "enum" });
            Assert.That(token.Type, Is.EqualTo(CTokenType.ENUM_SPECIFIER));
        }

        [TestMethod]
        public void TestCreateKeyword()
        {
            var token = CToken.Create(new Token<TokenType> { Type = TokenType.SYMBOL, Value = "while" });
            Assert.That(token.Type, Is.EqualTo(CTokenType.KEYWORD));
        }
    }
}
