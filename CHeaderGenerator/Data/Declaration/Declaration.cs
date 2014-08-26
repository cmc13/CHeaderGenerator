using CHeaderGenerator.Data.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CHeaderGenerator.Data
{
    public class Declaration : BaseDeclaration<Declarator>, IEquatable<Declaration>
    {
        public Declaration()
        {
            this.DeclarationSpecifiers = new DeclarationSpecifiers();
        }

        public DeclarationSpecifiers DeclarationSpecifiers { get; set; }
        public override IEnumerable<string> Modifiers
        {
            get { return this.DeclarationSpecifiers.Modifiers; }
        }
        public override TypeSpecifier TypeSpecifier
        {
            get { return this.DeclarationSpecifiers.TypeSpecifier; }
        }
        public IReadOnlyCollection<string> IfStack { get; internal set; }

        public IEnumerable<string> GetNeededDefinitions()
        {
            if (Declarator != null)
            {
                foreach (var defn in Declarator.GetNeededDefinitions())
                    yield return defn;
            }
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", DeclarationSpecifiers, Declarator);
        }

        public void Write(TextWriter writer, List<DeclMarker> declList, IReadOnlyCollection<Definition> defns)
        {
            if (!WriterExtensions.IsIgnoredDecl(this.IfStack, defns))
            {
                for (int i = 0; i < declList.Count; ++i)
                {
                    if (declList[i].Decl.Equals(this))
                    {
                        declList[i].Written = true;
                        break;
                    }
                }

                this.WriteDependencies(writer, declList, defns);

                using (var ifStackWriter = new IfStackWriter(writer, this.IfStack))
                {
                    int count = 0;
                    count += this.DeclarationSpecifiers.Write(writer, count, 0);

                    if (this.Declarator != null)
                    {
                        if (count > 0)
                            writer.Write(' ');
                        count += this.Declarator.Write(writer, count, 0);
                    }
                    writer.WriteLine(';');
                }
            }
        }

        public bool Equals(Declaration other)
        {
            if (other == null)
                return false;

            if (this.DeclarationSpecifiers.TypeQualifiers == other.DeclarationSpecifiers.TypeQualifiers)
            {
                if (this.DeclarationSpecifiers.TypeSpecifier == null)
                {
                    if (other.DeclarationSpecifiers.TypeSpecifier != null)
                        return false;
                }
                else if (other.DeclarationSpecifiers.TypeSpecifier != null)
                {
                    if (!this.DeclarationSpecifiers.TypeSpecifier.Equals(other.DeclarationSpecifiers.TypeSpecifier))
                        return false;
                }
                else
                    return false;

                if (this.Declarator == null)
                {
                    if (other.Declarator != null)
                        return false;
                }
                else if (other.Declarator != null)
                {
                    if (!this.Declarator.Equals(other.Declarator))
                        return false;
                }
                else
                    return false;
            }
            else
                return false;

            return true;
        }

        private void WriteDependencies(TextWriter writer, List<DeclMarker> declList,
            IReadOnlyCollection<Definition> defns)
        {
            var dependencies = this.GetDependencies();

            foreach (var dep in dependencies)
            {
                var dm = FindMatchingDeclaration(dep, declList);
                if (dm != null)
                    dm.Decl.Write(writer, declList, defns);
            }
        }

        private static DeclMarker FindMatchingDeclaration(string dep, IEnumerable<DeclMarker> declList)
        {
            var decl = from d in declList
                       where !d.Written
                        && ((Regex.IsMatch(dep, "^(struct|union|enum) ")
                                && d.Decl.DeclarationSpecifiers.TypeSpecifier.TypeName.Equals(dep)
                                && d.Decl.Declarator == null)
                            || (d.Decl.DeclarationSpecifiers.StorageClass == StorageClass.Typedef
                                && d.Decl.Declarator.DirectDeclarator.Identifier == dep))
                       select d;
            return decl.FirstOrDefault();
        }
    }
}
