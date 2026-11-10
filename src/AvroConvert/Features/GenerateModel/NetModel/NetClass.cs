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

                var unionBranches = field.UnionBranches(defaultValue != null);
                if (unionBranches != null)
                {
                    sb.AppendLine($"	[AvroUnion({string.Join(", ", unionBranches)})]");
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

        /// <summary>Position of "null" in the union of the field, -1 when it has none.</summary>
        public int NullBranch { get; set; } = -1;

        /// <summary>
        /// The arguments of [AvroUnion], or null when the field needs none. A class puts null first, or last for a single
        /// branch with a default ([DefaultValue(5)] int? is ["int", "null"]); null anywhere else is named in [AvroUnion] at
        /// its position: ["int", "null"] without a default is [AvroUnion(typeof(int), null)] int?.
        /// </summary>
        internal List<string> UnionBranches(bool hasDefault)
        {
            // typeof cannot take a nullable reference type; the branches of a union are not null anyway.
            var branches = UnionMembers.Count > 0
                ? UnionMembers.Select(m => $"typeof({m.FieldType.Replace("?", "")})").ToList()
                : new List<string> { $"typeof({FieldType.Replace("?", "")})" };

            var nullWhereTheClassPutsIt = UnionMembers.Count == 0 && hasDefault ? 1 : 0;
            if (NullBranch >= 0 && NullBranch != nullWhereTheClassPutsIt)
            {
                branches.Insert(NullBranch, "null");
                return branches;
            }

            return UnionMembers.Count > 0 ? branches : null;
        }

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
