using System;

namespace CHeaderGenerator.Parser
{
    [Serializable]
    public class UnexpectedCharEncounteredException : Exception
    {
        public UnexpectedCharEncounteredException(string message)
            : base(message) { }

        public UnexpectedCharEncounteredException()
            : base() { }
    }
}
