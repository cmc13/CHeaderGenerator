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
using CHeaderGenerator.Data;
using System.Collections.Generic;
using Moq;
using CHeaderGenerator.Parser.C;

namespace CHeaderGeneratorTests.Parser.C
{
    [TestClass]
    public class CParserTest
    {
        private static Token<CTokenType> GenerateToken(CTokenType type, string value = "", int line = 0)
        {
            var token = CToken.Create(new Token<TokenType>
            {
                LineNumber = line,
                PositionInLine = 0,
                Type = TokenType.INVALID,
                Value = value
            });

            token.Type = type;

            return token;
        }

        private static Mock<ICLexer> SetupMock(Stack<Token<CTokenType>> tokens)
        {
            var m = new Mock<ICLexer>();
            m.SetupGet(l => l.HasMoreTokens).Returns(() => tokens.Count > 0);
            m.Setup(l => l.GetNextToken(It.IsAny<bool>())).Returns(() =>
            {
                if (tokens.Count > 0)
                    return tokens.Pop();
                return null;
            });
            m.Setup(l => l.SkipToNextLine());
            m.Setup(l => l.PushToken(It.IsAny<Token<CTokenType>>())).Callback((Token<CTokenType> t) => tokens.Push(t));

            return m;
        }

        [TestMethod]
        public void TestParserConstructorThrowsIfNullParameter()
        {
            Assert.That(() => new CParser(null), Throws.TypeOf(typeof(ArgumentNullException)));
        }

        [TestMethod]
        public void TestParserThrowsExceptionIfLexerThrowsException()
        {
            var m = new Mock<ICLexer>();
            m.SetupGet(l => l.HasMoreTokens).Returns(true);
            m.Setup(l => l.GetNextToken(It.IsAny<bool>())).Throws(new Exception());

            var p = new CParser(m.Object);

            Assert.That(() => p.PerformParse(), Throws.TypeOf(typeof(ParserException)));
        }

        [TestMethod]
        public void TestParsingEmptyLexer()
        {
            var m = SetupMock(new Stack<Token<CTokenType>>());

            var p = new CParser(m.Object);
            var sf = p.PerformParse();

            Assert.That(sf, Is.Not.Null);
        }

        [TestMethod]
        public void TestParsingInvalidFirstToken()
        {
            Stack<Token<CTokenType>> tokens = new Stack<Token<CTokenType>>();

            var m = SetupMock(tokens);

            tokens.Push(GenerateToken(CTokenType.TERMINATOR));

            var p = new CParser(m.Object);

            Assert.That(() => p.PerformParse(), Throws.TypeOf(typeof(ParserException)));
        }

        [TestMethod]
        public void TestParsingInvalidPPDirective()
        {
            Stack<Token<CTokenType>> tokens = new Stack<Token<CTokenType>>();

            var m = SetupMock(tokens);

            tokens.Push(GenerateToken(CTokenType.PP_SYMBOL));

            var p = new CParser(m.Object);

            Assert.That(() => p.PerformParse(), Throws.TypeOf(typeof(ParserException)));
        }

        [TestMethod]
        public void TestParsingUnknownPPDirectiveSymbol()
        {
            Stack<Token<CTokenType>> tokens = new Stack<Token<CTokenType>>();

            var m = SetupMock(tokens);

            tokens.Push(GenerateToken(CTokenType.SYMBOL));
            tokens.Push(GenerateToken(CTokenType.PP_SYMBOL));

            var p = new CParser(m.Object);
            var sf = p.PerformParse();

            Assert.That(sf, Is.Not.Null);
            m.Verify(l => l.SkipToNextLine());
        }

        [TestMethod]
        public void TestParsingInvalidPPDirectiveVerb()
        {
            Stack<Token<CTokenType>> tokens = new Stack<Token<CTokenType>>();

            var m = SetupMock(tokens);

            tokens.Push(GenerateToken(CTokenType.KEYWORD));
            tokens.Push(GenerateToken(CTokenType.PP_SYMBOL));

            var p = new CParser(m.Object);
            Assert.That(() => p.PerformParse(), Throws.TypeOf(typeof(ParserException)));
        }

