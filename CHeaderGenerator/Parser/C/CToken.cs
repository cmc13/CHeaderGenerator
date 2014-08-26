using System.Linq;

namespace CHeaderGenerator.Parser.C
{
    public class CToken : Token<CTokenType>
    {
        #region Private Data Members

        private static readonly string[] ppDirectives = { "include", "define",
                        "undef", "pragma", "ifdef", "ifndef", "endif", "elif", "module" };
        private static readonly string[] typeSpecifiers = { "int", "void", "char", "short",
                        "int", "long", "float", "double", "signed", "unsigned", "size_t",
                        "div_t", "ldiv_t", "ptrdiff_t", "time_t", "clock_t", "__int64" };
        private static readonly string[] structureSpecifiers = { "struct", "union" };
        private static readonly string[] keywords = {
                                "auto",
                                "break",
                                "case",
                                "const",
                                "continue",
                                "default",
                                "defined",
                                "do",
                                "else",
                                "enum",
                                "extern",
                                "for",
                                "globalvalue",
                                "goto",
                                "if",
                                "pragma",
                                "register",
                                "return",
                                "sizeof",
                                "static",
                                "switch",
                                "typedef",
                                "volatile",
                                "while" };

        #endregion

        #region Public Constructor Definition

        private CToken(Token<TokenType> token)
            : base(token.PositionInLine, token.LineNumber, CTokenType.INVALID, token.Value)
        {
            this.BaseTokenType = token.Type;
        }

        #endregion

        #region Public Property Definition

        public TokenType BaseTokenType { get; private set; }

        #endregion

        #region Public Function Definitions

        public static CToken Create(Token<TokenType> token)
        {
            return Create(token, null);
        }

        public static CToken Create(Token<TokenType> token, Token<TokenType> prevToken)
        {
            var cSourceToken = new CToken(token);
            cSourceToken.Type = MapTokenType(token, prevToken);
            return cSourceToken;
        }

        #endregion

        #region Private Function Definitions

        private static CTokenType MapTokenType(Token<TokenType> token, Token<TokenType> prevToken)
        {
            switch (token.Type)
            {
                case TokenType.PUNCTUATOR:
                    if (token.Value == "#")
                        return CTokenType.PP_SYMBOL;
                    else if (token.Value == ";")
                        return CTokenType.TERMINATOR;
                    return CTokenType.PUNCTUATOR;

                case TokenType.SYMBOL:
                    return MapSymbolTokenType(token, prevToken);

                case TokenType.STRING_LITERAL:
                    return CTokenType.STRING_LITERAL;

                case TokenType.NUMERIC_LITERAL:
                    return CTokenType.NUMERIC_LITERAL;

                case TokenType.CHAR_LITERAL:
                    return CTokenType.CHAR_LITERAL;
            }

            return CTokenType.INVALID;
        }

        private static CTokenType MapSymbolTokenType(Token<TokenType> token, Token<TokenType> prevToken)
        {
            if (token.Value == "main" || token.Value == "main_program")
                return CTokenType.TARGET_IDENTIFIER;
            else if (ppDirectives.Contains(token.Value))
            {
                if (prevToken != null && prevToken.Type == TokenType.PUNCTUATOR
                    && prevToken.Value.Equals("#"))
                    return CTokenType.PP_DIRECTIVE;
                else
                    return CTokenType.SYMBOL;
            }
            else if (new string[] { "if", "else" }.Contains(token.Value)
                        && prevToken != null && prevToken.Type == TokenType.PUNCTUATOR
                        && prevToken.Value.Equals("#"))
            {
                return CTokenType.PP_DIRECTIVE;
            }
            else if (typeSpecifiers.Contains(token.Value))
            {
                return CTokenType.TYPE_SPECIFIER;
            }
            else if (structureSpecifiers.Contains(token.Value))
            {
                return CTokenType.STRUCTURE_SPECIFIER;
            }
            else if (token.Value != null && token.Value.Equals("enum"))
            {
                return CTokenType.ENUM_SPECIFIER;
            }
            else if (keywords.Contains(token.Value))
            {
                return CTokenType.KEYWORD;
            }

            return CTokenType.SYMBOL;
        }

        #endregion
    }
}
