using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SolTechnology.Avro.Features.GenerateModel.NetModel
{
    internal class NetClass : INetType
    {
        public string Name { get; set; }
        public string ClassNamespace { get; set; }

        /// <summary>Name and namespace of the record in the schema; kept with [DataContract] so the schema of the class is the same.</summary>
        public string AvroName { get; set; }
        public string AvroNamespace { get; set; }
        public string Doc { get; set; }

        public List<NetClassField> Fields { get; set; } = new List<NetClassField>();

        public void Write(StringBuilder sb)
        {
            CSharp.WriteDoc(sb, Doc, "");
            // With [DataContract] every property is a [DataMember], so the schema is the same with IncludeOnlyDataContractMembers.
            var dataContract = NetEnum.WriteDataContract(sb, Name, AvroName, AvroNamespace);
            sb.AppendLine($"public class {Name}");
            sb.AppendLine("{");
            foreach (NetClassField field in Fields)
            {
                CSharp.WriteDoc(sb, field.Doc, "\t");

                var defaultValue = field.DefaultLiteral();
                if (defaultValue != null)
                {
                    sb.AppendLine($"	[DefaultValue({defaultValue})]");
                }

                foreach (var attribute in field.Attributes)
                {
                    sb.AppendLine($"\t{attribute}");
                }

                if (field.UnionMembers.Count > 0)
                {
                    // typeof cannot take a nullable reference type; the branches of a union are not null anyway.
                    sb.AppendLine($"	[AvroUnion({string.Join(", ", field.UnionMembers.Select(m => $"typeof({m.FieldType.Replace("?", "")})"))})]");
                }
                if (dataContract)
                {
                    sb.AppendLine("\t[DataMember]");
                }
                sb.AppendLine($"	public {field.FieldType} {CSharp.Identifier(field.Name)} {{ get; set; }}");
            }
            sb.AppendLine("}");
            sb.AppendLine();
        }
    }


    internal class NetClassField
    {
        public string FieldType { get; set; }
        public string Name { get; set; }
        public string Namespace { get; set; }
        public string Default { get; set; }
        public string Doc { get; set; }

        /// <summary>The default is a JSON string in the schema (it can still be a number for a .NET type, e.g. "NaN").</summary>
        public bool DefaultIsString { get; set; }

        /// <summary>Set when the default is for a generated enum: it is written as the symbol.</summary>
        public bool DefaultIsEnum { get; set; }

        /// <summary>The .NET type the default is for: the field type, or the first branch of a union.</summary>
        public string DefaultType => (UnionMembers.Count > 0 ? UnionMembers[0].FieldType : FieldType)?.TrimEnd('?');

        /// <summary>Attributes that keep the schema of the property, e.g. [AvroType(AvroTypeRepresentation.TimestampMilliseconds)].</summary>
        public List<string> Attributes { get; } = new();

        /// <summary>The branches of a union field other than null: <c>[AvroUnion(typeof(...))]</c>.</summary>
        public List<NetClassField> UnionMembers { get; } = new();

        /// <summary>The C# argument of [DefaultValue(...)], or null when the default has no C# constant.</summary>
        internal string DefaultLiteral()
        {
            if (Default == null)
            {
                return null;
            }

            var type = DefaultType;
            if (DefaultIsEnum)
            {
                // The symbol as text: the schema gets it as it is, and the code compiles in any namespace and next to a
                // property with the name of the enum (where "Enum.Symbol" would name the property).
                return CSharp.StringLiteral(Default);
            }

            switch (type)
            {
                case "string":
                    return CSharp.StringLiteral(Default);
                case "bool":
                    return bool.TryParse(Default, out var value) ? (value ? "true" : "false") : null;
                case "float":
                case "double":
                    switch (Default)
                    {
                        case "NaN": return $"{type}.NaN";
                        case "Infinity": return $"{type}.PositiveInfinity";
                        case "-Infinity": return $"{type}.NegativeInfinity";
                    }
                    break;
            }

            // A number (also a date or a time as a number) is written as it is; other JSON strings (bytes, fixed) have no C# constant.
            return DefaultIsString || string.IsNullOrWhiteSpace(Default) ? null : Default;
        }
    }
}