        [TestMethod]
        public void TestParsingUnterminatedInclude()
        {
            Stack<Token<CTokenType>> tokens = new Stack<Token<CTokenType>>();

            var m = SetupMock(tokens);

            tokens.Push(GenerateToken(CTokenType.PP_DIRECTIVE, "include"));
            tokens.Push(GenerateToken(CTokenType.PP_SYMBOL));

            var p = new CParser(m.Object);
            Assert.That(() => p.PerformParse(), Throws.TypeOf(typeof(ParserException)));
        }

        [TestMethod]
        public void TestParsingInvalidInclude()
        {
            Stack<Token<CTokenType>> tokens = new Stack<Token<CTokenType>>();

            var m = SetupMock(tokens);

            tokens.Push(GenerateToken(CTokenType.INVALID));
            tokens.Push(GenerateToken(CTokenType.PP_DIRECTIVE, "include"));
            tokens.Push(GenerateToken(CTokenType.PP_SYMBOL));

            var p = new CParser(m.Object);
            Assert.That(() => p.PerformParse(), Throws.TypeOf(typeof(ParserException)));
        }

        [TestMethod]
        public void TestParsingInvalidInclude2()
        {
            Stack<Token<CTokenType>> tokens = new Stack<Token<CTokenType>>();

            var m = SetupMock(tokens);

            tokens.Push(GenerateToken(CTokenType.PUNCTUATOR, "."));
            tokens.Push(GenerateToken(CTokenType.PP_DIRECTIVE, "include"));
            tokens.Push(GenerateToken(CTokenType.PP_SYMBOL));

            var p = new CParser(m.Object);
            Assert.That(() => p.PerformParse(), Throws.TypeOf(typeof(ParserException)));
        }

        [TestMethod]
        public void TestParsingIncludeStringLiteral()
        {
            Stack<Token<CTokenType>> tokens = new Stack<Token<CTokenType>>();

            var m = SetupMock(tokens);

            tokens.Push(GenerateToken(CTokenType.STRING_LITERAL, "\"test.h\""));
            tokens.Push(GenerateToken(CTokenType.PP_DIRECTIVE, "include"));
            tokens.Push(GenerateToken(CTokenType.PP_SYMBOL));

            var p = new CParser(m.Object);
            var sf = p.PerformParse();

            Assert.That(sf, Is.Not.Null);
            Assert.That(sf.IncludeList.Count, Is.EqualTo(1));
            Assert.That(sf.IncludeList.ElementAt(0).File, Is.EqualTo("test.h"));
            Assert.That(sf.IncludeList.ElementAt(0).IsStandard, Is.False);
        }

        [TestMethod]
        public void TestParsingIncludeStandard()
        {
            Stack<Token<CTokenType>> tokens = new Stack<Token<CTokenType>>();

            var m = SetupMock(tokens);

            tokens.Push(GenerateToken(CTokenType.PUNCTUATOR, ">"));
            tokens.Push(GenerateToken(CTokenType.SYMBOL, "h"));
            tokens.Push(GenerateToken(CTokenType.PUNCTUATOR, "."));
            tokens.Push(GenerateToken(CTokenType.SYMBOL, "test"));
            tokens.Push(GenerateToken(CTokenType.PUNCTUATOR, "<"));
            tokens.Push(GenerateToken(CTokenType.PP_DIRECTIVE, "include"));
            tokens.Push(GenerateToken(CTokenType.PP_SYMBOL));

            var p = new CParser(m.Object);
            var sf = p.PerformParse();

            Assert.That(sf, Is.Not.Null);
            Assert.That(sf.IncludeList.Count, Is.EqualTo(1));
            Assert.That(sf.IncludeList.ElementAt(0).File, Is.EqualTo("test.h"));
            Assert.That(sf.IncludeList.ElementAt(0).IsStandard, Is.True);
        }

