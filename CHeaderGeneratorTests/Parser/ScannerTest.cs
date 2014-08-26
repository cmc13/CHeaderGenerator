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
    public class ScannerTest
    {
        [TestMethod]
        public void TestScannerShouldNotBeConstructedWithNullStreamReader()
        {
            Assert.That(() => new Scanner(null), Throws.TypeOf(typeof(ArgumentNullException)));
        }

        [TestMethod]
        public void TestScanningEmptyStream()
        {
            const string data = "";
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
                Scanner scanner = new Scanner(stream);

                stream = null;
                int lineNumber;
                int posInLine;
                char? nextChar = scanner.GetNextCharacter(out lineNumber, out posInLine);
                Assert.That(nextChar, Is.Null);
                Assert.That(lineNumber, Is.EqualTo(0));
                Assert.That(posInLine, Is.EqualTo(0));

            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }

        [TestMethod]
        public void TestScanningStream()
        {
            const string data = "a\nb";
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
                Scanner scanner = new Scanner(stream);

                stream = null;
                int lineNumber;
                int posInLine;

                char? nextChar = scanner.GetNextCharacter(out lineNumber, out posInLine);
                Assert.That(nextChar, Is.EqualTo('a'));
                Assert.That(lineNumber, Is.EqualTo(1));
                Assert.That(posInLine, Is.EqualTo(1));

                nextChar = scanner.GetNextCharacter(out lineNumber, out posInLine);
                Assert.That(nextChar, Is.EqualTo('\n'));
                Assert.That(lineNumber, Is.EqualTo(1));
                Assert.That(posInLine, Is.EqualTo(2));

                nextChar = scanner.GetNextCharacter(out lineNumber, out posInLine);
                Assert.That(nextChar, Is.EqualTo('b'));
                Assert.That(lineNumber, Is.EqualTo(2));
                Assert.That(posInLine, Is.EqualTo(1));

                nextChar = scanner.GetNextCharacter(out lineNumber, out posInLine);
                Assert.That(nextChar, Is.Null);
                Assert.That(lineNumber, Is.EqualTo(0));
                Assert.That(posInLine, Is.EqualTo(0));
            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }

        [TestMethod]
        public void TestSkippingToNextLineOnEmptyStream()
        {
            const string data = "";
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
                Scanner scanner = new Scanner(stream);

                stream = null;
                int lineNumber;
                int posInLine;

                scanner.SkipToNextLine();

                char? nextChar = scanner.GetNextCharacter(out lineNumber, out posInLine);
                Assert.That(nextChar, Is.Null);
                Assert.That(lineNumber, Is.EqualTo(0));
                Assert.That(posInLine, Is.EqualTo(0));

            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }

        [TestMethod]
        public void TestSkippingToNextLine()
        {
            const string data = "a\nb";
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
                Scanner scanner = new Scanner(stream);

                stream = null;
                int lineNumber;
                int posInLine;

                scanner.SkipToNextLine();

                char? nextChar = scanner.GetNextCharacter(out lineNumber, out posInLine);
                Assert.That(nextChar, Is.EqualTo('b'));
                Assert.That(lineNumber, Is.EqualTo(2));
                Assert.That(posInLine, Is.EqualTo(1));

                nextChar = scanner.GetNextCharacter(out lineNumber, out posInLine);
                Assert.That(nextChar, Is.Null);
                Assert.That(lineNumber, Is.EqualTo(0));
                Assert.That(posInLine, Is.EqualTo(0));

            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }

        [TestMethod]
        public void TestPeeking()
        {
            const string data = "a\nb";
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
                Scanner scanner = new Scanner(stream);

                stream = null;
                int lineNumber;
                int posInLine;

                char? nextChar = scanner.GetNextCharacter(out lineNumber, out posInLine);
                Assert.That(nextChar, Is.EqualTo('a'));
                Assert.That(lineNumber, Is.EqualTo(1));
                Assert.That(posInLine, Is.EqualTo(1));

                nextChar = scanner.Peek();
                Assert.That(nextChar, Is.EqualTo('\n'));

                nextChar = scanner.GetNextCharacter(out lineNumber, out posInLine);
                Assert.That(nextChar, Is.EqualTo('\n'));
                Assert.That(lineNumber, Is.EqualTo(1));
                Assert.That(posInLine, Is.EqualTo(2));

                nextChar = scanner.GetNextCharacter(out lineNumber, out posInLine);
                Assert.That(nextChar, Is.EqualTo('b'));
                Assert.That(lineNumber, Is.EqualTo(2));
                Assert.That(posInLine, Is.EqualTo(1));

                nextChar = scanner.GetNextCharacter(out lineNumber, out posInLine);
                Assert.That(nextChar, Is.Null);
                Assert.That(lineNumber, Is.EqualTo(0));
                Assert.That(posInLine, Is.EqualTo(0));

            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }

        [TestMethod]
        public void TestPeekingEmptyStream()
        {
            const string data = "";
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
                Scanner scanner = new Scanner(stream);

                stream = null;
                char? nextChar = scanner.Peek();
                Assert.That(nextChar, Is.Null);

            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }

        [TestMethod]
        public void TestPeekingMultipleEmptyStream()
        {
            const string data = "";
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
                Scanner scanner = new Scanner(stream);

                stream = null;
                string nextChar = scanner.Peek(5);
                Assert.That("", Is.EqualTo(nextChar));

            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }

        [TestMethod]
        public void TestPeekingMultipleChars()
        {
            const string data = "a\nb";
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
                Scanner scanner = new Scanner(stream);

                stream = null;
                int lineNumber;
                int posInLine;

                char? nextChar = scanner.GetNextCharacter(out lineNumber, out posInLine);
                Assert.That(nextChar, Is.EqualTo('a'));
                Assert.That(lineNumber, Is.EqualTo(1));
                Assert.That(posInLine, Is.EqualTo(1));

                string peekTest = scanner.Peek(2);
                Assert.That(peekTest, Is.EqualTo("\nb"));

                nextChar = scanner.GetNextCharacter(out lineNumber, out posInLine);
                Assert.That(nextChar, Is.EqualTo('\n'));
                Assert.That(lineNumber, Is.EqualTo(1));
                Assert.That(posInLine, Is.EqualTo(2));

                nextChar = scanner.GetNextCharacter(out lineNumber, out posInLine);
                Assert.That(nextChar, Is.EqualTo('b'));
                Assert.That(lineNumber, Is.EqualTo(2));
                Assert.That(posInLine, Is.EqualTo(1));

                nextChar = scanner.GetNextCharacter(out lineNumber, out posInLine);
                Assert.That(nextChar, Is.Null);
                Assert.That(lineNumber, Is.EqualTo(0));
                Assert.That(posInLine, Is.EqualTo(0));

            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }

        [TestMethod]
        public void TestMultiplePeeks()
        {
            const string data = "abcd";
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
                Scanner scanner = new Scanner(stream);

                stream = null;
                int lineNumber;
                int posInLine;

                string peekString = scanner.Peek(3);
                Assert.That("abc", Is.EqualTo(peekString));

                peekString = scanner.Peek(2);
                Assert.That("ab", Is.EqualTo(peekString));

                peekString = scanner.Peek(4);
                Assert.That("abcd", Is.EqualTo(peekString));

                char? nextChar = scanner.GetNextCharacter(out lineNumber, out posInLine);
                Assert.That(nextChar, Is.EqualTo('a'));
                Assert.That(lineNumber, Is.EqualTo(1));
                Assert.That(posInLine, Is.EqualTo(1));

                nextChar = scanner.GetNextCharacter(out lineNumber, out posInLine);
                Assert.That(nextChar, Is.EqualTo('b'));
                Assert.That(lineNumber, Is.EqualTo(1));
                Assert.That(posInLine, Is.EqualTo(2));

                nextChar = scanner.GetNextCharacter(out lineNumber, out posInLine);
                Assert.That(nextChar, Is.EqualTo('c'));
                Assert.That(lineNumber, Is.EqualTo(1));
                Assert.That(posInLine, Is.EqualTo(3));

                nextChar = scanner.GetNextCharacter(out lineNumber, out posInLine);
                Assert.That(nextChar, Is.EqualTo('d'));
                Assert.That(lineNumber, Is.EqualTo(1));
                Assert.That(posInLine, Is.EqualTo(4));

            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }

        [TestMethod]
        public void TestPeekingPastEOS()
        {
            const string data = "a\nb";
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
                var scanner = new Scanner(stream);

                stream = null;
                int lineNumber;
                int posInLine;

                char? nextChar = scanner.GetNextCharacter(out lineNumber, out posInLine);
                Assert.That(nextChar, Is.EqualTo('a'));
                Assert.That(lineNumber, Is.EqualTo(1));
                Assert.That(posInLine, Is.EqualTo(1));

                string peekTest = scanner.Peek(10);
                Assert.That(peekTest, Is.EqualTo("\nb"));

                nextChar = scanner.GetNextCharacter(out lineNumber, out posInLine);
                Assert.That(nextChar, Is.EqualTo('\n'));
                Assert.That(lineNumber, Is.EqualTo(1));
                Assert.That(posInLine, Is.EqualTo(2));

                nextChar = scanner.GetNextCharacter(out lineNumber, out posInLine);
                Assert.That(nextChar, Is.EqualTo('b'));
                Assert.That(lineNumber, Is.EqualTo(2));
                Assert.That(posInLine, Is.EqualTo(1));

                nextChar = scanner.GetNextCharacter(out lineNumber, out posInLine);
                Assert.That(nextChar, Is.Null);
                Assert.That(lineNumber, Is.EqualTo(0));
                Assert.That(posInLine, Is.EqualTo(0));

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
                var scanner = new Scanner(stream);

                stream = null;

                Assert.That(scanner.HasMoreCharacters, Is.False);
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
                var scanner = new Scanner(stream);

                stream = null;

                Assert.That(scanner.HasMoreCharacters, Is.True);
            }
            finally
            {
                if (stream != null)
                    stream.Dispose();
            }
        }
    }
}
