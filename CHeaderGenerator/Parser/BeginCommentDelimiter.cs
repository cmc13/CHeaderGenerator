namespace CHeaderGenerator.Parser
{
    public class BeginCommentDelimiter
    {
        private BeginCommentDelimiter(string str)
        {
            this.Delimiter = str;
        }

        public string Delimiter { get; private set; }

        public static implicit operator string(BeginCommentDelimiter d)
        {
            return d.Delimiter;
        }

        public static implicit operator BeginCommentDelimiter(string s)
        {
            return new BeginCommentDelimiter(s);
        }
    }
}