        [TestMethod]
        public void TestParsingUnterminatedStandardInclude()
        {
            Stack<Token<CTokenType>> tokens = new Stack<Token<CTokenType>>();

            var m = SetupMock(tokens);

            tokens.Push(GenerateToken(CTokenType.SYMBOL, "h"));
            tokens.Push(GenerateToken(CTokenType.PUNCTUATOR, "."));
            tokens.Push(GenerateToken(CTokenType.SYMBOL, "test"));
            tokens.Push(GenerateToken(CTokenType.PUNCTUATOR, "<"));
            tokens.Push(GenerateToken(CTokenType.PP_DIRECTIVE, "include"));
            tokens.Push(GenerateToken(CTokenType.PP_SYMBOL));

            var p = new CParser(m.Object);
            Assert.That(() => p.PerformParse(), Throws.TypeOf(typeof(ParserException)));
        }

        [TestMethod]
        public void TestParsingUnknownPPDirective()
        {
            Stack<Token<CTokenType>> tokens = new Stack<Token<CTokenType>>();

            var m = SetupMock(tokens);

            tokens.Push(GenerateToken(CTokenType.PP_DIRECTIVE, "pragma"));
            tokens.Push(GenerateToken(CTokenType.PP_SYMBOL));

            var p = new CParser(m.Object);
            var sf = p.PerformParse();

            Assert.That(sf, Is.Not.Null);
            m.Verify(l => l.SkipToNextLine());
        }

        [TestMethod]
        public void TestParsingUnterminatedIfDirective()
        {
            Stack<Token<CTokenType>> tokens = new Stack<Token<CTokenType>>();

            var m = SetupMock(tokens);

            tokens.Push(GenerateToken(CTokenType.PP_DIRECTIVE, "if"));
            tokens.Push(GenerateToken(CTokenType.PP_SYMBOL));

            var p = new CParser(m.Object);
            Assert.That(() => p.PerformParse(), Throws.TypeOf(typeof(ParserException)));
        }

        [TestMethod]
        public void TestParsingValidIfCondition()
        {
            Stack<Token<CTokenType>> tokens = new Stack<Token<CTokenType>>();

            var m = SetupMock(tokens);

            tokens.Push(GenerateToken(CTokenType.NUMERIC_LITERAL, "1"));
            tokens.Push(GenerateToken(CTokenType.PP_DIRECTIVE, "if"));
            tokens.Push(GenerateToken(CTokenType.PP_SYMBOL));

            var p = new CParser(m.Object);
            var sf = p.PerformParse();
            Assert.That(sf, Is.Not.Null);
            var str = sf.PopIfCond();
            Assert.That(str, Is.Not.Null);
            Assert.That(str, Is.EqualTo("1"));
        }

        [TestMethod]
        public void TestParsingEndifWithNoStartingIf()
        {
            Stack<Token<CTokenType>> tokens = new Stack<Token<CTokenType>>();

            var m = SetupMock(tokens);

            tokens.Push(GenerateToken(CTokenType.PP_DIRECTIVE, "endif"));
            tokens.Push(GenerateToken(CTokenType.PP_SYMBOL));

            var p = new CParser(m.Object);

            Assert.That(() => p.PerformParse(), Throws.TypeOf(typeof(ParserException)));
        }

        [TestMethod]
        public void TestParsingValidEndif()
        {
            Stack<Token<CTokenType>> tokens = new Stack<Token<CTokenType>>();

            var m = SetupMock(tokens);

            tokens.Push(GenerateToken(CTokenType.PP_DIRECTIVE, "endif"));
            tokens.Push(GenerateToken(CTokenType.PP_SYMBOL, "", 1));
            tokens.Push(GenerateToken(CTokenType.NUMERIC_LITERAL, "1"));
            tokens.Push(GenerateToken(CTokenType.PP_DIRECTIVE, "if"));
            tokens.Push(GenerateToken(CTokenType.PP_SYMBOL));

            var p = new CParser(m.Object);
            var sf = p.PerformParse();
            Assert.That(() => sf.PopIfCond(), Throws.TypeOf(typeof(InvalidOperationException)));
        }

