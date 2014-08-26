using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CHeaderGenerator.Data
{
    public class ParameterDeclaration : BaseDeclaration<BaseDeclarator>
    {
        public ParameterDeclaration()
        {
            this.SpecifierQualifierList = new SpecifierQualifierList();
        }

        public SpecifierQualifierList SpecifierQualifierList { get; private set; }
        public override IEnumerable<string> Modifiers
        {
            get { return this.SpecifierQualifierList.Modifiers; }
        }
        public override TypeSpecifier TypeSpecifier
        {
            get { return this.SpecifierQualifierList.TypeSpecifier; }
        }

        public override string ToString()
        {
            var str = new StringBuilder();

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

        public virtual int Write(TextWriter writer, int count, int p)
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

            return count;
        }
    }

    public class EllipsisParameterDeclaration : ParameterDeclaration
    {
        public override string ToString()
        {
            return "...";
        }

        public override int Write(TextWriter writer, int count, int p)
        {
            writer.Write("...");
            count += 3;
            return count;
        }
    }
}
