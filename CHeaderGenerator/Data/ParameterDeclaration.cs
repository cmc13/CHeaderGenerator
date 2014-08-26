using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CHeaderGenerator.Data
{
    public class ParameterDeclaration
    {
        public ParameterDeclaration()
        {
            this.SpecifierQualifierList = new SpecifierQualifierList();
        }

        public SpecifierQualifierList SpecifierQualifierList { get; private set; }
        public BaseDeclarator Declarator { get; set; }

        public override string ToString()
        {
            StringBuilder str = new StringBuilder();

            if (SpecifierQualifierList != null)
                str.Append(SpecifierQualifierList);

            if (Declarator != null)
            {
                if (str.Length > 0)
                    str.Append(' ');
                str.Append(Declarator);
            }

            return str.ToString();
        }

        public IEnumerable<string> GetNeededDefinitions()
        {
            return this.Declarator != null
                ? this.Declarator.GetNeededDefinitions()
                : new string[] { };
        }

        public IEnumerable<string> GetDependencies()
        {
            if (this.SpecifierQualifierList.TypeSpecifier != null)
            {
                foreach (var dep in this.SpecifierQualifierList.TypeSpecifier.GetDependencies())
                    yield return dep;
            }

            foreach (var mod in SpecifierQualifierList.Modifiers)
                yield return mod;

            if (this.Declarator != null)
            {
                foreach (var d in this.Declarator.GetDependencies())
                    yield return d;
            }
        }

        public int Write(TextWriter writer, int count, int p)
        {
            if (this is EllipsisParameterDeclaration)
            {
                writer.Write("...");
                count += 3;
            }
            else
            {
                count += this.SpecifierQualifierList.Write(writer, count, p);
                if (this.Declarator != null)
                {
                    if (count > 0)
                    {
                        writer.Write(' ');
                        count++;
                    }

                    count += this.Declarator.Write(writer, count, 0);
                }
            }

            return count;
        }
    }

    public class EllipsisParameterDeclaration : ParameterDeclaration
    {
        public override string ToString()
        {
            return "...";
        }
    }
}
