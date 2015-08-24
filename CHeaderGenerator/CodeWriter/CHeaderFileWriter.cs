using CHeaderGenerator.Data;
using CHeaderGenerator.Data.Helpers;
using System.IO;

namespace CHeaderGenerator.CodeWriter
{
    public class CHeaderFileWriter
    {
        #region Public Property Definitions

        public string IncludeGuard { get; set; }
        public string HeaderComment { get; set; }
        public bool IncludeStaticFunctions { get; set; }
        public bool IncludeExternFunctions { get; set; }
        public CommentPlacement HeaderCommentPlacement { get; set; }

        #endregion

        #region Public Function Definitions

        public void WriteHeaderFile(CSourceFile parsedFile, Stream outStream)
        {
            using (var writer = new StreamWriter(outStream))
            {
                if(HeaderCommentPlacement == CommentPlacement.StartOfFile)
                    WriteHeaderComment(writer, this.HeaderComment);

                using (var includeGuardWriter = new IncludeGuardWriter(writer, this.IncludeGuard))
                {
                    if(HeaderCommentPlacement == CommentPlacement.InsideIncludeGuard)
                        WriteHeaderComment(writer, this.HeaderComment);
                    parsedFile.WritePPDefinitions(writer);
                    parsedFile.WriteDeclarations(writer, this.IncludeStaticFunctions, this.IncludeExternFunctions);
                }
            }
        }

        #endregion

        #region Private Function Definitions

        private static void WriteHeaderComment(TextWriter writer, string headerComment)
        {
            if (!string.IsNullOrEmpty(headerComment))
            {
                writer.WriteLine(headerComment);
                writer.WriteLine();
            }
        }

        #endregion
    }
}