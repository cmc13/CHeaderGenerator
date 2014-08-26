using NLog;
using System;
using System.Collections.Generic;

namespace CHeaderGenerator.Parser.C
{
    public class CLexer : ICLexer
    {
        #region Private Data Members

        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly ITokenizer<TokenType> localTokenizer;
        private readonly Stack<Token<CTokenType>> pushedTokens = new Stack<Token<CTokenType>>();
        private Token<TokenType> prevToken = null;

        #endregion

        #region Public Constructor Definition

        /// <summary>
        /// Constructs a new lexer for the C language.
        /// </summary>
        /// <param name="tokenizer">An object to return generic tokens.</param>
        public CLexer(ITokenizer<TokenType> tokenizer)
        {
            if (tokenizer == null)
                throw new ArgumentNullException("tokenizer");

            this.localTokenizer = tokenizer;
        }

        #endregion

        #region Public Property Definitions

        /// <summary>
        /// Indicates whether the stream still contains tokens to be parsed.
        /// </summary>
        public bool HasMoreTokens
        {
            get { return this.localTokenizer.HasMoreTokens; }
        }

        #endregion

        #region Public Function Definitions

        /// <summary>
        /// Pushes a token back onto the stack to be returned later.
        /// </summary>
        /// <param name="token"></param>
        public void PushToken(Token<CTokenType> token)
        {
            this.pushedTokens.Push(token);
        }

        /// <summary>
        /// Returns the next token from the stream.
        /// </summary>
        /// <param name="ignoreWhitespace">indicates whether the return whitespace tokens or to ignore them.</param>
        /// <returns>The next token from the stream.</returns>
        public Token<CTokenType> GetNextToken(bool ignoreWhitespace = true)
        {
            Token<TokenType> nextToken = null;

            while (this.pushedTokens.Count > 0)
            {
                var pushedToken = this.pushedTokens.Pop() as CToken;
                if (!ignoreWhitespace || pushedToken.BaseTokenType != TokenType.WHITESPACE)
                    return pushedToken;
            }

            nextToken = this.localTokenizer.GetNextToken(ignoreWhitespace);
            if (nextToken == null)
                return null;

            CToken cToken = CToken.Create(nextToken, this.prevToken);
            this.prevToken = nextToken;

            log.Trace("Next token read at position {0} on line {1}. Type: {2}, Value: {3}",
                cToken.PositionInLine, cToken.LineNumber, cToken.Type, cToken.Value);
            return cToken;
        }

        /// <summary>
        /// Skips the rest of the tokens on the current line and starts returning them from the next line in the stream.
        /// </summary>
        public void SkipToNextLine()
        {
            this.localTokenizer.SkipToNextLine();
        }

        #endregion
    }
}
