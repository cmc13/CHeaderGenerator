using CHeaderGenerator.Data;
using System.ComponentModel.Composition;
using System.IO;

namespace CHeaderGenerator.Parser.C
{
    [Export(typeof(ICParserFactory))]
    public class CParserFactory : ICParserFactory
    {
        public IParser<CSourceFile> CreateParser(Stream stream)
        {
            return new CParser(new CLexer(new CTokenizer(new Scanner(stream))));
        }
    }
}
