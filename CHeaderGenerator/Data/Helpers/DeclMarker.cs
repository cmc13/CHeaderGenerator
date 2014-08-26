using CHeaderGenerator.Data;
using System;

namespace CHeaderGenerator.Data.Helpers
{
    public class DeclMarker : IEquatable<DeclMarker>
    {
        public Declaration Decl { get; set; }
        public bool Written { get; set; }

        public bool Equals(DeclMarker other)
        {
            return other != null && Decl.Equals(other.Decl) && Written == other.Written;
        }
    }
}
