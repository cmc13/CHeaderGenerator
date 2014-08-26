using CHeaderGenerator.Data.Helpers;
using System;
using System.IO;
using System.Text;

namespace CHeaderGenerator.Data
{
    public class Enumerator : IEquatable<Enumerator>
    {
        public string Identifier { get; set; }
        public string ConstantExpression { get; set; }

        public override string ToString()
        {
            StringBuilder str = new StringBuilder();

            if (!string.IsNullOrEmpty(Identifier))
            {
                str.Append(Identifier);
                if (!string.IsNullOrEmpty(ConstantExpression))
                    str.Append(" = ").Append(ConstantExpression);
            }

            return str.ToString();
        }

        public bool Equals(Enumerator other)
        {
            if (other == null)
                return false;

            if (Identifier != null)
            {
                if (other.Identifier != null)
                {
                    if (Identifier.Equals(other.Identifier))
                    {
                        if (ConstantExpression != null)
                        {
                            if (other.ConstantExpression != null)
                                return ConstantExpression.Equals(other.ConstantExpression);
                        }
                        else if (other.ConstantExpression == null)
                            return true;
                    }
                }
            }
            else if (other.Identifier == null)
                return true;

            return false;
        }

        public int Write(TextWriter writer, int count, int p)
        {
            writer.WriteIndentTabs(p);
            writer.Write(this.Identifier);
            count += this.Identifier.Length + p;

            if (this.ConstantExpression != null)
            {
                writer.Write(" = ");
                writer.Write(this.ConstantExpression);
                count += this.ConstantExpression.Length + 3;
            }

            return count;
        }
    }
}
