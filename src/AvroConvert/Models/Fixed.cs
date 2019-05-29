namespace AvroConvert.Models
{
    using System;
    using Schema;

    public class Fixed
    {
        protected readonly byte[] value;
        private FixedSchema schema;

        public FixedSchema Schema
        {
            get
            {
                return schema;
            }

            set
            {
                if (!(value is FixedSchema))
                    throw new AvroException("Schema " + value.Name + " in set is not FixedSchema");

                if ((value as FixedSchema).Size != this.value.Length)
                    throw new AvroException("Schema " + value.Name + " Size " + (value as FixedSchema).Size + "is not equal to bytes length " + this.value.Length);

                schema = value;
            }
        }

        public Fixed(FixedSchema schema)
        {
            value = new byte[schema.Size];
            this.Schema = schema;
        }

        public Fixed(FixedSchema schema, byte[] value)
        {
            this.value = new byte[schema.Size];
            this.Schema = schema;
            Value = value;
        }

        protected Fixed(uint size)
        {
            this.value = new byte[size];
        }

        public byte[] Value
        {
            get { return this.value; }
            set
            {
                if (value.Length == this.value.Length)
                {
                    Array.Copy(value, this.value, value.Length);
                    return;
                }
                throw new AvroException("Invalid length for fixed: " + value.Length + ", (" + Schema + ")");
            }
        }

        public override bool Equals(object obj)
        {
            if (this == obj) return true;
            if (obj != null && obj is Fixed)
            {
                Fixed that = obj as Fixed;
                if (that.Schema.Equals(this.Schema))
                {
                    for (int i = 0; i < value.Length; i++) if (this.value[i] != that.value[i]) return false;
                    return true;
                }
            }
            return false;
        }

        public override int GetHashCode()
        {
            int result = Schema.GetHashCode();
            foreach (byte b in value)
            {
                result += 23 * b;
            }
            return result;
        }
    }
}
