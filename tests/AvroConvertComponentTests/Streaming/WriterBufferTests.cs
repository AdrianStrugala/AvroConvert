using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using AwesomeAssertions;
using NUnit.Framework;
using SolTechnology.Avro;

namespace AvroConvertComponentTests.Streaming
{
    /// <summary>
    /// Values around and above the writer's internal buffer (4 KB in stream mode, 256 B in memory mode) and the
    /// short-string fast path (up to 20 chars), so every grow, flush and direct-write path is exercised.
    /// </summary>
    public class WriterBufferTests
    {
        public class Text
        {
            public string Value { get; set; }
        }

        public class Blob
        {
            public int Id { get; set; }
            public byte[] Data { get; set; }
        }

        public class Row
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public long Total { get; set; }
        }

        private static IEnumerable<int> Lengths() => new[] { 0, 1, 19, 20, 21, 42, 63, 64, 85, 255, 256, 4_095, 4_096, 4_097, 100_000 };

        private static string Utf8Text(int length) =>
            new(Enumerable.Range(0, length).Select(i => (i % 4) switch { 0 => 'a', 1 => 'ż', 2 => '€', _ => 'Z' }).ToArray());

        [Test]
        public void String_of_any_length([ValueSource(nameof(Lengths))] int length, [ValueSource(typeof(Formats), nameof(Formats.Core))] Format format)
        {
            var value = new Text { Value = Utf8Text(length) };

            var result = RoundTrip.Run<Text>(value, format);

            result.Value.Should().Be(value.Value);
        }

        [Test]
        public void String_with_surrogate_pairs([ValueSource(typeof(Formats), nameof(Formats.Core))] Format format)
        {
            var value = new Text { Value = string.Concat(Enumerable.Repeat("😀ż", 30)) };

            var result = RoundTrip.Run<Text>(value, format);

            result.Value.Should().Be(value.Value);
        }

        [Test]
        public void Bytes_bigger_than_the_buffer([Values(4_095, 4_096, 4_097, 1_000_000)] int size, [ValueSource(typeof(Formats), nameof(Formats.Core))] Format format)
        {
            var data = new byte[size];
            new Random(size).NextBytes(data);
            var value = new Blob { Id = 1, Data = data };

            var result = RoundTrip.Run<Blob>(value, format);

            result.Should().BeEquivalentTo(value);
        }

        [Test]
        public void Headless_into_buffer_writer_matches_headless_bytes()
        {
            var value = new Blob { Id = 7, Data = Enumerable.Range(0, 10_000).Select(i => (byte)i).ToArray() };
            var schema = AvroSchema.Create<Blob>();
            var output = new ArrayBufferWriter<byte>(16);

            AvroConvert.SerializeHeadless(value, output, schema);

            output.WrittenSpan.ToArray().Should().Equal(AvroConvert.SerializeHeadless(value, schema));
        }

        [Test]
        public void Container_into_buffer_writer_reads_back()
        {
            var rows = Rows(20_000);
            var output = new ArrayBufferWriter<byte>(16);

            AvroConvert.Serialize(rows, output);

            AvroConvert.Deserialize<List<Row>>(output.WrittenSpan.ToArray()).Should().BeEquivalentTo(rows);
        }

        [Test]
        public async Task Async_stream_with_many_blocks_reads_back()
        {
            var rows = Rows(20_000);
            using var output = new MemoryStream();

            await AvroConvert.Serialize(rows.ToAsyncEnumerable(), output);

            AvroConvert.Deserialize<List<Row>>(output.ToArray()).Should().BeEquivalentTo(rows);
        }

        [Test]
        public void Merge_of_files_with_many_blocks()
        {
            var first = Rows(10_000);
            var second = Rows(7_000, offset: 10_000);

            var merged = AvroConvert.Merge<Row>(new[] { AvroConvert.Serialize(first), AvroConvert.Serialize(second) });

            AvroConvert.Deserialize<List<Row>>(merged).Should().BeEquivalentTo(first.Concat(second));
        }

        [Test]
        public void Record_with_500_fields([ValueSource(typeof(Formats), nameof(Formats.Core))] Format format)
        {
            var type = WideType(500);
            var value = Activator.CreateInstance(type)!;
            var properties = type.GetProperties();
            for (int i = 0; i < properties.Length; i++)
            {
                properties[i].SetValue(value, i % 2 == 0 ? i * 1_000_003L : $"field-{i}");
            }

            var result = RoundTrip.Run(value, type, format);

            properties.Select(p => p.GetValue(result)).Should().Equal(properties.Select(p => p.GetValue(value)));
        }

        private static List<Row> Rows(int count, int offset = 0) =>
            Enumerable.Range(offset, count).Select(i => new Row { Id = i, Name = $"row-{i}", Total = i * 31L }).ToList();

        /// <summary>A class with <paramref name="count"/> auto-properties, alternating long and string.</summary>
        private static Type WideType(int count)
        {
            var module = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("WideTypes"), AssemblyBuilderAccess.Run).DefineDynamicModule("WideTypes");
            var builder = module.DefineType($"Wide{count}", TypeAttributes.Public | TypeAttributes.Class);
            for (int i = 0; i < count; i++)
            {
                var propertyType = i % 2 == 0 ? typeof(long) : typeof(string);
                var field = builder.DefineField($"_f{i}", propertyType, FieldAttributes.Private);
                var property = builder.DefineProperty($"F{i}", PropertyAttributes.None, propertyType, null);
                const MethodAttributes accessor = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;

                var get = builder.DefineMethod($"get_F{i}", accessor, propertyType, Type.EmptyTypes);
                var il = get.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, field);
                il.Emit(OpCodes.Ret);

                var set = builder.DefineMethod($"set_F{i}", accessor, null, new[] { propertyType });
                il = set.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Stfld, field);
                il.Emit(OpCodes.Ret);

                property.SetGetMethod(get);
                property.SetSetMethod(set);
            }

            return builder.CreateType();
        }
    }
}
