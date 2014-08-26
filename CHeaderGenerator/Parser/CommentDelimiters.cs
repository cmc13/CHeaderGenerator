namespace CHeaderGenerator.Parser
{
    public class CommentDelimiters
    {
        public CommentDelimiters(BeginCommentDelimiter beginCommentDelimiter, EndCommentDelimiter endCommentDelimiter)
        {
            this.BeginCommentDelimiter = beginCommentDelimiter;
            this.EndCommentDelimiter = endCommentDelimiter;
        }

        public BeginCommentDelimiter BeginCommentDelimiter { get; private set; }

        public EndCommentDelimiter EndCommentDelimiter { get; private set; }
    }
}
