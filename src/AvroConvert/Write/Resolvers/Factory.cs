namespace AvroConvert.Write.Resolvers
{
    using Exceptions;
    using Schema;

    public static class Factory
    {
        public delegate void Writer<in T>(T t);

        private static readonly Array Array;
        private static readonly Map Map;
        private static readonly Null Null;
        private static readonly String String;
        private static readonly Record Record;
        private static readonly Enum Enum;
        private static readonly Fixed Fixed;
        private static readonly Union Union;

        static Factory()
        {
            Array = new Array();
            Null = new Null();
            Map = new Map();
            String = new String();
            Record = new Record();
            Enum = new Enum();
            Fixed = new Fixed();
            Union = new Union();
        }

        public static Encoder.WriteItem ResolveWriter(Schema schema)
        {
            switch (schema.Tag)
            {
                case Schema.Type.Null:
                    return Null.Resolve;
                case Schema.Type.Boolean:
                    return (v, e) => Write<bool>(v, schema.Tag, e.WriteBoolean);
                case Schema.Type.Int:
                    return (v, e) => Write<int>(v, schema.Tag, e.WriteInt);
                case Schema.Type.Long:
                    return (v, e) => Write<long>(v, schema.Tag, e.WriteLong);
                case Schema.Type.Float:
                    return (v, e) => Write<float>(v, schema.Tag, e.WriteFloat);
                case Schema.Type.Double:
                    return (v, e) => Write<double>(v, schema.Tag, e.WriteDouble);
                case Schema.Type.String:
                    return String.Resolve;
                case Schema.Type.Bytes:
                    return (v, e) => Write<byte[]>(v, schema.Tag, e.WriteBytes);
                case Schema.Type.Error:
                case Schema.Type.Record:
                    return Record.Resolve((RecordSchema)schema);
                case Schema.Type.Enumeration:
                    return Enum.Resolve((EnumSchema)schema);
                case Schema.Type.Fixed:
                    return Fixed.Resolve((FixedSchema)schema);
                case Schema.Type.Array:
                    return Array.Resolve((ArraySchema)schema);
                case Schema.Type.Map:
                    return Map.Resolve((MapSchema)schema);
                case Schema.Type.Union:
                    return Union.Resolve((UnionSchema)schema);
                default:
                    return (v, e) =>
                        throw new AvroTypeMismatchException(
                            $"Tried to write against [{schema}] schema, but found [{v.GetType()}] type");
            }
        }

        /// <summary>
        /// A generic method to serialize primitive Avro types.
        /// </summary>
        /// <typeparam name="S">Type of the C# type to be serialized</typeparam>
        /// <param name="value">The value to be serialized</param>
        /// <param name="tag">The schema type tag</param>
        /// <param name="writer">The writer which should be used to write the given type.</param>
        private static void Write<S>(object value, Schema.Type tag, Writer<S> writer)
        {
            if (value == null)
            {
                value = default(S);
            }

            if (!(value is S))
                throw new AvroTypeMismatchException(
                    $"[{typeof(S)}] required to write against [{tag.ToString()}] schema but found " + value.GetType());

            writer((S)value);
        }
    }
}
