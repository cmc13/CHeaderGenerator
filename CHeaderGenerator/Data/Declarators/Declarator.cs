using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CHeaderGenerator.Data
{
    public abstract class BaseDeclarator : IEquatable<BaseDeclarator>
    {
        public Pointer Pointer { get; set; }
        public DirectDeclarator DirectDeclarator { get; set; }

        public virtual IEnumerable<string> GetNeededDefinitions()
        {
            return DirectDeclarator != null
                ? DirectDeclarator.GetNeededDefinitions()
                : new string[] { };
        }

        public IEnumerable<string> GetDependencies()
        {
            if (this.DirectDeclarator != null)
            {
                var dependencies = this.DirectDeclarator.GetDependencies();
                foreach (var d in dependencies)
                    yield return d;
            }
        }

        public override string ToString()
        {
            StringBuilder str = new StringBuilder();

            if (Pointer != null)
                str.Append(Pointer);

            if (DirectDeclarator != null)
            {
                if (str.Length > 0)
                    str.Append(' ');
                str.Append(DirectDeclarator);
            }

            return str.ToString();
        }

        public bool Equals(BaseDeclarator other)
        {
            if (other == null)
                return false;

            return ((Pointer != null && Pointer.Equals(other.Pointer)) || other.Pointer == null)
                && ((DirectDeclarator != null && DirectDeclarator.Equals(other.DirectDeclarator)) || other.DirectDeclarator == null);
        }

        public int Write(TextWriter writer, int count, int p)
        {
            if (this.Pointer != null)
                count += this.Pointer.Write(writer, count, 0);

            if (this.DirectDeclarator != null)
                count += this.DirectDeclarator.Write(writer, count, 0);

            return count;
        }
    }

    public sealed class Declarator : BaseDeclarator, IEquatable<Declarator>
    {
        public string Initializer { get; set; }

        public override string ToString()
        {
            StringBuilder str = new StringBuilder();

            str.Append(base.ToString());

            if (!string.IsNullOrEmpty(Initializer))
                str.Append(" = ").Append(Initializer);

            return str.ToString();
        }

        public bool Equals(Declarator other)
        {
            if (other == null)
                return false;
            else if (Initializer == null)
                return (other.Initializer == null) && base.Equals((BaseDeclarator)other);
            else
                return other.Initializer != null && Initializer.Equals(other.Initializer)
                    && base.Equals((BaseDeclarator)other);
        }
    }
}
