using CHeaderGenerator.Data.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CHeaderGenerator.Data
{
    public class SpecifierQualifierList : IEquatable<SpecifierQualifierList>
    {
        public SpecifierQualifierList()
        {
            this.TypeQualifiers = TypeQualifier.Default;
            this.Modifiers = new List<string>();
        }

        public TypeSpecifier TypeSpecifier { get; set; }
        public int TypeQualifiers { get; set; }
        public List<string> Modifiers { get; private set; }

        public override string ToString()
        {
            StringBuilder str = new StringBuilder();

            str.Append(string.Join(" ", Modifiers));

            string tqString = TypeQualifier.GetTypeQualifierString(TypeQualifiers);
            if (!string.IsNullOrEmpty(tqString))
            {
                str.Append(' ');
                str.Append(tqString);
            }

            if (TypeSpecifier != null)
            {
                if (str.Length > 0)
                    str.Append(' ');
                str.Append(TypeSpecifier);
            }

            return str.ToString();
        }

        public bool Equals(SpecifierQualifierList other)
        {
            if (other == null)
                return false;

            if (TypeQualifiers != other.TypeQualifiers)
                return false;

            if ((this.TypeSpecifier == null || !this.TypeSpecifier.Equals(other.TypeSpecifier)) && other.TypeSpecifier != null)
                return false;

            return true;
        }

        public virtual int Write(TextWriter writer, int count, int p)
        {
            int localCount = this.TypeQualifiers.WriteTypeQualifiers(writer, 0);
            if (localCount > 0)
                writer.Write(' ');

            count += localCount;

            if (this.TypeSpecifier != null)
                count = this.TypeSpecifier.Write(writer, count, p);

            return count;
        }
    }

    public class DeclarationSpecifiers : SpecifierQualifierList
    {
        public DeclarationSpecifiers()
            : base()
        {
            this.StorageClass = StorageClass.Default;
        }

        public override string ToString()
        {
            var str = new StringBuilder(Utilities.GetStorageClassString(StorageClass));

            if (str.Length > 0)
                str.Append(' ');

            str.Append(base.ToString());

            return str.ToString();
        }

        public StorageClass StorageClass { get; set; }

        public override int Write(TextWriter writer, int count, int p)
        {
            writer.WriteIndentTabs(p);
            count += p;

            count += this.WriteModifiers(writer, count);
            count += this.WriteStorageClass(writer, count);
            count += this.TypeQualifiers.WriteTypeQualifiers(writer, count);

            if (this.TypeSpecifier != null)
            {
                if (count > 0)
                {
                    writer.Write(' ');
                    count++;
                }

                count += this.TypeSpecifier.Write(writer, count, 0);
            }

            return count;
        }

        private int WriteModifiers(TextWriter writer, int count)
        {
            foreach (var mod in this.Modifiers)
            {
                if (count > 0)
                {
                    writer.Write(' ');
                    count++;
                }

                writer.Write(mod);
                count += mod.Length;
            }

            return count;
        }

        private int WriteStorageClass(TextWriter writer, int count)
        {
            if (this.StorageClass == StorageClass.Typedef)
            {
                if (count > 0)
                {
                    writer.Write(' ');
                    count++;
                }

                writer.Write("typedef");
                count += 7;
            }
            else if (this.TypeSpecifier == null
                || this.TypeSpecifier.GetType() == typeof(TypeSpecifier))
            {
                if (count > 0)
                {
                    writer.Write(' ');
                    count++;
                }

                writer.Write("extern");
                count += 6;
            }

            return count;
        }
    }
}
