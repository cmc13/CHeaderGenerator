using CHeaderGenerator.Data;
using System.IO;

namespace CHeaderGenerator.Parser.C
{
    public interface ICParserFactory
    {
        IParser<CSourceFile> CreateParser(Stream stream);
    }
}
