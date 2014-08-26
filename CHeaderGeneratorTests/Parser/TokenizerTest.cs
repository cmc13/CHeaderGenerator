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

namespace CHeaderGeneratorTests.Parser
{
    [TestClass]
    public class TokenizerTest
    {
        [TestMethod]
        public void TestCreatingTokenizerWithNullScanner()
        {
            Assert.That(() => new Tokenizer(null), Throws.TypeOf(typeof(ArgumentNullException)));
        }

        [TestMethod]
        public void TestTokenizingStringWithUnexpectedCharacters()
        {
            const string data = "@";
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
                Scanner scanner = new Scanner(stream);
                stream = null;
                Tokenizer tokenizer = new Tokenizer(scanner);

                Assert.That(() => { Token<TokenType> nextToken = tokenizer.GetNextToken(); },
                    Throws.TypeOf(typeof(UnexpectedCharEncounteredException)));
            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }

        [TestMethod]
        public void TestTokenizingEmptyStream()
        {
            const string data = "";
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
                Scanner scanner = new Scanner(stream);

                stream = null;
                Tokenizer tokenizer = new Tokenizer(scanner);

                Token<TokenType> nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken, Is.Null);
            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }

        [TestMethod]
        public void TestTokenizingComment()
        {
            const string data = "/* test */";
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
                Scanner scanner = new Scanner(stream);

                stream = null;
                Tokenizer tokenizer = new Tokenizer(scanner)
                {
                    CommentDelimiters = new[] { new CommentDelimiters("/*", "*/") }
                };

                Token<TokenType> nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken, Is.Null);

            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }

        [TestMethod]
        public void TestTokenizingCommentWithPunctuators()
        {
            const string data = "/* test */";
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
                Scanner scanner = new Scanner(stream);

                stream = null;
                Tokenizer tokenizer = new Tokenizer(scanner)
                {
                    CommentDelimiters = new[] { new CommentDelimiters("/*", "*/") },
                    Punctuators = "/*"
                };

                Token<TokenType> nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken, Is.Null);

            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }

        [TestMethod]
        public void TestFailingTokenizingEndComment()
        {
            const string data = "test */";
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
                Scanner scanner = new Scanner(stream);

                stream = null;
                Tokenizer tokenizer = new Tokenizer(scanner)
                {
                    CommentDelimiters = new[] { new CommentDelimiters("/*", "*/") }
                };

                Token<TokenType> nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("test"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.SYMBOL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(1));

                Assert.That(() => nextToken = tokenizer.GetNextToken(), Throws.TypeOf(typeof(UnexpectedCharEncounteredException)));
            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }

        [TestMethod]
        public void TestFailingTokenizingUnterminatedComment()
        {
            const string data = "/* test";
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
                Scanner scanner = new Scanner(stream);

                stream = null;
                Tokenizer tokenizer = new Tokenizer(scanner)
                {
                    CommentDelimiters = new[] { new CommentDelimiters("/*", "*/") }
                };

                Assert.That(() => { Token<TokenType> nextToken = tokenizer.GetNextToken(); },
                    Throws.TypeOf(typeof(UnexpectedCharEncounteredException)));
            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }

        [TestMethod]
        public void TestTokenizingLongCommentDelimiters()
        {
            const string data = "/*~~ test ~~*/";
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
                Scanner scanner = new Scanner(stream);

                stream = null;
                Tokenizer tokenizer = new Tokenizer(scanner)
                {
                    CommentDelimiters = new[] { new CommentDelimiters("/*~~", "~~*/") }
                };

                Token<TokenType> nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken, Is.Null);

            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }

        [TestMethod]
        public void TestTokenizingEndCommentAsPunctuators()
        {
            const string data = "test */";
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
                Scanner scanner = new Scanner(stream);

                stream = null;
                Tokenizer tokenizer = new Tokenizer(scanner)
                {
                    CommentDelimiters = new[] { new CommentDelimiters("/*", "*/") },
                    Punctuators = "/*"
                };

                Token<TokenType> nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("test"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.SYMBOL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(1));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("*"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.PUNCTUATOR));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(6));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("/"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.PUNCTUATOR));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(7));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken, Is.Null);

            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }

        [TestMethod]
        public void TestTokenizingSingleLineComment()
        {
            const string data = "int // test";
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
                Scanner scanner = new Scanner(stream);

                stream = null;
                Tokenizer tokenizer = new Tokenizer(scanner)
                {
                    CommentDelimiters = new[] { new CommentDelimiters("//", EndCommentDelimiter.EndOfLine) }
                };

                Token<TokenType> nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("int"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.SYMBOL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(1));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken, Is.Null);

            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }

        [TestMethod]
        public void TestTokenizingSingleLineCommentWithStuffOnSubsequentLines()
        {
            const string data = @"// test
int";
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
                Scanner scanner = new Scanner(stream);

                stream = null;
                Tokenizer tokenizer = new Tokenizer(scanner)
                {
                    CommentDelimiters = new[] { new CommentDelimiters("//", EndCommentDelimiter.EndOfLine) }
                };

                Token<TokenType> nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("int"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.SYMBOL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(2));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(1));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken, Is.Null);

            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }

        [TestMethod]
        public void TestTokenizingSingleLineCommentWithStuffBeforeAndAfter()
        {
            const string data = @"a // test
b";
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
                Scanner scanner = new Scanner(stream);

                stream = null;
                Tokenizer tokenizer = new Tokenizer(scanner)
                {
                    CommentDelimiters = new[] { new CommentDelimiters("//", EndCommentDelimiter.EndOfLine) }
                };

                Token<TokenType> nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("a"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.SYMBOL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(1));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("b"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.SYMBOL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(2));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(1));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken, Is.Null);

            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }

        [TestMethod]
        public void TestTokenizingEmptyStreamWithUnignoredWhitespace()
        {
            const string data = "";
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
                Scanner scanner = new Scanner(stream);

                stream = null;
                Tokenizer tokenizer = new Tokenizer(scanner);

                Token<TokenType> nextToken = tokenizer.GetNextToken(false);
                Assert.That(nextToken, Is.Null);
            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }

        [TestMethod]
        public void TestTokenizingWhitespace()
        {
            const string data = "\t \n";
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
                Scanner scanner = new Scanner(stream);

                stream = null;
                Tokenizer tokenizer = new Tokenizer(scanner);

                Token<TokenType> nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken, Is.Null);

            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }

        [TestMethod]
        public void TestTokenizingUnignoredWhitespace()
        {
            const string data = "\t \n";
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
                Scanner scanner = new Scanner(stream);

                stream = null;
                Tokenizer tokenizer = new Tokenizer(scanner);

                Token<TokenType> nextToken = tokenizer.GetNextToken(false);
                Assert.That(nextToken, Is.Not.Null);
                Assert.That(nextToken.Value, Is.EqualTo("\t \n"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.WHITESPACE));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(1));

            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }

        [TestMethod]
        public void TestTokenizingSymbols()
        {
            const string data = "symbol1 symbol2";
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
                Scanner scanner = new Scanner(stream);

                stream = null;
                Tokenizer tokenizer = new Tokenizer(scanner);

                Token<TokenType> nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("symbol1"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.SYMBOL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(1));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("symbol2"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.SYMBOL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(9));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken, Is.Null);

            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }

        [TestMethod]
        public void TestTokenizingSymbolsIgnoringWhitespace()
        {
            const string data = "\t\t\t          symbol1  \t \t \t   \nsymbol2     ";
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
                Scanner scanner = new Scanner(stream);

                stream = null;
                Tokenizer tokenizer = new Tokenizer(scanner);

                Token<TokenType> nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("symbol1"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.SYMBOL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(14));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("symbol2"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.SYMBOL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(2));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(1));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken, Is.Null);

            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }

        [TestMethod]
        public void TestTokenizingSymbolsNotIgnoringWhitespace()
        {
            const string data = "\t\t\t          symbol1  \t \t \t   \nsymbol2     ";
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
                Scanner scanner = new Scanner(stream);

                stream = null;
                Tokenizer tokenizer = new Tokenizer(scanner);

                Token<TokenType> nextToken = tokenizer.GetNextToken(false);
                Assert.That(nextToken.Value, Is.EqualTo("\t\t\t          "));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.WHITESPACE));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(1));

                nextToken = tokenizer.GetNextToken(false);
                Assert.That(nextToken.Value, Is.EqualTo("symbol1"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.SYMBOL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(14));

                nextToken = tokenizer.GetNextToken(false);
                Assert.That(nextToken.Value, Is.EqualTo("  \t \t \t   \n"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.WHITESPACE));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(21));

                nextToken = tokenizer.GetNextToken(false);
                Assert.That(nextToken.Value, Is.EqualTo("symbol2"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.SYMBOL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(2));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(1));

                nextToken = tokenizer.GetNextToken(false);
                Assert.That(nextToken.Value, Is.EqualTo("     "));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.WHITESPACE));
                Assert.That(nextToken.LineNumber, Is.EqualTo(2));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(8));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken, Is.Null);

            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }

        [TestMethod]
        public void TestTokenizingPunctuators()
        {
            const string data = ", ;";
            string punctuators = ",;";

            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
                Scanner scanner = new Scanner(stream);

                stream = null;
                Tokenizer tokenizer = new Tokenizer(scanner)
                {
                    Punctuators = punctuators
                };

                Token<TokenType> nextToken = tokenizer.GetNextToken();
                Assert.AreEqual(",", nextToken.Value);
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.PUNCTUATOR));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(1));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo(";"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.PUNCTUATOR));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(3));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken, Is.Null);

            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }

        [TestMethod]
        public void TestFailingToTokenizeInvalidFloatingPointNumber()
        {
            Assert.That(() => TestTokenizingNumericLiteral("0.0.0"), Throws.TypeOf(typeof(FormatException)));
        }

        [TestMethod]
        public void TestFailingToTokenizeInvalidScientificNotationNumber()
        {
            Assert.That(() => TestTokenizingNumericLiteral("5e7e4"), Throws.TypeOf(typeof(FormatException)));
        }

        [TestMethod]
        public void TestFailingToTokenizeInvalidScientificNotationNumber2()
        {
            Assert.That(() => TestTokenizingNumericLiteral("5ee"), Throws.TypeOf(typeof(FormatException)));
        }

        [TestMethod]
        public void TestFailingToTokenizeInvalidNumericLiteral()
        {
            Assert.That(() => TestTokenizingNumericLiteral("5z"), Throws.TypeOf(typeof(UnexpectedCharEncounteredException)));
        }

        private static Token<TokenType> TestTokenizingNumericLiteral(string data)
        {
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
                Scanner scanner = new Scanner(stream);

                stream = null;
                Tokenizer tokenizer = new Tokenizer(scanner);

                return tokenizer.GetNextToken();
            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }

        /// <summary>
        /// Only going to do simple numeric literals at this time, i.e. no 1.0f, 0x10c, etc..
        /// </summary>
        [TestMethod]
        public void TestTokenizingNumericLiterals()
        {
            const string data = "1 123 0x123 0 0x0 -9 5.0 0.0 5.1 -3.5 0xABC 4.5e8 -4.5e8 0x0L 0xABCL 0x123L"
                + " 0x0UL 0xABCUL 0x123UL 12e4L 0. .0 0x0ee 0xFFFFFFFFULL 1.20f";

            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
                Scanner scanner = new Scanner(stream);

                stream = null;
                Tokenizer tokenizer = new Tokenizer(scanner);

                Token<TokenType> nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("1"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.NUMERIC_LITERAL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(1));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("123"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.NUMERIC_LITERAL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(3));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("0x123"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.NUMERIC_LITERAL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(7));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("0"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.NUMERIC_LITERAL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(13));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("0x0"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.NUMERIC_LITERAL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(15));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("-9"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.NUMERIC_LITERAL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(19));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("5.0"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.NUMERIC_LITERAL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(22));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("0.0"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.NUMERIC_LITERAL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(26));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("5.1"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.NUMERIC_LITERAL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(30));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("-3.5"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.NUMERIC_LITERAL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(34));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("0xABC"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.NUMERIC_LITERAL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(39));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("4.5e8"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.NUMERIC_LITERAL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(45));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("-4.5e8"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.NUMERIC_LITERAL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(51));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("0x0L"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.NUMERIC_LITERAL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(58));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("0xABCL"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.NUMERIC_LITERAL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(63));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("0x123L"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.NUMERIC_LITERAL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(70));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("0x0UL"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.NUMERIC_LITERAL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(77));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("0xABCUL"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.NUMERIC_LITERAL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(83));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("0x123UL"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.NUMERIC_LITERAL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(91));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("12e4L"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.NUMERIC_LITERAL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(99));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("0."));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.NUMERIC_LITERAL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(105));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo(".0"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.NUMERIC_LITERAL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(108));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("0x0ee"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.NUMERIC_LITERAL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(111));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("0xFFFFFFFFULL"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.NUMERIC_LITERAL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(117));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("1.20f"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.NUMERIC_LITERAL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(131));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken, Is.Null);

            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }

        [TestMethod]
        public void TestTokenizingStringLiteral()
        {
            const string data = @"""str1"" ""str2""";
            const char stringLiteralPunctuator = '"';

            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
                Scanner scanner = new Scanner(stream);

                stream = null;
                Tokenizer tokenizer = new Tokenizer(scanner)
                {
                    StringLiteralPunctuator = stringLiteralPunctuator
                };

                Token<TokenType> nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("\"str1\""));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.STRING_LITERAL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(1));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("\"str2\""));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.STRING_LITERAL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(8));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken, Is.Null);

            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }

        [TestMethod]
        public void TestTokenizingStringLiteralWithEscapeCharacter()
        {
            const string data = @"""str\""test"" ""\\""";
            const char stringLiteralPunctuator = '"';

            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
                Scanner scanner = new Scanner(stream);

                stream = null;
                Tokenizer tokenizer = new Tokenizer(scanner)
                {
                    StringLiteralPunctuator = stringLiteralPunctuator,
                    StringEscapeCharacter = '\\'
                };

                Token<TokenType> nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("\"str\\\"test\""));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.STRING_LITERAL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(1));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("\"\\\\\""));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.STRING_LITERAL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(13));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken, Is.Null);

            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }

        [TestMethod]
        public void TestTokenizingUnterminatedStringLiteral()
        {
            const string data = @"""str1";
            const char stringLiteralPunctuator = '"';

            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
                Scanner scanner = new Scanner(stream);

                stream = null;
                Tokenizer tokenizer = new Tokenizer(scanner)
                {
                    StringLiteralPunctuator = stringLiteralPunctuator
                };

                Assert.That(() => { Token<TokenType> nextToken = tokenizer.GetNextToken(); },
                    Throws.TypeOf(typeof(UnexpectedCharEncounteredException)));
            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }

        [TestMethod]
        public void TestTokenizingCharLiteral()
        {
            const string data = @"'C' 'D'";

            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
                Scanner scanner = new Scanner(stream);

                stream = null;
                Tokenizer tokenizer = new Tokenizer(scanner)
                {
                    CharLiteralPunctuator = '\''
                };

                Token<TokenType> nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("'C'"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.CHAR_LITERAL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(1));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("'D'"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.CHAR_LITERAL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(5));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken, Is.Null);

            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }

        [TestMethod]
        public void TestTokenizingCharLiteralWithEscapeCharacter()
        {
            const string data = @"'\''";

            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
                Scanner scanner = new Scanner(stream);

                stream = null;
                Tokenizer tokenizer = new Tokenizer(scanner)
                {
                    CharLiteralPunctuator = '\'',
                    StringEscapeCharacter = '\\'
                };

                Token<TokenType> nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("'\\''"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.CHAR_LITERAL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(1));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken, Is.Null);

            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }

        [TestMethod]
        public void TestTokenizingUnterminatedCharLiteral()
        {
            const string data = @"'C";

            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
                Scanner scanner = new Scanner(stream);

                stream = null;
                Tokenizer tokenizer = new Tokenizer(scanner)
                {
                    CharLiteralPunctuator = '\''
                };

                Assert.That(() => { Token<TokenType> nextToken = tokenizer.GetNextToken(); },
                    Throws.TypeOf(typeof(UnexpectedCharEncounteredException)));
            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }

        [TestMethod]
        public void TestTokenizingTokensTerminatedByPunctuators()
        {
            const string data = "keyword(symbol1, 123, 'str')";
            string punctuators = ",()";
            const char stringLiteralPunctuator = '\'';

            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
                Scanner scanner = new Scanner(stream);

                stream = null;
                Tokenizer tokenizer = new Tokenizer(scanner)
                {
                    Punctuators = punctuators,
                    StringLiteralPunctuator = stringLiteralPunctuator
                };

                Token<TokenType> nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("keyword"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.SYMBOL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(1));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("("));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.PUNCTUATOR));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(8));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("symbol1"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.SYMBOL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(9));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo(","));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.PUNCTUATOR));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(16));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("123"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.NUMERIC_LITERAL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(18));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo(","));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.PUNCTUATOR));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(21));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("'str'"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.STRING_LITERAL));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(23));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo(")"));
                Assert.That(nextToken.Type, Is.EqualTo(TokenType.PUNCTUATOR));
                Assert.That(nextToken.LineNumber, Is.EqualTo(1));
                Assert.That(nextToken.PositionInLine, Is.EqualTo(28));

                nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken, Is.Null);

            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }

        [TestMethod]
        public void TestFailingAtTokenizingNumericLiteralsTerminatedByNonPunctuatorOrWhitespace()
        {
            const string data = "123a";
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
                Scanner scanner = new Scanner(stream);

                stream = null;
                Tokenizer tokenizer = new Tokenizer(scanner);

                Assert.That(() => { Token<TokenType> nextToken = tokenizer.GetNextToken(); },
                    Throws.TypeOf(typeof(UnexpectedCharEncounteredException)));
            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }

        [TestMethod]
        public void TestFailingAtTokenizingSymbolsTerminatedByNonPunctuatorOrWhitespace()
        {
            const string data = "symbol'";
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
                Scanner scanner = new Scanner(stream);

                stream = null;
                Tokenizer tokenizer = new Tokenizer(scanner);

                Assert.That(() => { Token<TokenType> nextToken = tokenizer.GetNextToken(); },
                    Throws.TypeOf(typeof(UnexpectedCharEncounteredException)));
            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }

        [TestMethod]
        public void TestSkippingToNextLineOfTokens()
        {
            const string data = " symbol1 symbol2 \n symbol3 ";
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
                Scanner scanner = new Scanner(stream);

                stream = null;
                Tokenizer tokenizer = new Tokenizer(scanner);

                Token<TokenType> nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("symbol1"));

                tokenizer.SkipToNextLine();
                nextToken = tokenizer.GetNextToken();

                Assert.That(nextToken.Value, Is.EqualTo("symbol3"));

            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }

        [TestMethod]
        public void TestSkippingToNextLineOfTokensWhenTokenTerminatedByNewLine()
        {
            const string data = " symbol1\nsymbol2";
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
                Scanner scanner = new Scanner(stream);

                stream = null;
                Tokenizer tokenizer = new Tokenizer(scanner);

                Token<TokenType> nextToken = tokenizer.GetNextToken();
                Assert.That(nextToken.Value, Is.EqualTo("symbol1"));

                tokenizer.SkipToNextLine();
                nextToken = tokenizer.GetNextToken();

                Assert.That(nextToken.Value, Is.EqualTo("symbol2"));

            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }

        [TestMethod]
        public void TestHasMoreCharactersReturnsFalseWhenStreamEmpty()
        {
            const string data = "";
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
                Scanner scanner = new Scanner(stream);

                stream = null;
                Tokenizer tokenizer = new Tokenizer(scanner);

                Assert.That(tokenizer.HasMoreTokens, Is.False);
            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }

        [TestMethod]
        public void TestHasMoreCharactersReturnsTrueWhenStreamNotEmpty()
        {
            const string data = "a";
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
                Scanner scanner = new Scanner(stream);

                stream = null;
                Tokenizer tokenizer = new Tokenizer(scanner);

                Assert.That(tokenizer.HasMoreTokens, Is.True);
            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }
    }
}
