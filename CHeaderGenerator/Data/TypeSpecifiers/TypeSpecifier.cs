using System;
using System.Collections.Generic;
using System.IO;

namespace CHeaderGenerator.Data
{
    public class TypeSpecifier : IEquatable<TypeSpecifier>
    {
        public virtual string TypeName { get; set; }

        public virtual bool Equals(TypeSpecifier other)
        {
            return other != null && this.TypeName.Equals(other.TypeName);
        }

        public override string ToString()
        {
            return this.TypeName;
        }

        public virtual IEnumerable<string> GetDependencies()
        {
            yield return TypeName;
        }

        public virtual int Write(TextWriter writer, int count, int p)
        {
            writer.Write(this.TypeName);
            count += this.TypeName.Length;

            return count;
        }
    }
}
