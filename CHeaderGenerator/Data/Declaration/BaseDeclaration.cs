using System.Collections.Generic;

namespace CHeaderGenerator.Data
{
    public abstract class BaseDeclaration<TDeclarator> where TDeclarator : BaseDeclarator
    {
        public TDeclarator Declarator { get; set; }
        public abstract TypeSpecifier TypeSpecifier { get; }
        public abstract IEnumerable<string> Modifiers { get; }

        public IEnumerable<string> GetDependencies()
        {
            if (this.TypeSpecifier != null)
            {
                foreach (var dep in this.TypeSpecifier.GetDependencies())
                    yield return dep;
            }

            foreach (var mod in this.Modifiers)
                yield return mod;

            if (this.Declarator != null)
            {
                var dependencies = this.Declarator.GetDependencies();
                foreach (var d in dependencies)
                    yield return d;
            }
        }
    }
}
