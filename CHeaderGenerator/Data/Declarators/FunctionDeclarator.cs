using CHeaderGenerator.Data.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CHeaderGenerator.Data
{
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
            count += 2;

            return count;
        }
    }
}
