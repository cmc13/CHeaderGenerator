using System;

namespace CHeaderGenerator.Parser
{
    [Serializable]
    public class InvalidTokenException : Exception
    {
        public InvalidTokenException(string message, Token token, Exception innerException)
            : base(message, innerException)
        {
            this.Token = token;
        }

        public InvalidTokenException(string message, Token token)
            : this(message, token, null) 
        { }

        public InvalidTokenException(Token token, string expectedValue)
            : this(string.Format("Invalid token encountered; found value '{0}', expected '{1}'",
                token.Value, expectedValue), token, null)
        { }

        public InvalidTokenException(Token token)
            : this("Invalid token encountered", token, null)
        { }

        public InvalidTokenException(string message)
            : this(message, null, null)
        { }

        public Token Token { get; private set; }

        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
        {
            base.GetObjectData(info, context);

            if (info != null)
                info.AddValue("Token", this.Token);
        }
    }
}
