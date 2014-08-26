using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CHeaderGenerator.Parser
{
    /// <summary>
    /// Class to scan characters from to be tokenized & parsed.
    /// </summary>
    public class Scanner : IScanner
    {
        #region Private Data Members
        
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private int lineNumber;
        private int positionInLine;
        private Stream stream;
        private Queue<char> peekQueue = new Queue<char>();
        bool? streamIsEmpty = null;

        #endregion

        #region Public Constructor Definition

        /// <summary>
        /// Construct a new Scanner object
        /// </summary>
        /// <param name="stream">The stream to scan characters from.</param>
        public Scanner(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            this.lineNumber = 1;
            this.positionInLine = 0;
            this.stream = stream;
        }

        #endregion

        #region Public Property Definitions

        /// <summary>
        /// Indicates whether the scanner has characters remaining
        /// </summary>
        public bool HasMoreCharacters
        {
            get
            {
                if (this.peekQueue.Count > 0)
                    return true;
                if (this.streamIsEmpty == null)
                {
                    var nextChar = this.stream.ReadByte();
                    if (nextChar < 0)
                        this.streamIsEmpty = true;
                    else
                    {
                        this.peekQueue.Enqueue((char)nextChar);
                        this.streamIsEmpty = false;
                    }
                }

                return !this.streamIsEmpty.Value;
            }
        }

        #endregion

        #region Public Function Definitions

        /// <summary>
        /// Gets next character from the stream
        /// </summary>
        /// <returns>The next character in the stream, or null if end of stream</returns>
        public char? GetNextCharacter()
        {
            int lineNumber, positionInLine;
            return this.GetNextCharacter(out lineNumber, out positionInLine);
        }

        /// <summary>
        /// Gets next character from the stream, and its line number and position in line
        /// </summary>
        /// <param name="lineNumber">The line the character belongs to</param>
        /// <param name="positionInLine">The position in the line the character can be found</param>
        /// <returns>The next character in the stream, or null if end of stream</returns>
        public char? GetNextCharacter(out int lineNumber, out int positionInLine)
        {
            if (!this.HasMoreCharacters)
            {
                lineNumber = 0;
                positionInLine = 0;
                return null;
            }

            this.positionInLine++;
            char charValue;
            if (this.peekQueue.Count > 0)
                charValue = this.peekQueue.Dequeue();
            else
            {
                var nextByte = this.stream.ReadByte();
                if (nextByte < 0)
                {
                    this.streamIsEmpty = true;
                    lineNumber = 0;
                    positionInLine = 0;
                    return null;
                }

                charValue = (char)nextByte;
            }
            positionInLine = this.positionInLine;
            lineNumber = this.lineNumber;

            if (charValue == '\n')
            {
                this.lineNumber++;
                this.positionInLine = 0;
            }

            log.Trace("Scanning next character ({0}) at position {1} on line {2}",
                charValue, this.positionInLine, this.lineNumber);
            return charValue;
        }
        
        /// <summary>
        /// Returns the next character in the stream, without consuming it.
        /// </summary>
        /// <returns>The next character in the underlying stream.</returns>
        public char? Peek()
        {
            if (!this.HasMoreCharacters)
                return null;
            if (this.peekQueue.Count > 0)
                return this.peekQueue.Peek();
            else
            {
                var nextByte = this.stream.ReadByte();
                if (nextByte < 0)
                {
                    this.streamIsEmpty = true;
                    return null;
                }
                this.peekQueue.Enqueue((char)nextByte);
                return (char)nextByte;
            }
        }

        /// <summary>
        /// Returns a string containing the next characters in the stream, with a length equal to numChars.
        /// </summary>
        /// <param name="numChars">The maximum number of characters to return from the stream.</param>
        /// <returns>A string containing the next characters in the underlying stream.</returns>
        public string Peek(int numChars)
        {
            if (!this.HasMoreCharacters)
                return "";

            while (this.peekQueue.Count < numChars)
            {
                int nextChar = this.stream.ReadByte();
                if (nextChar < 0)
                {
                    this.streamIsEmpty = true;
                    break;
                }
                this.peekQueue.Enqueue((char)nextChar);
            }

            var chars = this.peekQueue.Take(numChars).ToArray();
            return new string(chars);
        }

        /// <summary>
        /// Sometimes we want to skip to the end of a line, like when a comment goes to the
        /// new line. This routine accomplishes that.
        /// </summary>
        public void SkipToNextLine()
        {
            char? nextChar = this.GetNextCharacter();
            while (nextChar != null && nextChar != '\n')
                nextChar = this.GetNextCharacter();
        }

        #endregion
    }
}
