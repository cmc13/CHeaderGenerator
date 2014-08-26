using CHeaderGenerator.Data.Helpers;
using System;
using System.IO;
using System.Text;

namespace CHeaderGenerator.Data
{
    public class Pointer : IEquatable<Pointer>
    {
        public Pointer InnerPointer { get; set; }
        public int TypeQualifiers { get; set; }

        public override string ToString()
        {
            var str = new StringBuilder();

            if (this.InnerPointer != null)
                str.Append(this.InnerPointer);

            string tqString = TypeQualifier.GetTypeQualifierString(this.TypeQualifiers);
            if (!string.IsNullOrEmpty(tqString))
            {
                if (str.Length > 0)
                    str.Append(' ');

                str.Append(tqString);
            }

            return str.ToString();
        }

        public bool Equals(Pointer other)
        {
            if (other == null)
                return false;

            if (this.TypeQualifiers.Equals(other.TypeQualifiers))
            {
                if (this.InnerPointer != null)
                {
                    if (other.InnerPointer != null)
                        return this.InnerPointer.Equals(other.InnerPointer);
                }
                else if (other.InnerPointer == null)
                    return true;
            }

            return false;
        }

        public int Write(TextWriter writer, int count, int p)
        {
            writer.Write('*');
            count++;

            if (this.TypeQualifiers != TypeQualifier.Default)
            {
                writer.Write(' ');
                count++;
            }

            int localCount = this.TypeQualifiers.WriteTypeQualifiers(writer, 0);
            count += localCount;

            if (localCount > 0)
            {
                writer.Write(' ');
                count++;
            }

            if (this.InnerPointer != null)
                count = this.InnerPointer.Write(writer, count, 0);

            return count;
        }
    }
}
