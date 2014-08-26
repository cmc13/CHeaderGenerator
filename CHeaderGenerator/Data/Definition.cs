using CHeaderGenerator.CodeWriter;
using CHeaderGenerator.Data.Helpers;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CHeaderGenerator.Data
{
    public class Definition
    {
        public string Identifier { get; set; }
        public List<string> Arguments { get; set; }
        public IReadOnlyCollection<string> IfStack { get; internal set; }
        public string Replacement { get; set; }

        public override string ToString()
        {
            StringBuilder str = new StringBuilder();

            if (!string.IsNullOrEmpty(Identifier))
                str.Append(Identifier);

            if (Arguments != null)
            {
                str.Append('(')
                    .Append(string.Join(", ", Arguments))
                    .Append(')');
            }

            if (!string.IsNullOrEmpty(Replacement))
                str.Append(' ').Append(Replacement);

            return str.ToString();
        }

        public void Write(TextWriter writer, IReadOnlyCollection<Definition> defns)
        {
            if (!WriterExtensions.IsIgnoredDecl(this.IfStack, defns))
            {
                using (var ifStackWriter = new IfStackWriter(writer, this.IfStack))
                {
                    writer.Write("#define ");
                    writer.Write(this.Identifier);

                    if (this.Arguments != null)
                    {
                        using (var parentWriter = new WrappingWriter(() => writer.Write('('), () => writer.Write(')')))
                        {
                            bool first = true;
                            foreach (var arg in this.Arguments)
                            {
                                if (first)
                                    first = false;
                                else
                                    writer.Write(',');
                                writer.Write(arg);
                            }
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(this.Replacement))
                    {
                        writer.Write(' ');
                        writer.WriteLine(this.Replacement.Trim());
                    }
                }
            }
        }
    }
}
