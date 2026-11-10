#region license
/**Copyright (c) 2019-2026 Adrian Strugała (SolTechnology)
*
* Licensed under the PolyForm Noncommercial License 1.0.0, the PolyForm Small Business License 1.0.0,
* or the SolTechnology Commercial Licence – pick the one that applies to you. See LICENSE.md:
*
* https://github.com/AdrianStrugala/AvroConvert/blob/master/LICENSE.md
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
*
* Required Notice: Copyright Adrian Strugała (https://soltechnology.dev)
*/
#endregion
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SolTechnology.Avro.Features.GenerateModel
{
    /// <summary>Writing Avro names and values as C# source.</summary>
    internal static class CSharp
    {
        private static readonly HashSet<string> Keywords = new()
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue",
            "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally",
            "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
            "long", "namespace", "new", "null", "object", "operator", "out", "override", "params", "private", "protected",
            "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string",
            "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
            "using", "virtual", "void", "volatile", "while"
        };

        /// <summary>
        /// The "doc" of a record, an enum or a field: an XML comment to read, and [Description] that puts it back in the
        /// schema of the class. An empty "doc" is left out.
        /// </summary>
        internal static void WriteDoc(StringBuilder sb, string doc, string indent)
        {
            if (string.IsNullOrWhiteSpace(doc))
            {
                return;
            }

            sb.AppendLine($"{indent}/// <summary>");
            foreach (var line in doc.Replace("\r\n", "\n").Split('\n'))
            {
                sb.AppendLine($"{indent}/// {XmlDoc(line)}");
            }
            sb.AppendLine($"{indent}/// </summary>");
            sb.AppendLine($"{indent}[Description({StringLiteral(doc)})]");
        }

        /// <summary>An Avro name as a C# identifier: a keyword gets '@', which is not part of the member name (the schema stays the same).</summary>
        internal static string Identifier(string name) => Keywords.Contains(name) ? "@" + name : name;

        internal static string StringLiteral(string value)
        {
            var sb = new StringBuilder("\"");
            foreach (var c in value)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\0': sb.Append("\\0"); break;
                    default:
                        if (char.IsControl(c))
                            sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else
                            sb.Append(c);
                        break;
                }
            }
            return sb.Append('"').ToString();
        }

        internal static string XmlDoc(string text) => text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }
}
