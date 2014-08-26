namespace CHeaderGenerator.Parser.C
{
    public sealed class CTokenizer : Tokenizer
    {
        public CTokenizer(IScanner scanner)
            : base(scanner)
        {
            base.CommentDelimiters = new CommentDelimiters[] {
                new CommentDelimiters("/*", "*/"),
                new CommentDelimiters("//", EndCommentDelimiter.EndOfLine)
            };
            base.StringEscapeCharacter = '\\';
            base.CharLiteralPunctuator = '\'';
            base.StringLiteralPunctuator = '"';
            base.Punctuators = @"!:#(){}[]\/*=|,.&-+<>;%?^~";
        }
    }
}
