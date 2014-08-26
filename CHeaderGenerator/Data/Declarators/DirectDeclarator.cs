using CHeaderGenerator.Data.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CHeaderGenerator.Data
{
    public class DirectDeclarator : IEquatable<DirectDeclarator>
    {
        public virtual string Identifier { get; set; }

        public override string ToString()
        {
            return this.Identifier;
        }

        public virtual IEnumerable<string> GetNeededDefinitions()
        {
            return new string[] { };
        }

        public virtual bool Equals(DirectDeclarator other)
        {
            return other != null && this.Identifier.Equals(other.Identifier);
        }

        public virtual IEnumerable<string> GetDependencies()
        {
            yield break;
        }

        public virtual int Write(TextWriter writer, int count, int p)
        {
            writer.Write(this.Identifier);
            count += this.Identifier.Length;

            return count;
        }
    }
}
