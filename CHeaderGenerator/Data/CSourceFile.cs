using CHeaderGenerator.Data.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CHeaderGenerator.Data
{
    public class Include
    {
        public Include(string file, bool isStandard)
        {
            this.File = file;
            this.IsStandard = isStandard;
        }

        public IReadOnlyCollection<string> IfStack { get; internal set; }
        public string File { get; private set; }
        public bool IsStandard { get; private set; }
    }

    public class CSourceFile
    {
        #region Private Data Members

        private Stack<string> ifStack = new Stack<string>();
        private List<Include> includeList = new List<Include>();
        private List<Declaration> declarations = new List<Declaration>();
        private List<Definition> ppDefinitions = new List<Definition>();

        #endregion

        #region Public Property Definitions

        public IReadOnlyCollection<Include> IncludeList
        {
            get { return this.includeList.AsReadOnly(); }
        }

        public IReadOnlyCollection<Declaration> DeclarationList
        {
            get { return this.declarations.AsReadOnly(); }
        }

        public IReadOnlyCollection<Definition> PreProcessorDefinitions
        {
            get { return this.ppDefinitions.AsReadOnly(); }
        }

        #endregion

        #region Public Function Definitions

        public void PushIfCondition(string cond)
        {
            this.ifStack.Push(cond);
        }

        public string PopIfCond()
        {
            if (this.ifStack.Count > 0)
                return this.ifStack.Pop();
            else
                throw new InvalidOperationException("Invalid preprocessor condition, no corresponding condition exists.");
        }

        public void AddInclude(Include inc)
        {
            inc.IfStack = new List<string>(this.ifStack);
            this.includeList.Add(inc);
        }

        public void AddDeclaration(Declaration decl)
        {
            decl.IfStack = new List<string>(this.ifStack);
            if (!this.declarations.Any(d => DeclaratorsSame(d, decl)
                    && IfStacksSame(d, decl)))
            {
                this.declarations.Add(decl);
            }
        }

        public void AddPreProcessorDefinition(Definition defn)
        {
            defn.IfStack = new List<string>(this.ifStack);
            this.ppDefinitions.Add(defn);
        }

        public void WriteDeclarations(TextWriter writer, bool includeStaticFunctions, bool includeExternFunctions)
        {
            var declList = (from d in this.DeclarationList
                            select new DeclMarker { Decl = d, Written = false })
                           .ToList();

            var funcDeclList = from d in declList
                               where d.Decl.Declarator != null
                                && d.Decl.Declarator.DirectDeclarator != null
                                && d.Decl.Declarator.DirectDeclarator.GetType() == typeof(FunctionDeclarator)
                                && (includeStaticFunctions || d.Decl.DeclarationSpecifiers.StorageClass != StorageClass.Static)
                                && (includeExternFunctions || d.Decl.DeclarationSpecifiers.StorageClass != StorageClass.Extern)
                               select d;

            foreach (var f in funcDeclList)
                f.Decl.Write(writer, declList, this.PreProcessorDefinitions);
        }

        public void WritePPDefinitions(TextWriter writer)
        {
            foreach (var neededDefn in this.DeclarationList.SelectMany(d => d.GetNeededDefinitions()).Distinct())
            {
                var defn = this.PreProcessorDefinitions.FirstOrDefault(d => d.Identifier == neededDefn);
                if (defn != null)
                    defn.Write(writer, this.PreProcessorDefinitions);
            }
        }

        #endregion

        #region Private Function Definitions

        private static bool DeclaratorsSame(Declaration d1, Declaration d2)
        {
            return d1.Declarator != null && d2.Declarator != null
                && d1.Declarator.Equals(d2.Declarator);
        }

        private static bool IfStacksSame(Declaration d1, Declaration d2)
        {
            if (d1.IfStack == null && d2.IfStack == null)
                return true;

            if (d1.IfStack != null && d2.IfStack != null
                && d1.IfStack.Count == d2.IfStack.Count)
            {
                var list1 = new List<string>(d1.IfStack);
                var list2 = new List<string>(d2.IfStack);
                list1.Sort();
                list2.Sort();

                for (int i = 0; i < list1.Count; ++i)
                {
                    if (!list1[i].Equals(list2[i]))
                        return false;
                }

                return true;
            }

            return false;
        }

        #endregion
    }
}
