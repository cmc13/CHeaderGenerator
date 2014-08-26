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

    public class ParenthesizedDeclarator : DirectDeclarator
    {
        public Declarator Declarator { get; set; }

        public override string Identifier
        {
            get { return this.Declarator.DirectDeclarator.Identifier; }
            set { throw new InvalidOperationException(); }
        }

        public override string ToString()
        {
            return string.Format("({0})", Declarator);
        }

        public override IEnumerable<string> GetNeededDefinitions()
        {
            return this.Declarator.GetNeededDefinitions();
        }

        public override IEnumerable<string> GetDependencies()
        {
            return this.Declarator.GetDependencies();
        }

        public override int Write(TextWriter writer, int count, int p)
        {
            using (var parenWriter = new WrappingWriter(() => writer.Write('('), () => writer.Write(')')))
            {
                count += this.Declarator.Write(writer, count, 0) + 2;
            }
            return count;
        }
    }

    public class ArrayDeclarator : DirectDeclarator
    {
        public DirectDeclarator Declarator { get; set; }
        public string ArraySizeExpression { get; set; }

        public override string ToString()
        {
            return string.Format("{0}[{1}]", Declarator, ArraySizeExpression);
        }

        public override string Identifier
        {
            get { return this.Declarator.Identifier; }
            set { throw new InvalidOperationException(); }
        }

        public override IEnumerable<string> GetNeededDefinitions()
        {
            Regex r = new Regex(@"\b[a-zA-Z_$][a-zA-Z0-9_$]*\b");
            var mc = r.Matches(this.ArraySizeExpression);
            foreach (Match m in mc)
                yield return m.Value;
        }

        public override int Write(TextWriter writer, int count, int p)
        {
            if (this.Declarator != null)
                count += this.Declarator.Write(writer, count, 0);

            if (this.ArraySizeExpression != null)
            {
                writer.Write("[{0}]", this.ArraySizeExpression);
                count += this.ArraySizeExpression.Length + 2;
            }

            return count;
        }
    }

    public class FunctionDeclarator : DirectDeclarator
    {
        public FunctionDeclarator()
        {
            this.ParameterTypeList = new List<ParameterDeclaration>();
        }

        public DirectDeclarator Declarator { get; set; }
        public List<ParameterDeclaration> ParameterTypeList { get; private set; }

        public override string Identifier
        {
            get { return this.Declarator.Identifier; }
            set { throw new InvalidOperationException(); }
        }

        public override string ToString()
        {
            return string.Format("{0}({1})", Declarator, string.Join(", ", from p in ParameterTypeList
                                                                           select p.ToString()));
        }

        public override IEnumerable<string> GetNeededDefinitions()
        {
            if (this.Declarator != null)
            {
                foreach (var d in this.Declarator.GetNeededDefinitions())
                    yield return d;
            }

            if (this.ParameterTypeList != null)
            {
                foreach (var p in this.ParameterTypeList)
                {
                    foreach (var d in p.GetNeededDefinitions())
                        yield return d;
                }
            }
        }

        public override IEnumerable<string> GetDependencies()
        {
            if (this.Declarator != null)
            {
                foreach (var d in this.Declarator.GetDependencies())
                    yield return d;
            }

            foreach (var param in this.ParameterTypeList)
            {
                var pDeps = param.GetDependencies();
                foreach (var d in pDeps)
                    yield return d;
            }
        }

        public override int Write(TextWriter writer, int count, int p)
        {
            if (this.Declarator != null)
                count += this.Declarator.Write(writer, count, 0);

            using (var parenWriter = new WrappingWriter(() => writer.Write('('), () => writer.Write(')')))
            {
                count++;
                if (this.ParameterTypeList != null)
                {
                    bool first = true;
                    foreach (var param in this.ParameterTypeList)
                    {
                        if (first)
                            first = false;
                        else
                        {
                            writer.Write(", ");
                            count += 2;
                        }

                        count += param.Write(writer, 0, 0);
                    }
                }
            }
            count++;

            return count;
        }
    }
}
