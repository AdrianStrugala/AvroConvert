namespace AvroConvert.Models
{
    using Schema;

    public class Enum
    {
        public EnumSchema Schema { get; private set; }
        private string value;
        public string Value
        {
            get { return value; }
            set
            {
                if (!Schema.Contains(value)) throw new AvroException("Unknown value for enum: " + value + "(" + Schema + ")");
                this.value = value;
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
            return (obj != null && obj is Enum) ? Value.Equals((obj as Enum).Value) : false;
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
