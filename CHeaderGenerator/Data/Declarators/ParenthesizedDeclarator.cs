using CHeaderGenerator.Data.Helpers;
using System;
using System.Collections.Generic;
using System.IO;

namespace CHeaderGenerator.Data
{
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
}
