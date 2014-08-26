using System;

namespace CHeaderGenerator.Parser
{
    public interface IParser<T>
    {
        T PerformParse();
    }
}
