namespace CHeaderGenerator.Parser
{
    public abstract class Token
    {
        public Token(int positionInLine, int lineNumber, string value)
        {
            this.PositionInLine = positionInLine;
            this.LineNumber = lineNumber;
            this.Value = value;
        }

        public string Value { get; set; }

        public int LineNumber { get; set; }

        public int PositionInLine { get; set; }
    }

    public class Token<TTokenType> : Token
    {
        public Token(int positionInLine, int lineNumber, TTokenType type, string value)
            : base(positionInLine, lineNumber, value)
        {
            this.Type = type;
        }

        public Token()
            : this(0, 0, default(TTokenType), null) { }

        public TTokenType Type { get; set; }
    }
}
