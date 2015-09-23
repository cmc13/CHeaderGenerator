using CHeaderGenerator.Data;
using CHeaderGenerator.Data.Helpers;
using System.IO;

namespace CHeaderGenerator.CodeWriter
{
    public class CHeaderFileWriter
    {
        #region Public Property Definitions

        public string IncludeGuard { get; set; }
        public bool IncludeStaticFunctions { get; set; }
        public bool IncludeExternFunctions { get; set; }
        public CommentPlacement HeaderCommentPlacement { get; set; }

        #endregion

        #region Public Function Definitions

        public void WriteHeaderFile(CSourceFile parsedFile, string headerComment, Stream outStream)
        {
            using (var writer = new StreamWriter(outStream))
            {
                if(HeaderCommentPlacement == CommentPlacement.StartOfFile)
                    WriteHeaderComment(writer, headerComment);

                using (var includeGuardWriter = new IncludeGuardWriter(writer, this.IncludeGuard))
                {
                    if(HeaderCommentPlacement == CommentPlacement.InsideIncludeGuard)
                        WriteHeaderComment(writer, headerComment);
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