        [TestMethod]
        public void TestParsingElifWithNoStartingIf()
        {
            Stack<Token<CTokenType>> tokens = new Stack<Token<CTokenType>>();

            var m = SetupMock(tokens);

            tokens.Push(GenerateToken(CTokenType.PP_DIRECTIVE, "elif"));
            tokens.Push(GenerateToken(CTokenType.PP_SYMBOL));

            var p = new CParser(m.Object);

            Assert.That(() => p.PerformParse(), Throws.TypeOf(typeof(ParserException)));
        }

        [TestMethod]
        public void TestParsingElseWithNoStartingIf()
        {
            Stack<Token<CTokenType>> tokens = new Stack<Token<CTokenType>>();

            var m = SetupMock(tokens);

            tokens.Push(GenerateToken(CTokenType.PP_DIRECTIVE, "else"));
            tokens.Push(GenerateToken(CTokenType.PP_SYMBOL));

            var p = new CParser(m.Object);

            Assert.That(() => p.PerformParse(), Throws.TypeOf(typeof(ParserException)));
        }

        [TestMethod]
        public void TestParsingValidElifCondition()
        {
            Stack<Token<CTokenType>> tokens = new Stack<Token<CTokenType>>();

            var m = SetupMock(tokens);

            tokens.Push(GenerateToken(CTokenType.NUMERIC_LITERAL, "1"));
            tokens.Push(GenerateToken(CTokenType.PP_DIRECTIVE, "elif"));
            tokens.Push(GenerateToken(CTokenType.PP_SYMBOL, "", 1));
            tokens.Push(GenerateToken(CTokenType.NUMERIC_LITERAL, "1"));
            tokens.Push(GenerateToken(CTokenType.PP_DIRECTIVE, "if"));
            tokens.Push(GenerateToken(CTokenType.PP_SYMBOL));

            var p = new CParser(m.Object);
            var sf = p.PerformParse();
            Assert.That(sf, Is.Not.Null);
            var str = sf.PopIfCond();
            Assert.That(str, Is.Not.Null);
            Assert.That(str, Is.EqualTo("!(1) && (1)"));
        }

        [TestMethod]
        public void TestParsingValidElseCondition()
        {
            Stack<Token<CTokenType>> tokens = new Stack<Token<CTokenType>>();

            var m = SetupMock(tokens);

            tokens.Push(GenerateToken(CTokenType.PP_DIRECTIVE, "else"));
            tokens.Push(GenerateToken(CTokenType.PP_SYMBOL, "", 1));
            tokens.Push(GenerateToken(CTokenType.NUMERIC_LITERAL, "1"));
            tokens.Push(GenerateToken(CTokenType.PP_DIRECTIVE, "if"));
            tokens.Push(GenerateToken(CTokenType.PP_SYMBOL));

            var p = new CParser(m.Object);
            var sf = p.PerformParse();
            Assert.That(sf, Is.Not.Null);
            var str = sf.PopIfCond();
            Assert.That(str, Is.Not.Null);
            Assert.That(str, Is.EqualTo("!(1)"));
        }

        [TestMethod]
        public void TestParsingValidIfdefCondition()
        {
            Stack<Token<CTokenType>> tokens = new Stack<Token<CTokenType>>();

            var m = SetupMock(tokens);

            tokens.Push(GenerateToken(CTokenType.NUMERIC_LITERAL, "TEST"));
            tokens.Push(GenerateToken(CTokenType.PP_DIRECTIVE, "ifdef"));
            tokens.Push(GenerateToken(CTokenType.PP_SYMBOL));

            var p = new CParser(m.Object);
            var sf = p.PerformParse();
            Assert.That(sf, Is.Not.Null);
            var str = sf.PopIfCond();
            Assert.That(str, Is.Not.Null);
            Assert.That(str, Is.EqualTo("defined(TEST)"));
        }

