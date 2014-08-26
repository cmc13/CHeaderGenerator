namespace CHeaderGenerator.Parser
{
    public class EndCommentDelimiter
    {
        private EndCommentDelimiter(string str)
        {
            this.Delimiter = str;
        }

        public string Delimiter { get; private set; }

        public static EndCommentDelimiter EndOfLine = new EndOfLineEndCommentDelimiter();

        public static implicit operator string(EndCommentDelimiter d)
        {
            return d.Delimiter;
        }

        public static implicit operator EndCommentDelimiter(string s)
        {
            return new EndCommentDelimiter(s);
        }

        private class EndOfLineEndCommentDelimiter : EndCommentDelimiter
        {
            public EndOfLineEndCommentDelimiter()
                : base(null) {}
        }
    }
}
