using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CHeaderGenerator.Data
{
    class KeywordAttribute : System.Attribute
    {
        public string Keyword { get; private set; }

        public KeywordAttribute(string keyword)
        {
            this.Keyword = keyword;
        }
    }

    public enum StorageClass
    {
        Default = -1,
        [Keyword("typedef")] Typedef,
        [Keyword("static")] Static,
        [Keyword("auto")] Auto,
        [Keyword("extern")] Extern,
        [Keyword("register")] Register
    }

    public static class Utilities
    {
        public static bool IsStorageClassKeyword(string keyword)
        {
            return GetStorageClass(keyword) != StorageClass.Default;
        }

        public static StorageClass GetStorageClass(string keyword)
        {
            var members = System.Enum.GetValues(typeof(StorageClass))
                .Cast<StorageClass>();
            foreach (var member in members)
            {
                var mem = typeof(StorageClass).GetMember(member.ToString())
                    .FirstOrDefault();
                if (mem != null)
                {
                    var att = mem.GetCustomAttributes(typeof(KeywordAttribute), false)
                        .Cast<KeywordAttribute>()
                        .FirstOrDefault();
                    if (att != null && att.Keyword.Equals(keyword, System.StringComparison.CurrentCulture))
                        return member;
                }
            }
            return StorageClass.Default;
        }

        public static string GetStorageClassString(StorageClass stC)
        {
            var member = typeof(StorageClass).GetMember(stC.ToString())
                .FirstOrDefault();
            if (member != null)
            {
                var att = member.GetCustomAttributes(typeof(KeywordAttribute), false)
                    .Cast<KeywordAttribute>()
                    .FirstOrDefault();
                if (att != null)
                    return att.Keyword;
            }
            return "";
        }
    }

    public static class TypeQualifier
    {
        public const int Default = 0x00;
        public const int Const = 0x01;
        public const int Volatile = 0x02;

        public static bool IsTypeQualifierKeyword(string keyword)
        {
            return keyword.Equals("const") || keyword.Equals("volatile");
        }

        public static string GetTypeQualifierString(int typeQualifier)
        {
            StringBuilder str = new StringBuilder();

            if ((typeQualifier & Const) != 0)
                str.Append("const");

            if ((typeQualifier & Volatile) != 0)
            {
                if (str.Length > 0)
                    str.Append(' ');
                str.Append("volatile");
            }

            return str.ToString();
        }
    }
}
