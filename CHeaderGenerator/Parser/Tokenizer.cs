using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CHeaderGenerator.Parser
{
    /// <summary>
    /// Reads characters from the stream and forms them into Tokens to be 
    /// used by a lexer or parser.
    /// </summary>
    public class Tokenizer : ITokenizer<TokenType>
    {
        #region Private Data Members

        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly IScanner scanner;

        #endregion

        /// <summary>
        /// Creates an object to read characters and parse them into tokens.
        /// </summary>
        /// <param name="scanner">The scanner to read characters from.</param>
        public Tokenizer(IScanner scanner)
        {
            if (scanner == null)
                throw new ArgumentNullException("scanner");

            this.scanner = scanner;

            this.Punctuators = null;
            this.StringLiteralPunctuator = null;
            this.CharLiteralPunctuator = null;
            this.CommentDelimiters = null;
        }

        public bool HasMoreTokens
        {
            get { return this.scanner.HasMoreCharacters; }
        }

        public string Punctuators { get; set; }
        public char? StringLiteralPunctuator { get; set; }
        public char? CharLiteralPunctuator { get; set; }
        public IEnumerable<CommentDelimiters> CommentDelimiters { get; set; }
        public char? StringEscapeCharacter { get; set; }

        /// <summary>
        /// Get the next token.
        /// </summary>
        /// <returns></returns>
        public Token<TokenType> GetNextToken(bool ignoreWhitespace = true)
        {
            int lineNumber = 0;
            int posInLine = 0;
            char? nextChar = null;

            this.GetFirstCharacterForNextToken(ref lineNumber, ref posInLine, ref nextChar, ignoreWhitespace);
            if (nextChar == null)
                return null;

            this.IgnoreComments(ignoreWhitespace, ref lineNumber, ref posInLine, ref nextChar);
            if (nextChar == null)
                return null;

            var valueBuilder = new StringBuilder();
            var token = new Token<TokenType>
            {
                LineNumber = lineNumber,
                PositionInLine = posInLine
            };
            valueBuilder.Append(nextChar.Value);

            char? peekValue = scanner.Peek();

            if (IsNumericLiteral(nextChar, peekValue))
            {
                this.ParseNumericLiteral(lineNumber, posInLine, nextChar, token, valueBuilder);
            }
            else if (this.IsStringLiteralPunctuator(nextChar.Value))
            {
                this.ParseStringLiteral(nextChar, valueBuilder, token);
            }
            else if (this.IsCharLiteralPunctuator(nextChar.Value))
            {
                this.ParseCharLiteral(nextChar, valueBuilder, token);
            }
            else if (this.IsPunctuator(nextChar.Value))
            {
                token.Type = TokenType.PUNCTUATOR;
            }
            else if (IsValidSymbolCharacter(nextChar.Value))
            {
                this.ParseSymbol(ref lineNumber, ref posInLine, ref nextChar, valueBuilder, token, ref peekValue);
            }
            else if (!ignoreWhitespace && IsWhiteSpace(nextChar.Value))
            {
                this.ParseWhitespace(ref lineNumber, ref posInLine, ref nextChar, valueBuilder, token, ref peekValue);
            }
            else
            {
                log.Trace("Unexpected character ({0}) encountered at position {1} on line {2}", nextChar.Value,
                    posInLine, lineNumber);
                throw new UnexpectedCharEncounteredException(String.Format("Unexpected character {0} at position {1} on line {2}",
                    nextChar.Value, posInLine, lineNumber));
            }

            token.Value = valueBuilder.ToString();

            log.Trace("Successfully parsed token of type {0} with value {1} at position {2} on line {3}",
                token.Type, token.Value, token.PositionInLine, token.LineNumber);
            return token;
        }

        /// <summary>
        /// Only certain characters should be allowed to be string literal punctuators. This determines
        /// if the passed character is one of those characters.
        /// </summary>
        /// <param name="character"></param>
        /// <returns></returns>
        private bool IsValidStringLiteralPunctuator(char character)
        {
            return character == '\'' || character == '"';
        }

        private void IgnoreComments(bool ignoreWhitespace, ref int lineNumber, ref int posInLine, ref char? nextChar)
        {
            this.CheckMultiLineComments(ignoreWhitespace, ref lineNumber, ref posInLine, ref nextChar);
        }

        private void CheckMultiLineComments(bool ignoreWhitespace, ref int lineNumber, ref int posInLine, ref char? nextChar)
        {
            CommentDelimiters currentCommentDelimiter;

            while (nextChar != null && this.IsBeginCommentDelimiter(nextChar, ref lineNumber, ref posInLine, out currentCommentDelimiter))
            {
                nextChar = this.scanner.GetNextCharacter(out lineNumber, out posInLine);
                while (nextChar != null)
                {
                    if (this.IsEndCommentDelimiter(nextChar, currentCommentDelimiter, ref lineNumber, ref posInLine))
                    {
                        this.GetFirstCharacterForNextToken(ref lineNumber, ref posInLine, ref nextChar, ignoreWhitespace);
                        break;
                    }
                    else
                    {
                        nextChar = this.scanner.GetNextCharacter(out lineNumber, out posInLine);
                        if (nextChar == null && currentCommentDelimiter.EndCommentDelimiter != EndCommentDelimiter.EndOfLine)
                        {
                            log.Trace("Unterminated comment encountered at position {0} on line {1}", posInLine, lineNumber);
                            throw new UnexpectedCharEncounteredException(String.Format("Unterminated comment encountered at position {0} on line {1}",
                                posInLine, lineNumber));
                        }
                    }
                }
            }
        }

        private void ParseWhitespace(ref int lineNumber, ref int posInLine, ref char? nextChar, StringBuilder valueBuilder, Token<TokenType> token, ref char? peekValue)
        {
            token.Type = TokenType.WHITESPACE;
            while (peekValue != null && IsWhiteSpace(peekValue.Value))
            {
                nextChar = this.scanner.GetNextCharacter(out lineNumber, out posInLine);
                valueBuilder.Append(nextChar);
                if (nextChar.Value == '\n')
                    break;
                peekValue = this.scanner.Peek();
            }
        }

        private void ParseSymbol(ref int lineNumber, ref int posInLine, ref char? nextChar, StringBuilder valueBuilder, Token<TokenType> token, ref char? peekValue)
        {
            token.Type = TokenType.SYMBOL;
            peekValue = this.scanner.Peek();
            while (peekValue != null && IsValidSymbolCharacter(peekValue.Value))
            {
                nextChar = this.scanner.GetNextCharacter(out lineNumber, out posInLine);
                valueBuilder.Append(nextChar);
                peekValue = this.scanner.Peek();
            }

            peekValue = this.scanner.Peek();
            if (peekValue != null)
            {
                if (!this.IsPunctuator(peekValue.Value) && !IsWhiteSpace(peekValue.Value))
                {
                    log.Trace("Invalid symbol encountered at position {0} on line {1}.", posInLine, lineNumber);
                    throw new UnexpectedCharEncounteredException(String.Format("Symbols may only be terminated by punctuator or whitespace: line {0} pos {1}",
                        lineNumber, posInLine));
                }
            }
        }

        private void ParseCharLiteral(char? nextChar, StringBuilder valueBuilder, Token<TokenType> token)
        {
            token.Type = TokenType.CHAR_LITERAL;
            this.ParseLiteral(nextChar, valueBuilder, token, IsCharLiteralPunctuator);
        }

        private void ParseStringLiteral(char? nextChar, StringBuilder valueBuilder, Token<TokenType> token)
        {
            token.Type = TokenType.STRING_LITERAL;
            this.ParseLiteral(nextChar, valueBuilder, token, IsStringLiteralPunctuator);
        }

        private void ParseLiteral(char? nextChar, StringBuilder valueBuilder, Token<TokenType> token,
            Func<char, bool> IsLiteralEnd)
        {
            nextChar = this.scanner.GetNextCharacter();
            bool escaped = false;

            while (nextChar != null && (escaped || !IsLiteralEnd(nextChar.Value)))
            {
                escaped = (this.IsStringEscapeCharacter(nextChar.Value) && !escaped) ? true : false;
                valueBuilder.Append(nextChar.Value);
                nextChar = this.scanner.GetNextCharacter();
            }

            if (nextChar == null)
            {
                log.Trace("Unterminated string literal encountered at position {0} on line {1}", token.PositionInLine, token.LineNumber);
                throw new UnexpectedCharEncounteredException(String.Format("Unterminated string literal encountered at position {0} on line {1}",
                    token.PositionInLine, token.LineNumber));
            }

            valueBuilder.Append(nextChar.Value);
        }

        private bool IsStringEscapeCharacter(char p)
        {
            if (this.StringEscapeCharacter == null)
                return false;
            return (p == this.StringEscapeCharacter.Value);
        }

        /// <summary>
        /// Parses a numeric literal.
        /// </summary>
        /// <param name="lineNumber"></param>
        /// <param name="posInLine"></param>
        /// <param name="nextChar"></param>
        /// <param name="token"></param>
        private void ParseNumericLiteral(int lineNumber, int posInLine, char? nextChar, Token<TokenType> token,
            StringBuilder valueBuilder)
        {
            token.Type = TokenType.NUMERIC_LITERAL;

            bool hex = false;
            char? peekValue;
            if (nextChar.Value == '0')
            {
                peekValue = this.scanner.Peek();
                if (peekValue != null && peekValue.Value == 'x')
                {
                    hex = true;
                    nextChar = this.scanner.GetNextCharacter(out lineNumber, out posInLine);
                    valueBuilder.Append(nextChar);
                }
            }

            peekValue = this.scanner.Peek();
            while (peekValue != null)
            {
                if (peekValue.Value == '.')
                {
                    if (!valueBuilder.ToString().Contains('.'))
                    {
                        nextChar = this.scanner.GetNextCharacter(out lineNumber, out posInLine);
                        valueBuilder.Append(nextChar);
                        peekValue = this.scanner.Peek();
                    }
                    else
                    {
                        log.Trace("Invalid floating-point numeric literal at position {0} on line {1}",
                            token.PositionInLine, token.LineNumber);
                        throw new FormatException(String.Format("Invalid floating-point numeric literal at position {0} on line {1}",
                            token.PositionInLine, token.LineNumber));
                    }
                }
                else if (!hex && char.ToUpper(peekValue.Value) == 'E')
                {
                    if (!valueBuilder.ToString().ToUpperInvariant().Contains('E'))
                    {
                        nextChar = this.scanner.GetNextCharacter(out lineNumber, out posInLine);
                        valueBuilder.Append(nextChar);
                        peekValue = this.scanner.Peek();
                    }
                    else
                    {
                        log.Trace("Invalid scientific notation numeric literal at position {0} on line {1}",
                            token.PositionInLine, token.LineNumber);
                        throw new FormatException(String.Format("Invalid scientific notation numeric literal at position {0} on line {1}",
                            token.PositionInLine, token.LineNumber));
                    }
                }
                else if (IsValidNumericLiteralSuffix(peekValue.Value))
                {
                    nextChar = this.scanner.GetNextCharacter(out lineNumber, out posInLine);
                    valueBuilder.Append(nextChar);
                    peekValue = this.scanner.Peek();
                    if (char.ToLower(nextChar.Value) == 'u' && peekValue != null
                        && char.ToLower(peekValue.Value) == 'l')
                    {
                        nextChar = this.scanner.GetNextCharacter(out lineNumber, out posInLine);
                        valueBuilder.Append(nextChar);
                        peekValue = this.scanner.Peek();
                    }
                }
                else if (IsValidHexadecimalDigit(peekValue.Value))
                {
                    if (!hex && !char.IsDigit(peekValue.Value))
                    {
                        log.Trace("Invalid hexadecimal numeric literal at position {0} on line {1}", token.PositionInLine,
                            token.LineNumber);
                        throw new UnexpectedCharEncounteredException(String.Format("Invalid hexadecimal numeric literal at position {0} on line {1}",
                            token.PositionInLine, token.LineNumber));
                    }
                    else
                    {
                        nextChar = this.scanner.GetNextCharacter(out lineNumber, out posInLine);
                        valueBuilder.Append(nextChar);
                        peekValue = this.scanner.Peek();
                    }
                }

                else
                {
                    if (!IsWhiteSpace(peekValue.Value) && !this.IsPunctuator(peekValue.Value))
                    {
                        log.Trace("Invalid numeric literal encountered at position {0} on line {1}",
                            token.PositionInLine, token.LineNumber);
                        throw new UnexpectedCharEncounteredException(String.Format("Invalid numeric literal encountered at position {0} on line {1}",
                            token.PositionInLine, token.LineNumber));
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Returns true if the nextChar is the beginning of a numeric literal.
        /// </summary>
        /// <param name="nextChar"></param>
        /// <param name="peekValue"></param>
        /// <returns></returns>
        private static bool IsNumericLiteral(char? nextChar, char? peekValue)
        {
            return char.IsDigit(nextChar.Value) || (nextChar.Value == '-'
                            && peekValue.HasValue && char.IsDigit(peekValue.Value))
                            || (nextChar.Value == '.' && peekValue.HasValue && char.IsDigit(peekValue.Value));
        }

        /// <summary>
        /// Returns true if the character is a valid suffix for a numeric literal, false otherwise.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private static bool IsValidNumericLiteralSuffix(char p)
        {
            return "LUFluf".Contains(p);
        }

        /// <summary>
        /// Determines if the current character is an end comment delimiter,
        /// and consumes it if so.
        /// </summary>
        /// <param name="nextChar"></param>
        /// <param name="lineNumber"></param>
        /// <param name="posInLine"></param>
        /// <returns></returns>
        private bool IsEndCommentDelimiter(char? nextChar, CommentDelimiters currentCommentDelimiter, ref int lineNumber, ref int posInLine)
        {
            if (currentCommentDelimiter.EndCommentDelimiter == EndCommentDelimiter.EndOfLine
                    && nextChar.HasValue && nextChar.Value == '\n')
                return true;
            else if (!String.IsNullOrEmpty(currentCommentDelimiter.EndCommentDelimiter)
                && currentCommentDelimiter.EndCommentDelimiter.Delimiter.StartsWith(nextChar.Value.ToString()))
            {
                int count = 0;
                string endComment = "" + nextChar.Value + this.scanner.Peek(++count);
                while (currentCommentDelimiter.EndCommentDelimiter.Delimiter.StartsWith(endComment)
                    && currentCommentDelimiter.EndCommentDelimiter != endComment)
                    endComment = "" + nextChar.Value + this.scanner.Peek(++count);

                if (endComment == currentCommentDelimiter.EndCommentDelimiter)
                {
                    // consume end comment delimiter
                    for (int i = 0; i < count; ++i)
                        this.scanner.GetNextCharacter(out lineNumber, out posInLine);
                }

                return endComment == currentCommentDelimiter.EndCommentDelimiter;
            }
            else
                return false;
        }

        /// <summary>
        /// Determines if the current character is the beginning of a comment,
        /// and consumes the entire comment delimiter if so.
        /// </summary>
        /// <param name="nextChar"></param>
        /// <param name="lineNumber"></param>
        /// <param name="posInLine"></param>
        /// <returns></returns>
        private bool IsBeginCommentDelimiter(char? nextChar, ref int lineNumber, ref int posInLine, out CommentDelimiters currentCommentDelimiter)
        {
            currentCommentDelimiter = null;

            if (this.CommentDelimiters != null)
            {
                foreach (var commentDelimiter in this.CommentDelimiters)
                {
                    if (!string.IsNullOrEmpty(commentDelimiter.BeginCommentDelimiter))
                    {
                        int count = 0;
                        string peekValue = this.scanner.Peek(++count);
                        string beginComment = nextChar.Value.ToString() + peekValue;
                        while (commentDelimiter.BeginCommentDelimiter.Delimiter.StartsWith(beginComment)
                            && commentDelimiter.BeginCommentDelimiter != beginComment
                            && !String.IsNullOrEmpty(peekValue) && peekValue.Length == count)
                        {
                            peekValue = this.scanner.Peek(++count);
                            beginComment = nextChar.Value.ToString() + peekValue;
                        }

                        if (beginComment == commentDelimiter.BeginCommentDelimiter)
                        {
                            // consume begin comment delimiter
                            for (int i = 0; i < count; ++i)
                                this.scanner.GetNextCharacter(out lineNumber, out posInLine);

                            currentCommentDelimiter = commentDelimiter;

                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the first character for the next token.
        /// </summary>
        /// <param name="lineNumber"></param>
        /// <param name="posInLine"></param>
        /// <param name="nextChar"></param>
        private void GetFirstCharacterForNextToken(ref int lineNumber, ref int posInLine, ref char? nextChar, bool ignoreWhitespace)
        {
            nextChar = this.scanner.GetNextCharacter(out lineNumber, out posInLine);
            while (nextChar != null && ignoreWhitespace && IsWhiteSpace(nextChar.Value))
                nextChar = this.scanner.GetNextCharacter(out lineNumber, out posInLine);
        }

        /// <summary>
        /// Determines if a character is the string literal punctuator.
        /// </summary>
        /// <param name="character"></param>
        /// <returns></returns>
        private bool IsStringLiteralPunctuator(char character)
        {
            if (this.StringLiteralPunctuator == null)
                return false;

            return (character == this.StringLiteralPunctuator.Value);
        }

        /// <summary>
        /// Determines if a character is the char literal punctuator.
        /// </summary>
        /// <param name="character"></param>
        /// <returns></returns>
        private bool IsCharLiteralPunctuator(char character)
        {
            if (this.CharLiteralPunctuator == null)
                return false;

            return (character == this.CharLiteralPunctuator);
        }

        /// <summary>
        /// Is the character a punctuator?
        /// </summary>
        /// <param name="character"></param>
        /// <returns></returns>
        private bool IsPunctuator(char character)
        {
            if (this.Punctuators == null)
                return false;

            return this.Punctuators.Contains(character);
        }

        /// <summary>
        /// Checks if a characters is valid for a symbol, a letter, number, underscore, or '$' to support VMS.
        /// </summary>
        /// <param name="character"></param>
        /// <returns></returns>
        private static bool IsValidSymbolCharacter(char character)
        {
            return char.IsLetterOrDigit(character) || "$_.".Contains(character);
        }

        /// <summary>
        /// Checks if the character is whitepspace including newline characters.
        /// </summary>
        /// <param name="character"></param>
        /// <returns></returns>
        private static bool IsWhiteSpace(char character)
        {
            return (char.IsWhiteSpace(character) || character == '\n');
        }

        private static bool IsValidHexadecimalDigit(char character)
        {
            return char.IsDigit(character) || "abcdef".Contains(char.ToLower(character));
        }

        /// <summary>
        /// This allows clients to skip all tokens remaining on this line and go to the next. This is useful 
        /// for comments.
        /// </summary>
        public void SkipToNextLine()
        {
            this.scanner.SkipToNextLine();
        }
    }
}
