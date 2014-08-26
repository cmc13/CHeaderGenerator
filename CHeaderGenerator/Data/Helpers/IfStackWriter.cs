using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CHeaderGenerator.Data.Helpers
{
    class IfStackWriter : WrappingWriter
    {
        public IfStackWriter(TextWriter writer, IReadOnlyCollection<string> ifStack)
            : base(() =>
            {
                if (ifStack.Count > 0)
                {
                    var ifCond = new StringBuilder();
                    foreach (var ifString in ifStack)
                    {
                        if (ifCond.Length > 0)
                            ifCond.Append(" && ");
                        ifCond.Append(ifString);
                    }

                    writer.Write("#if ");

                    writer.WriteLine(ifCond.ToString());
                }
            }, () =>
            {
                if (ifStack.Count > 0)
                    writer.WriteLine("#endif");
            })
        { }
    }
}
