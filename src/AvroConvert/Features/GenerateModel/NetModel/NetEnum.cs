using System.Collections.Generic;
using System.Text;

namespace SolTechnology.Avro.Features.GenerateModel.NetModel
{
    internal class NetEnum : INetType
    {
        public string Name { get; set; }
        public List<string> Symbols { get; set; } = new List<string>();

        public void Write(StringBuilder sb)
        {
            sb.AppendLine($"public enum {Name}");
            sb.AppendLine("{");
            sb.AppendLine($"	{string.Join(",\r\n	", Symbols)}");
            sb.AppendLine("}");
            sb.AppendLine();
        }
    }
}