namespace AvroOld.CodeGen
{
    using System.Collections.Generic;
    using System.Text;

    public class CodeGenUtil
    {
        private static readonly CodeGenUtil instance = new CodeGenUtil();
        public static CodeGenUtil Instance { get { return instance; } }
        public HashSet<string> ReservedKeywords { get; private set; }
        private const char At = '@';
        private const char Dot = '.';
        public const string Object = "System.Object";

        public CodeGenUtil()
        {
            ReservedKeywords = new HashSet<string>() {
                "abstract","as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class",
                "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event",
                "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto", "if",
                "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace", "new",
                "null", "object", "operator", "out", "override", "params", "private", "protected", "public",
                "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static",
                "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong",
                "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while", "value", "partial" };
        }
        /// <summary>
        /// Append @ to all reserved keywords that appear on the given name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string Mangle(string name)
        {
            var builder = new StringBuilder();
            string[] names = name.Split(Dot);
            for (int i = 0; i < names.Length; ++i)
            {
                if (ReservedKeywords.Contains(names[i]))
                    builder.Append(At);
                builder.Append(names[i]);
                builder.Append(Dot);
            }
            builder.Remove(builder.Length - 1, 1);
            return builder.ToString();
        }
    }
}