        [TestMethod]
        public void TestParsingValidIfndefCondition()
        {
            Stack<Token<CTokenType>> tokens = new Stack<Token<CTokenType>>();

            var m = SetupMock(tokens);

            tokens.Push(GenerateToken(CTokenType.NUMERIC_LITERAL, "TEST"));
            tokens.Push(GenerateToken(CTokenType.PP_DIRECTIVE, "ifndef"));
            tokens.Push(GenerateToken(CTokenType.PP_SYMBOL));

            var p = new CParser(m.Object);
            var sf = p.PerformParse();
            Assert.That(sf, Is.Not.Null);
            var str = sf.PopIfCond();
            Assert.That(str, Is.Not.Null);
            Assert.That(str, Is.EqualTo("!defined(TEST)"));
        }

        [TestMethod]
        public void TestParsingUnterminatedPPDefinition()
        {
            Stack<Token<CTokenType>> tokens = new Stack<Token<CTokenType>>();

            var m = SetupMock(tokens);

            tokens.Push(GenerateToken(CTokenType.PP_DIRECTIVE, "define"));
            tokens.Push(GenerateToken(CTokenType.PP_SYMBOL));

            var p = new CParser(m.Object);
            Assert.That(() => p.PerformParse(), Throws.TypeOf(typeof(ParserException)));
        }

        [TestMethod]
        public void TestParsingInvalidPPDefinition()
        {
            Stack<Token<CTokenType>> tokens = new Stack<Token<CTokenType>>();

            var m = SetupMock(tokens);

            tokens.Push(GenerateToken(CTokenType.INVALID));
            tokens.Push(GenerateToken(CTokenType.PP_DIRECTIVE, "define"));
            tokens.Push(GenerateToken(CTokenType.PP_SYMBOL));

            var p = new CParser(m.Object);
            Assert.That(() => p.PerformParse(), Throws.TypeOf(typeof(ParserException)));
        }

        [TestMethod]
        public void TestParsingEmptyPPDefinition()
        {
            Stack<Token<CTokenType>> tokens = new Stack<Token<CTokenType>>();

            var m = SetupMock(tokens);

            tokens.Push(GenerateToken(CTokenType.SYMBOL, "CTokenType"));
            tokens.Push(GenerateToken(CTokenType.PP_DIRECTIVE, "define"));
            tokens.Push(GenerateToken(CTokenType.PP_SYMBOL));

            var p = new CParser(m.Object);

            var c = p.PerformParse();

            Assert.That(c, Is.Not.Null);
            Assert.That(c.PreProcessorDefinitions, Is.Not.Empty);

            var pp = c.PreProcessorDefinitions.First();
            Assert.That(pp, Is.Not.Null);
            Assert.That(pp.Identifier, Is.EqualTo("CTokenType"));
            Assert.That(pp.Arguments, Is.Null);
            Assert.That(pp.Replacement, Is.Null);
        }

        [TestMethod]
        public void TestParsingBasicPPDefinition()
        {
            Stack<Token<CTokenType>> tokens = new Stack<Token<CTokenType>>();

            var m = SetupMock(tokens);

            tokens.Push(GenerateToken(CTokenType.SYMBOL, "FDSA"));
            tokens.Push(GenerateToken(CTokenType.SYMBOL, "CTokenType"));
            tokens.Push(GenerateToken(CTokenType.PP_DIRECTIVE, "define"));
            tokens.Push(GenerateToken(CTokenType.PP_SYMBOL));

            var p = new CParser(m.Object);

            var c = p.PerformParse();

            Assert.That(c, Is.Not.Null);
            Assert.That(c.PreProcessorDefinitions, Is.Not.Empty);

            var pp = c.PreProcessorDefinitions.First();
            Assert.That(pp, Is.Not.Null);
            Assert.That(pp.Identifier, Is.EqualTo("CTokenType"));
            Assert.That(pp.Arguments, Is.Null);
            Assert.That(pp.Replacement, Is.EqualTo("FDSA"));
        }

