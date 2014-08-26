using System;
using System.Runtime.Serialization;

namespace CHeaderGenerator.Parser
{
    [Serializable]
    public class ParserException : Exception
    {
        public int PositionOnLine { get; private set; }
        public int LineNumber { get; private set; }

        public override string Message
        {
            get
            {
                return string.Format("{0}{1}The error occurred at position {2} on line {3}.",
                    base.Message, Environment.NewLine, PositionOnLine, LineNumber);
            }
        }

        public ParserException(string message, Exception innerException, int positionOnLine, int lineNumber)
            : base(message, innerException)
        {
            this.PositionOnLine = positionOnLine;
            this.LineNumber = lineNumber;
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            if (info != null)
            {
                info.AddValue("PositionOnLine", this.PositionOnLine);
                info.AddValue("LineNumber", this.LineNumber);
            }
        }
    }
}
