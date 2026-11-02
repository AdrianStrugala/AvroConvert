using System;
using System.Collections.Generic;
using NUnit.Framework;
using SolTechnology.Avro;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using SolTechnology.Avro.AvroObjectServices.Read;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.AvroObjectServices.Write;
using SolTechnology.Avro.Converters;

namespace AvroConvertUnitTests
{
    /// <summary>Compiled write plans are cached per schema object, so one shape of data has to give one schema object.</summary>
    public class RuntimeSchemaTests
    {
        public sealed class TextConverter : IAvroConverter
        {
            public TypeSchema TypeSchema { get; } = new StringSchema(typeof(Guid));

            public void Serialize(object data, IWriter writer) => writer.WriteString(data.ToString());

            public object Deserialize(IReader reader) => Guid.Parse(reader.ReadString());
        }

        private static Dictionary<string, object> Value() => new() { ["a"] = "x" };

        [Test]
        public void Complete_schema_is_returned_as_it_is()
        {
            var schema = Schema.Create(typeof(string));

            Assert.That(RuntimeSchema.Resolve(schema, new object[] { "x" }, null), Is.SameAs(schema));
        }

        [Test]
        public void One_shape_gives_one_schema_object()
        {
            var schema = Schema.Create(typeof(Dictionary<string, object>));

            var first = RuntimeSchema.Resolve(schema, new[] { Value() }, null);
            var second = RuntimeSchema.Resolve(schema, new[] { Value() }, null);

            Assert.That(second, Is.SameAs(first));
        }

        [Test]
        public void One_shape_gives_one_schema_object_with_converters_too()
        {
            var options = new AvroConvertOptions { AvroConverters = new List<IAvroConverter> { new TextConverter() } };
            var schema = Schema.Create(typeof(Dictionary<string, object>), options);

            var first = RuntimeSchema.Resolve(schema, new[] { Value() }, options);
            var second = RuntimeSchema.Resolve(schema, new[] { Value() }, options);

            Assert.That(second, Is.SameAs(first));
        }
    }
}
