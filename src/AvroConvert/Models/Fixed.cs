using System;
using System.Linq;
using SolTechnology.Avro.Schema;

namespace SolTechnology.Avro.Models
{
    public class Fixed
    {
        protected readonly byte[] _value;
        private FixedSchema _schema;

        public FixedSchema Schema
        {
            get => _schema;

            set
            {
                if (!(value is FixedSchema))
                    throw new AvroException("Schema " + value.Name + " in set is not FixedSchema");

                if ((value as FixedSchema).Size != _value.Length)
                    throw new AvroException("Schema " + value.Name + " Size " + (value as FixedSchema).Size + "is not equal to bytes length " + this._value.Length);

                _schema = value;
            }
        }

        public Fixed(FixedSchema schema)
        {
            _value = new byte[schema.Size];
            Schema = schema;
        }

        public Fixed(FixedSchema schema, byte[] value)
        {
            this._value = new byte[schema.Size];
            Schema = schema;
            Value = value;
        }

        protected Fixed(uint size)
        {
            this._value = new byte[size];
        }

        public byte[] Value
        {
            get { return this._value; }
            set
            {
                if (value.Length == this._value.Length)
                {
                    Array.Copy(value, this._value, value.Length);
                    return;
                }
                throw new AvroException("Invalid length for fixed: " + value.Length + ", (" + Schema + ")");
            }
        }

        public override bool Equals(object obj)
        {
            if (this == obj) return true;
            if (obj == null || !(obj is Fixed that)) return false;
            if (!that.Schema.Equals(Schema)) return false;
            return !_value.Where((t, i) => _value[i] != that._value[i]).Any();
        }

        public override int GetHashCode()
        {
            return Schema.GetHashCode() + _value.Sum(b => 23 * b);
        }
    }
}
