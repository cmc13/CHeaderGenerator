using CHeaderGenerator.Data.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CHeaderGenerator.Data
{
    public class EnumSpecifier : TypeSpecifier
    {
        public string Identifier { get; set; }
        public List<Enumerator> EnumeratorList { get; set; }
        public override string TypeName
        {
            get
            {
                StringBuilder str = new StringBuilder("enum ");
                if (this.Identifier != null)
                {
                    str.Append(this.Identifier);
                }
                else
                {
                    str.Append('{')
                        .Append(string.Join(", ", from e in EnumeratorList
                                                 select e.ToString()))
                        .Append('}');
                }

                return str.ToString();
            }
            set
            {
                throw new InvalidOperationException("Cannot assign typename to enum specifier.");
            }
        }

        public override bool Equals(TypeSpecifier other)
        {
            if (other == null)
                return false;

            EnumSpecifier enOther = other as EnumSpecifier;
            if (enOther == null)
                return false;

            if (Identifier.Equals(enOther.Identifier))
                return true;

            return false;
        }

        public override IEnumerable<string> GetDependencies()
        {
            if (this.Identifier != null && EnumeratorList == null)
                yield return TypeName;
        }

        public override int Write(TextWriter writer, int count, int p)
        {
            writer.Write("enum");
            count += 4;

            if (this.Identifier != null)
            {
                writer.Write(' ');
                writer.Write(this.Identifier);
                count += this.Identifier.Length + 1;
            }
            else if (this.EnumeratorList != null)
            {
                using (var bracketWriter = new WrappingWriter(() => writer.WriteLine(" {"), () => writer.WriteLine('}')))
                {
                    count += 2;

                    bool first = true;
                    foreach (var enumerator in this.EnumeratorList)
                    {
                        if (!first)
                        {
                            writer.WriteLine(",");
                            count++;
                        }
                        else
                            first = false;

                        enumerator.Write(writer, count, p + 1);
                    }

                    writer.WriteLine();
                    writer.WriteIndentTabs(p);
                    count += p + 2;
                }
            }
            return count;
        }
    }
}
