using SolTechnology.Avro.Exceptions;
using SolTechnology.Avro.Schema;

namespace SolTechnology.Avro.Models
{
    public class Enum
    {
        public EnumSchema Schema { get; }
        private string _value;
        public string Value
        {
            get => _value;
            set
            {
                if (!Schema.Contains(value)) throw new AvroException("Unknown value for enum: " + value + "(" + Schema + ")");
                _value = value;
            }
        }

        public Enum(EnumSchema schema, string value)
        {
            this.Schema = schema;
            this.Value = value;
        }

        public override bool Equals(object obj)
        {
            if (obj == this) return true;
            return (obj != null && obj is Enum @enum) && Value.Equals(@enum.Value);
        }

        public override int GetHashCode()
        {
            return 17 * Value.GetHashCode();
        }

        public override string ToString()
        {
            return "Schema: " + Schema + ", value: " + Value;
        }
    }
}
