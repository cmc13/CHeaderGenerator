using CHeaderGenerator.Data.Helpers;
using System;
using System.IO;
using System.Text;

namespace CHeaderGenerator.Data
{
    public class StructureDeclarator : IEquatable<StructureDeclarator>
    {
        public StructureDeclarator()
        {
            this.SpecifierQualifierList = new SpecifierQualifierList();
        }

        public SpecifierQualifierList SpecifierQualifierList { get; private set; }
        public Declarator Declarator { get; set; }
        public string ConstantExpression { get; set; }

        public override string ToString()
        {
            var str = new StringBuilder();

            if (this.SpecifierQualifierList != null)
                str.Append(this.SpecifierQualifierList);

            if (this.Declarator != null)
            {
                if (str.Length > 0)
                    str.Append(' ');
                str.Append(this.Declarator);

                if (!string.IsNullOrEmpty(this.ConstantExpression))
                    str.Append(" : ").Append(this.ConstantExpression);
            }

            return str.ToString();
        }

        public bool Equals(StructureDeclarator other)
        {
            if (other == null)
                return false;

            if (!this.SpecifierQualifierList.Equals(other.SpecifierQualifierList))
                return false;

            if ((this.Declarator == null || !this.Declarator.Equals(other.Declarator)) && other.Declarator != null)
                return false;

            if ((this.ConstantExpression == null || !this.ConstantExpression.Equals(other.ConstantExpression)) && other.ConstantExpression != null)
                return false;

            return true;
        }

        public int Write(TextWriter writer, int count, int p)
        {
            writer.WriteIndentTabs(p);

            count = this.SpecifierQualifierList.Write(writer, count, p);

            if (this.Declarator != null)
            {
                writer.Write(' ');
                count += this.Declarator.Write(writer, count, 0) + 1;
            }

            if (this.ConstantExpression != null)
            {
                writer.Write(" : ");
                writer.Write(this.ConstantExpression);
                count += this.ConstantExpression.Length + 3;
            }

            return count;
        }
    }
}
