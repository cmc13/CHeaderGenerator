using NCalc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CHeaderGenerator.Data.Helpers
{
    public static class WriterExtensions
    {
        #region Private Function Definitions

        public static bool IsIgnoredDecl(IReadOnlyCollection<string> ifStack, IReadOnlyCollection<Definition> defns)
        {
            foreach (var s in ifStack)
            {
                try
                {
                    Action<string, FunctionArgs> evalFunc = null;
                    Action<string, ParameterArgs> evalParam = null;

                    evalFunc = (name, args) =>
                    {
                        var def = defns.FirstOrDefault(d => d.Identifier == name && d.Arguments != null
                            && d.Arguments.Count == args.Parameters.Length);
                        if (def != null)
                        {
                            var e2 = new Expression(def.Replacement);

                            for (int i = 0; i < def.Arguments.Count; ++i)
                                e2.Parameters.Add(def.Arguments[i], args.Parameters[i].Evaluate());

                            e2.EvaluateFunction += new EvaluateFunctionHandler(evalFunc);
                            e2.EvaluateParameter += new EvaluateParameterHandler(evalParam);
                            args.Result = e2.Evaluate();
                        }
                    };

                    evalParam = (name, args) =>
                    {
                        var def = defns.FirstOrDefault(d => d.Identifier == name && d.Arguments == null);
                        if (def != null)
                        {
                            var e2 = new Expression(def.Replacement);
                            e2.EvaluateFunction += new EvaluateFunctionHandler(evalFunc);
                            e2.EvaluateParameter += new EvaluateParameterHandler(evalParam);
                            args.Result = e2.Evaluate();
                        }
                    };

                    var e = new Expression(s);
                    e.EvaluateFunction += new EvaluateFunctionHandler(evalFunc);
                    e.EvaluateParameter += new EvaluateParameterHandler(evalParam);

                    var result = e.Evaluate();
                    if (!Convert.ToBoolean(result))
                        return true;
                }
                catch
                {
                    // Empty catch, this is non-trivial, just print it out.
                }
            }

            return false;
        }

        public static int WriteTypeQualifiers(this int typeQualifiers, TextWriter writer, int count)
        {
            string tqString = TypeQualifier.GetTypeQualifierString(typeQualifiers);
            if (!string.IsNullOrEmpty(tqString))
            {
                if (count > 0)
                {
                    writer.Write(' ');
                    count++;
                }

                writer.Write(tqString);
                count += tqString.Length;
            }

            return count;
        }

        public static void WriteIndentTabs(this TextWriter writer, int p)
        {
            for (int i = 0; i < p; ++i)
                writer.Write('\t');
        }

        #endregion
    }
}
