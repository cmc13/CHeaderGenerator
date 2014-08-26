namespace CHeaderGenerator.Parser.C
{
    public interface ICLexer : ITokenizer<CTokenType>
    {
        void PushToken(Token<CTokenType> token);
    }
}