        [TestMethod]
        public void TestParsingPPFunctionDefinitionWithNoParameters()
        {
            Stack<Token<CTokenType>> tokens = new Stack<Token<CTokenType>>();

            var m = SetupMock(tokens);

            tokens.Push(GenerateToken(CTokenType.SYMBOL, "FDSA"));
            tokens.Push(GenerateToken(CTokenType.PUNCTUATOR, ")"));
            tokens.Push(GenerateToken(CTokenType.PUNCTUATOR, "("));
            tokens.Push(GenerateToken(CTokenType.SYMBOL, "CTokenType"));
            tokens.Push(GenerateToken(CTokenType.PP_DIRECTIVE, "define"));
            tokens.Push(GenerateToken(CTokenType.PP_SYMBOL));

            var p = new CParser(m.Object);

            var c = p.PerformParse();

            Assert.That(c, Is.Not.Null);
            Assert.That(c.PreProcessorDefinitions, Is.Not.Empty);

            var pp = c.PreProcessorDefinitions.First();
            Assert.That(pp, Is.Not.Null);
            Assert.That(pp.Identifier, Is.EqualTo("CTokenType"));
            Assert.That(pp.Arguments, Is.Not.Null);
            Assert.That(pp.Arguments, Is.Empty);
            Assert.That(pp.Replacement, Is.EqualTo("FDSA"));
        }

        [TestMethod]
        public void TestParsingPPFunctionDefinitionWithParameters()
        {
            Stack<Token<CTokenType>> tokens = new Stack<Token<CTokenType>>();

            var m = SetupMock(tokens);

            tokens.Push(GenerateToken(CTokenType.SYMBOL, "FDSA"));
            tokens.Push(GenerateToken(CTokenType.PUNCTUATOR, ")"));
            tokens.Push(GenerateToken(CTokenType.SYMBOL, "b"));
            tokens.Push(GenerateToken(CTokenType.PUNCTUATOR, ","));
            tokens.Push(GenerateToken(CTokenType.SYMBOL, "a"));
            tokens.Push(GenerateToken(CTokenType.PUNCTUATOR, "("));
            tokens.Push(GenerateToken(CTokenType.SYMBOL, "CTokenType"));
            tokens.Push(GenerateToken(CTokenType.PP_DIRECTIVE, "define"));
            tokens.Push(GenerateToken(CTokenType.PP_SYMBOL));

            var p = new CParser(m.Object);

            var c = p.PerformParse();

            Assert.That(c, Is.Not.Null);
            Assert.That(c.PreProcessorDefinitions, Is.Not.Empty);

            var pp = c.PreProcessorDefinitions.First();
            Assert.That(pp, Is.Not.Null);
            Assert.That(pp.Identifier, Is.EqualTo("CTokenType"));
            
            Assert.That(pp.Arguments, Is.Not.Null);
            Assert.That(pp.Arguments, Is.Not.Empty);
            var a = pp.Arguments.First();
            Assert.That(a, Is.Not.Null);
            Assert.That(a, Is.EqualTo("a"));
            var b = pp.Arguments.ElementAt(1);
            Assert.That(b, Is.Not.Null);
            Assert.That(b, Is.EqualTo("b"));

            Assert.That(pp.Replacement, Is.EqualTo("FDSA"));
        }

