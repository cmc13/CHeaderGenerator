using CHeaderGenerator.Data.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CHeaderGenerator.Data
{
    public enum StructureType
    {
        Struct,
        Union
    }

    public class StructureSpecifier : TypeSpecifier
    {
        public StructureType StructureType { get; set; }
        public string Identifier { get; set; }
        public List<StructureDeclarator> StructureDeclarationList { get; set; }
        public override string TypeName
        {
            get
            {
                StringBuilder str = new StringBuilder("struct ");
                if (this.Identifier != null)
                {
                    str.Append(this.Identifier);
                }
                else
                {
                    str.Append('{')
                        .Append(string.Join("; ", from s in StructureDeclarationList
                                                 select s.ToString()))
                        .Append('}');
                }

                return str.ToString();
            }
            set
            {
                throw new InvalidOperationException("Cannot assign typename to structure specifier.");
            }
        }

        public override bool Equals(TypeSpecifier other)
        {
            if (other == null)
                return false;

            StructureSpecifier stOther = other as StructureSpecifier;
            if (stOther == null)
                return false;

            if (this.Identifier != null)
            {
                if (this.Identifier.Equals(stOther.Identifier))
                    return true;
            }
            else if (stOther.Identifier == null)
            {
                if (this.StructureDeclarationList.Count == stOther.StructureDeclarationList.Count)
                {
                    for (int i = 0; i < this.StructureDeclarationList.Count; ++i)
                    {
                        if (!stOther.StructureDeclarationList[i].Equals(this.StructureDeclarationList[i]))
                            return false;
                    }

                    return true;
                }
            }

            return false;
        }

        public override IEnumerable<string> GetDependencies()
        {
            if (this.StructureDeclarationList != null)
            {
                foreach (var st in this.StructureDeclarationList)
                {
                    if (st.SpecifierQualifierList.TypeSpecifier != null)
                    {
                        foreach (var d in st.SpecifierQualifierList.TypeSpecifier.GetDependencies())
                            yield return d;
                    }

                    if (st.Declarator != null)
                    {
                        foreach (var d in st.Declarator.GetDependencies())
                            yield return d;
                    }
                }
            }
            else if (Identifier != null)
                yield return TypeName;
        }

        public override int Write(TextWriter writer, int count, int p)
        {
            if (this.StructureType == StructureType.Union)
            {
                writer.Write("union");
                count += 5;
            }
            else
            {
                writer.Write("struct");
                count += 6;
            }

            if (this.Identifier != null)
            {
                writer.Write(' ');
                writer.Write(this.Identifier);
                count += this.Identifier.Length + 1;
            }
            else if (this.StructureDeclarationList != null)
            {
                using (var bracketWriter = new WrappingWriter(() => writer.WriteLine(" {"), () => writer.WriteLine('}')))
                {
                    count += 2;

                    foreach (var decl in this.StructureDeclarationList)
                    {
                        count = decl.Write(writer, count, p + 1);
                        writer.WriteLine(';');
                        count++;
                    }

                    writer.WriteIndentTabs(p);
                    count += p + 1;
                }
            }

            return count;
        }
    }
}
