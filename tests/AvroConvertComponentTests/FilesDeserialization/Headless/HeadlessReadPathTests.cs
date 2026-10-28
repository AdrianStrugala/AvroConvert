using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using NUnit.Framework;
using SolTechnology.Avro;
using SolTechnology.Avro.Policies;

namespace AvroConvertComponentTests.FilesDeserialization.Headless
{
    /// <summary>The read path of one message: the last plan per type is reused, and spans are read through a pooled array.</summary>
    public class HeadlessReadPathTests
    {
        public class Point
        {
            public int X { get; set; }
            public string Name { get; set; }
        }

        private const string XThenName = """{"type":"record","name":"Point","fields":[{"name":"X","type":"int"},{"name":"Name","type":"string"}]}""";
        private const string NameThenX = """{"type":"record","name":"Point","fields":[{"name":"Name","type":"string"},{"name":"X","type":"int"}]}""";

        [Test]
        public void AlternatingWriterSchemas_EachMessageIsReadWithItsOwnSchema()
        {
            var first = AvroSchema.Parse(XThenName);
            var second = AvroSchema.Parse(NameThenX);
            var a = AvroConvert.SerializeHeadless(new Point { X = 1, Name = "a" }, first);
            var b = AvroConvert.SerializeHeadless(new Point { X = 2, Name = "b" }, second);

            for (var i = 0; i < 3; i++)
            {
                AvroConvert.DeserializeHeadless<Point>(a, first).Should().BeEquivalentTo(new Point { X = 1, Name = "a" });
                AvroConvert.DeserializeHeadless<Point>(b, second).Should().BeEquivalentTo(new Point { X = 2, Name = "b" });
                AvroConvert.DeserializeHeadless<Point>(b.AsSpan(), second, second).Should().BeEquivalentTo(new Point { X = 2, Name = "b" });
            }
        }

        [Test]
        public void SameSchemas_DifferentOptions_UseDifferentPlans()
        {
            var writer = AvroSchema.Parse("""{"type":"record","name":"Point","fields":[{"name":"Name","type":"string"}]}""");
            var reader = AvroSchema.Create<Point>();
            var bytes = AvroConvert.SerializeHeadless(new Point { X = 3, Name = "c" }, writer);
            var useDefault = new AvroConvertOptions { MissingFieldHandling = AvroMissingFieldHandling.UseDefault };

            AvroConvert.DeserializeHeadless<Point>(bytes.AsSpan(), writer, reader, useDefault).Should().BeEquivalentTo(new Point { X = 0, Name = "c" });
            var strict = () => AvroConvert.DeserializeHeadless<Point>(bytes.AsSpan(), writer, reader);
            strict.Should().Throw<Exception>();
            AvroConvert.DeserializeHeadless<Point>(bytes.AsSpan(), writer, reader, useDefault).Should().BeEquivalentTo(new Point { X = 0, Name = "c" });
        }

        [Test]
        public void Span_LargerThanThePooledCopyLimit_IsReadInPlace()
        {
            var schema = AvroSchema.Parse("""{"type":"array","items":"string"}""");
            var items = Enumerable.Range(0, 3_000).Select(i => new string('x', 500) + i).ToList();
            var bytes = AvroConvert.SerializeHeadless(items, schema);
            bytes.Length.Should().BeGreaterThan(1024 * 1024);

            AvroConvert.DeserializeHeadless<List<string>>(bytes.AsSpan(), schema, schema).Should().Equal(items);
        }

        [Test]
        public void Span_TruncatedData_Throws()
        {
            var schema = AvroSchema.Parse(XThenName);
            var bytes = AvroConvert.SerializeHeadless(new Point { X = 1, Name = "abcdef" }, schema);

            var act = () => AvroConvert.DeserializeHeadless<Point>(bytes.AsSpan(0, bytes.Length - 3), schema, schema);

            act.Should().Throw<Exception>();
        }
    }
}