        [TestMethod]
        public void TestParsingInvalidPPFunctionDefinitionWithParameters()
        {
            Stack<Token<CTokenType>> tokens = new Stack<Token<CTokenType>>();

            var m = SetupMock(tokens);

            tokens.Push(GenerateToken(CTokenType.SYMBOL, "FDSA"));
            tokens.Push(GenerateToken(CTokenType.PUNCTUATOR, ")"));
            tokens.Push(GenerateToken(CTokenType.PUNCTUATOR, ","));
            tokens.Push(GenerateToken(CTokenType.SYMBOL, "a"));
            tokens.Push(GenerateToken(CTokenType.PUNCTUATOR, "("));
            tokens.Push(GenerateToken(CTokenType.SYMBOL, "CTokenType"));
            tokens.Push(GenerateToken(CTokenType.PP_DIRECTIVE, "define"));
            tokens.Push(GenerateToken(CTokenType.PP_SYMBOL));

            var p = new CParser(m.Object);

            Assert.That(() => p.PerformParse(), Throws.TypeOf(typeof(ParserException)));
        }

        [TestMethod]
        public void TestParsingInvalidPPFunctionDefinitionWithParameters2()
        {
            Stack<Token<CTokenType>> tokens = new Stack<Token<CTokenType>>();

            var m = SetupMock(tokens);

            tokens.Push(GenerateToken(CTokenType.PUNCTUATOR, ","));
            tokens.Push(GenerateToken(CTokenType.SYMBOL, "a"));
            tokens.Push(GenerateToken(CTokenType.PUNCTUATOR, "("));
            tokens.Push(GenerateToken(CTokenType.SYMBOL, "CTokenType"));
            tokens.Push(GenerateToken(CTokenType.PP_DIRECTIVE, "define"));
            tokens.Push(GenerateToken(CTokenType.PP_SYMBOL));

            var p = new CParser(m.Object);

            Assert.That(() => p.PerformParse(), Throws.TypeOf(typeof(ParserException)));
        }

        [TestMethod]
        public void TestParsingInvalidPPFunctionDefinitionWithParameters3()
        {
            Stack<Token<CTokenType>> tokens = new Stack<Token<CTokenType>>();

            var m = SetupMock(tokens);

            tokens.Push(GenerateToken(CTokenType.SYMBOL, "a"));
            tokens.Push(GenerateToken(CTokenType.PUNCTUATOR, "("));
            tokens.Push(GenerateToken(CTokenType.SYMBOL, "CTokenType"));
            tokens.Push(GenerateToken(CTokenType.PP_DIRECTIVE, "define"));
            tokens.Push(GenerateToken(CTokenType.PP_SYMBOL));

            var p = new CParser(m.Object);

            Assert.That(() => p.PerformParse(), Throws.TypeOf(typeof(ParserException)));
        }

        [TestMethod]
        public void TestParsingInvalidPPFunctionDefinitionWithParameters4()
        {
            Stack<Token<CTokenType>> tokens = new Stack<Token<CTokenType>>();

            var m = SetupMock(tokens);

            tokens.Push(GenerateToken(CTokenType.SYMBOL, "b"));
            tokens.Push(GenerateToken(CTokenType.SYMBOL, "a"));
            tokens.Push(GenerateToken(CTokenType.PUNCTUATOR, "("));
            tokens.Push(GenerateToken(CTokenType.SYMBOL, "CTokenType"));
            tokens.Push(GenerateToken(CTokenType.PP_DIRECTIVE, "define"));
            tokens.Push(GenerateToken(CTokenType.PP_SYMBOL));

            var p = new CParser(m.Object);

            Assert.That(() => p.PerformParse(), Throws.TypeOf(typeof(ParserException)));
        }

        [TestMethod]
        public void TestParsingInvalidPPFunctionDefinitionWithParameters5()
        {
            Stack<Token<CTokenType>> tokens = new Stack<Token<CTokenType>>();

            var m = SetupMock(tokens);

            tokens.Push(GenerateToken(CTokenType.PUNCTUATOR, "("));
            tokens.Push(GenerateToken(CTokenType.SYMBOL, "CTokenType"));
            tokens.Push(GenerateToken(CTokenType.PP_DIRECTIVE, "define"));
            tokens.Push(GenerateToken(CTokenType.PP_SYMBOL));

            var p = new CParser(m.Object);

            Assert.That(() => p.PerformParse(), Throws.TypeOf(typeof(ParserException)));
        }
    }
}
