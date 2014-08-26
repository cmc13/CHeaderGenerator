using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CHeaderGenerator.Data
{
    public class ArrayDeclarator : DirectDeclarator
    {
        public DirectDeclarator Declarator { get; set; }
        public string ArraySizeExpression { get; set; }

        public override string ToString()
        {
            return string.Format("{0}[{1}]", this.Declarator, this.ArraySizeExpression);
        }

        public override string Identifier
        {
            get { return this.Declarator.Identifier; }
            set { throw new InvalidOperationException(); }
        }

        public override IEnumerable<string> GetNeededDefinitions()
        {
            var r = new Regex(@"\b[a-zA-Z_$][a-zA-Z0-9_$]*\b");
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
}
