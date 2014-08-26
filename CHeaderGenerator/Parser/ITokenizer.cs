
namespace CHeaderGenerator.Parser
{
    public interface ITokenizer<TTokenType>
    {
        bool HasMoreTokens { get; }
        Token<TTokenType> GetNextToken(bool ignoreWhitespace = true);
        void SkipToNextLine();
    }
}
