using System.Collections.Generic;
using System.Text;

namespace SolTechnology.Avro.Features.GenerateModel.NetModel
{
    internal class NetEnum : INetType
    {
        public string Name { get; set; }
        public string AvroName { get; set; }
        public string AvroNamespace { get; set; }
        public List<string> Symbols { get; set; } = new List<string>();

        public void Write(StringBuilder sb)
        {
            WriteDataContract(sb, Name, AvroName, AvroNamespace);
            sb.AppendLine($"public enum {Name}");
            sb.AppendLine("{");
            sb.AppendLine($"	{string.Join(",\r\n	", Symbols)}");
            sb.AppendLine("}");
            sb.AppendLine();
        }

        internal static void WriteDataContract(StringBuilder sb, string name, string avroName, string avroNamespace)
        {
            var arguments = new List<string>();
            if (!string.IsNullOrEmpty(avroName) && avroName != name)
            {
                arguments.Add($"Name = \"{avroName}\"");
            }
            if (!string.IsNullOrEmpty(avroNamespace))
            {
                arguments.Add($"Namespace = \"{avroNamespace}\"");
            }
            if (arguments.Count > 0)
            {
                sb.AppendLine($"[DataContract({string.Join(", ", arguments)})]");
            }
        }
    }
}