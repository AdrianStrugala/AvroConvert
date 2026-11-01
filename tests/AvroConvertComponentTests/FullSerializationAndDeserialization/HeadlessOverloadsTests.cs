using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AwesomeAssertions;
using NUnit.Framework;
using SolTechnology.Avro;
using SolTechnology.Avro.Policies;

namespace AvroConvertComponentTests.FullSerializationAndDeserialization
{
    /// <summary>What the overloads kept for older code and their replacements have to agree on.</summary>
    public class HeadlessOverloadsTests
    {
        public class Row
        {
            public long Id { get; set; }
            public string Name { get; set; }
        }

        private static readonly Row[] Rows = { new() { Id = 1, Name = "a" }, new() { Id = 2, Name = "b" }, new() { Id = 3, Name = "c" } };

        private static readonly AvroSchema RowSchema = AvroSchema.Create<Row>();

        private static byte[] Written(IEnumerable<Row> rows) => rows.SelectMany(row => AvroConvert.SerializeHeadless(row, RowSchema)).ToArray();

        [Test]
        public void Type_known_at_runtime_reads_with_the_schema_of_that_type()
        {
            var result = AvroConvert.DeserializeHeadless(AvroConvert.SerializeHeadless(Rows[0], typeof(Row)), typeof(Row));

            ((object)result).Should().BeOfType<Row>().Which.Should().BeEquivalentTo(Rows[0]);
        }

        [Test]
        public void Null_type_is_rejected()
        {
            var bytes = Written(Rows);

            ((Action)(() => AvroConvert.DeserializeHeadless(bytes, null))).Should().Throw<ArgumentNullException>();
            ((Action)(() => AvroConvert.DeserializeHeadless(bytes, RowSchema, null))).Should().Throw<ArgumentNullException>();
        }

        [Test]
        public void Null_writer_schema_is_rejected()
        {
            var bytes = Written(Rows);

            ((Action)(() => AvroConvert.DeserializeHeadless<Row>(bytes, (AvroSchema)null))).Should().Throw<ArgumentNullException>();
            ((Action)(() => AvroConvert.DeserializeHeadless<Row>(new ReadOnlySpan<byte>(bytes), (AvroSchema)null))).Should().Throw<ArgumentNullException>();
            ((Action)(() => AvroConvert.DeserializeHeadless(bytes, (AvroSchema)null, typeof(Row)))).Should().Throw<ArgumentNullException>();
        }

        [Test]
        public void Rows_written_one_after_another_are_read_to_the_end_by_every_runtime_type_overload()
        {
            var bytes = Written(Rows);
            var schema = RowSchema.Json;

            object[] results =
            {
                AvroConvert.DeserializeHeadless(bytes, schema, typeof(List<Row>)),
                AvroConvert.DeserializeHeadless(bytes, schema, schema, typeof(List<Row>)),
                AvroConvert.DeserializeHeadless(bytes, RowSchema, typeof(List<Row>)),
                AvroConvert.DeserializeHeadless(bytes, RowSchema, typeof(List<Row>), RowSchema)
            };

            foreach (var result in results)
            {
                result.Should().BeEquivalentTo(Rows.ToList());
            }
        }

        [Test]
        public void Number_of_rows_limits_every_overload_that_takes_it()
        {
            var bytes = Written(Rows);
            var first = Rows.Take(2).ToList();

            AvroConvert.DeserializeHeadless<List<Row>>(bytes, RowSchema, numberOfRows: 2).Should().BeEquivalentTo(first);
            AvroConvert.DeserializeHeadless(bytes, RowSchema, typeof(List<Row>), numberOfRows: 2).Should().BeEquivalentTo(first);
            AvroConvert.DeserializeHeadless<List<Row>>(bytes, RowSchema.Json, 2).Should().BeEquivalentTo(first);
        }

        [Test]
        public void Items_without_bytes_can_be_counted_only_by_number_of_rows()
        {
            var schema = AvroSchema.Parse(@"{""type"":""record"",""name"":""Empty"",""fields"":[]}");

            AvroConvert.DeserializeHeadless<List<Dictionary<string, object>>>(new byte[1], schema, numberOfRows: 3).Should().HaveCount(3);
        }

