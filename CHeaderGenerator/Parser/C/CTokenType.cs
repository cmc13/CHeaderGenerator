namespace CHeaderGenerator.Parser.C
{
    public enum CTokenType
    {
        INVALID = -1,
        PP_DIRECTIVE,
        PP_SYMBOL,
        SYMBOL,
        KEYWORD,
        STRING_LITERAL,
        NUMERIC_LITERAL,
        CHAR_LITERAL,
        PUNCTUATOR,
        TARGET_IDENTIFIER,
        TYPE_SPECIFIER,
        STRUCTURE_SPECIFIER,
        ENUM_SPECIFIER,
        TERMINATOR,
        BRACES
    }
}
