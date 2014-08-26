namespace CHeaderGenerator.Parser
{
    public enum TokenType
    {
        INVALID = 0,
        SYMBOL,
        PUNCTUATOR,
        STRING_LITERAL,
        CHAR_LITERAL,
        NUMERIC_LITERAL,
        WHITESPACE
    }
}