        public class Named
        {
            public long RowId { get; set; }
        }

        private sealed class PrefixedMembers : IAvroNamingPolicy
        {
            public AvroTypeInfo GetTypeName(Type type) => new(type.Name, null);

            public string GetMemberName(MemberInfo member) => "prefixed_" + member.Name;
        }

        [Test]
        public void Options_decide_the_schema_of_the_target_when_no_reader_schema_is_given()
        {
            var options = new AvroConvertOptions { NamingPolicy = new PrefixedMembers() };
            var schema = AvroSchema.Create<Named>(options);
            var bytes = AvroConvert.SerializeHeadless(new Named { RowId = 7 }, schema, options);

            AvroConvert.DeserializeHeadless<Named>(bytes, schema, options: options).RowId.Should().Be(7);
            AvroConvert.DeserializeHeadless<Named>(new ReadOnlySpan<byte>(bytes), schema, options: options).RowId.Should().Be(7);
            ((Named)AvroConvert.DeserializeHeadless(bytes, schema, typeof(Named), options: options)).RowId.Should().Be(7);
        }

        [Test]
        public async Task Async_names_and_the_ones_kept_from_the_previews_write_the_same()
        {
            using var renamed = new MemoryStream();
            using var kept = new MemoryStream();

            await AvroConvert.SerializeAsync(ToAsync(Rows), renamed);
            await AvroConvert.Serialize(ToAsync(Rows), kept);

            (await ReadAll(AvroConvert.DeserializeAsyncEnumerable<Row>(new MemoryStream(renamed.ToArray())))).Should().BeEquivalentTo(Rows);
            (await ReadAll(AvroConvert.Deserialize<Row>(new MemoryStream(kept.ToArray())))).Should().BeEquivalentTo(Rows);
        }

        [Test]
        public void Json_takes_the_codec_from_the_options_as_it_did_from_the_argument()
        {
            const string json = @"[{""Id"":1,""Name"":""a""}]";
            var options = new AvroConvertOptions { Codec = CodecType.Deflate };

            foreach (var avro in new[]
                     {
                         AvroConvert.Json2Avro(json, options),
                         AvroConvert.Json2Avro(json, CodecType.Deflate),
                         AvroConvert.Json2Avro<List<Row>>(json, options),
                         AvroConvert.Json2Avro<List<Row>>(json, CodecType.Deflate)
                     })
            {
                System.Text.Encoding.UTF8.GetString(AvroConvert.GetMetadata(avro)["avro.codec"]).Should().Be("deflate");
                AvroConvert.Deserialize<List<Row>>(avro).Should().BeEquivalentTo(new[] { new Row { Id = 1, Name = "a" } });
            }
        }

        [Test]
        public void Json_with_a_scalar_takes_the_options_too()
        {
            var avro = AvroConvert.Json2Avro("7", new AvroConvertOptions { Codec = CodecType.Deflate });

            System.Text.Encoding.UTF8.GetString(AvroConvert.GetMetadata(avro)["avro.codec"]).Should().Be("deflate");
            AvroConvert.Deserialize<long>(avro).Should().Be(7);
        }

        [Test]
        public void Null_options_are_the_defaults()
        {
            const string json = @"{""Id"":1,""Name"":""a""}";

            AvroConvert.Json2Avro(json, (AvroConvertOptions)null).Should().NotBeEmpty();
            AvroConvert.Json2Avro<Row>(json, (AvroConvertOptions)null).Should().NotBeEmpty();
        }

        private static async IAsyncEnumerable<Row> ToAsync(IEnumerable<Row> rows, [EnumeratorCancellation] System.Threading.CancellationToken token = default)
        {
            foreach (var row in rows)
            {
                await Task.Yield();
                yield return row;
            }
        }

        private static async Task<List<Row>> ReadAll(IAsyncEnumerable<Row> rows)
        {
            var result = new List<Row>();
            await foreach (var row in rows)
            {
                result.Add(row);
            }

            return result;
        }
    }
}
