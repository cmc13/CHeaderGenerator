using System;

namespace CHeaderGenerator.Parser
{
    public interface IScanner
    {
        bool HasMoreCharacters { get; }
        char? GetNextCharacter();
        char? GetNextCharacter(out int lineNumber, out int positionInLine);
        char? Peek();
        string Peek(int numChars);
        void SkipToNextLine();
    }
}
