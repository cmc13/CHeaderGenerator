using System;
using System.IO;

namespace CHeaderGenerator.Data.Helpers
{
    class IncludeGuardWriter : WrappingWriter
    {
        public IncludeGuardWriter(TextWriter writer, string includeGuard)
            : base(() =>
            {
                if (!string.IsNullOrEmpty(includeGuard))
                {
                    writer.WriteLine("#ifndef {0}", includeGuard);
                    writer.WriteLine("#define {0}", includeGuard);
                    writer.WriteLine();
                }
            }, () =>
            {
                if (!string.IsNullOrEmpty(includeGuard))
                {
                    writer.WriteLine();
                    writer.WriteLine("#endif");
                }
            })
        {
        }
    }
}
