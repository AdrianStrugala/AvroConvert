using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Runtime.Serialization;
using AwesomeAssertions;
using NUnit.Framework;
using SolTechnology.Avro;

namespace AvroConvertComponentTests.FullSerializationAndDeserialization
{
    /// <summary>
    /// A record read into a dictionary of objects: for records whose fields are known only from the file. Unlike
    /// <c>dynamic</c>, the result does not depend on the classes that happen to be loaded.
    /// </summary>
    public class RecordAsDictionaryTests
    {
        public class Address
        {
            public string City { get; set; }
            public string Zip { get; set; }
        }

        public class Order
        {
            public long Id { get; set; }
            public Address Address { get; set; }
        }

        public class NullableOrder
        {
            public long Id { get; set; }
#nullable enable annotations
            public Address? Address { get; set; }
#nullable restore
        }

        public class OrderWithDictionary
        {
            public long Id { get; set; }
            public Dictionary<string, object> Address { get; set; }
        }

        public class OrderWithIDictionary
        {
            public long Id { get; set; }
            public IDictionary<string, object> Address { get; set; }
        }

        public class OrderWithReadOnlyDictionary
        {
            public long Id { get; set; }
            public IReadOnlyDictionary<string, object> Address { get; set; }
        }

        public class OrderWithObjectToo
        {
            public long Id { get; set; }
            public Dictionary<string, object> Address { get; set; }
            public object Billing { get; set; }
        }

        public class TwoAddresses
        {
            public long Id { get; set; }
            public Address Address { get; set; }
            public Address Billing { get; set; }
        }

        public class Bag : Dictionary<string, object>
        {
        }

        public class Nested
        {
            public Address One { get; set; }
#nullable enable annotations
            public Address? Maybe { get; set; }
#nullable restore
            public List<Address> Many { get; set; }
            public Dictionary<string, Address> ByName { get; set; }
            public List<List<Address>> Grid { get; set; }
            public Dictionary<string, List<Address>> ListsByName { get; set; }
        }

        public enum Color
        {
            Red,
            Green
        }

        public class Values
        {
            public int Int { get; set; }
            public long Long { get; set; }
            public float Float { get; set; }
            public double Double { get; set; }
            public bool Bool { get; set; }
            public string String { get; set; }
            public byte[] Bytes { get; set; }
            public decimal Decimal { get; set; }
            public DateOnly Date { get; set; }
            public DateTime Timestamp { get; set; }
            public Guid Guid { get; set; }
            public Color Enum { get; set; }
            public int? Missing { get; set; }
        }

        public class Named
        {
            [DataMember(Name = "city_name")]
            public string City { get; set; }
        }

        public class Row
        {
            public long Id { get; set; }
            public string Name { get; set; }
        }

        private static readonly Order Value = new() { Id = 1, Address = new Address { City = "Wroclaw", Zip = "50-001" } };

        private static readonly Dictionary<string, object> ExpectedAddress = new() { ["City"] = "Wroclaw", ["Zip"] = "50-001" };

        private static object Field(object record, string name) =>
            ((IEnumerable<KeyValuePair<string, object>>)record).Single(entry => entry.Key == name).Value;

        private static void ShouldBeTheOrder(object record)
        {
            System.Convert.ToInt64(Field(record, "Id")).Should().Be(1);
            Field(record, "Address").Should().BeOfType<Dictionary<string, object>>().Which.Should().BeEquivalentTo(ExpectedAddress);
        }

        // ------------------------------------------------------------------ targets

        [Test]
        public void Whole_object_is_read_into_a_dictionary(
            [Values(typeof(Dictionary<string, object>), typeof(IDictionary<string, object>), typeof(IReadOnlyDictionary<string, object>),
                typeof(SortedDictionary<string, object>), typeof(ConcurrentDictionary<string, object>), typeof(ReadOnlyDictionary<string, object>), typeof(Bag))]
            Type target,
            [ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            var result = RoundTrip.ReadAs(Value, target, format);

            target.IsInstanceOfType(result).Should().BeTrue();
            ShouldBeTheOrder(result);
        }

        [Test]
        public void Member_is_a_dictionary([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            var result = RoundTrip.ReadAs<OrderWithDictionary>(Value, format);

            result.Id.Should().Be(1);
            result.Address.Should().BeEquivalentTo(ExpectedAddress);
        }

        [Test]
        public void Member_is_an_IDictionary([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            var result = RoundTrip.ReadAs<OrderWithIDictionary>(Value, format);

            result.Address.Should().BeEquivalentTo(ExpectedAddress);
        }

        [Test]
        public void Member_is_an_IReadOnlyDictionary([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            var result = RoundTrip.ReadAs<OrderWithReadOnlyDictionary>(Value, format);

            result.Address.Should().BeEquivalentTo(ExpectedAddress);
        }

        [Test]
        public void Nullable_record_with_a_value([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            var value = new NullableOrder { Id = 1, Address = Value.Address };

            var result = RoundTrip.ReadAs<OrderWithDictionary>(value, format);

            result.Address.Should().BeEquivalentTo(ExpectedAddress);
        }

        [Test]
        public void Nullable_record_without_a_value([ValueSource(typeof(Formats), nameof(Formats.Evolution))] Format format)
        {
            var result = RoundTrip.ReadAs<OrderWithDictionary>(new NullableOrder { Id = 1 }, format);

            result.Id.Should().Be(1);
            result.Address.Should().BeNull();
        }

        [Test]
        public void Empty_record_is_an_empty_dictionary()
        {
            const string schema = @"{""type"":""record"",""name"":""Nothing"",""fields"":[]}";

            var result = AvroConvert.DeserializeHeadless<Dictionary<string, object>>(Array.Empty<byte>(), schema);

            result.Should().BeEmpty();
        }

        // ------------------------------------------------------------------ collections

        [Test]
        public void Collection_of_records_is_a_collection_of_dictionaries(
            [ValueSource(typeof(Formats), nameof(Formats.All))] Format format,
            [Values(typeof(List<Dictionary<string, object>>), typeof(Dictionary<string, object>[]), typeof(IEnumerable<IDictionary<string, object>>), typeof(IReadOnlyList<IReadOnlyDictionary<string, object>>))]
            Type target)
        {
            var value = new List<Row> { new() { Id = 1, Name = "a" }, new() { Id = 2, Name = "b" } };

            var result = (System.Collections.IEnumerable)RoundTrip.ReadAs(value, target, format);

            var rows = result.Cast<object>().ToList();
            rows.Select(row => Field(row, "Name")).Should().Equal("a", "b");
            rows.Select(row => System.Convert.ToInt64(Field(row, "Id"))).Should().Equal(1, 2);
        }

        [Test]
        public void Entries_of_a_file_are_dictionaries(
            [ValueSource(typeof(Formats), nameof(Formats.ContainerWriters))] ContainerWriter writer,
            [ValueSource(typeof(Formats), nameof(Formats.ContainerReaders))] ContainerReader reader)
        {
            var avro = RoundTrip.Write(writer, new[] { Value, Value });

            var result = RoundTrip.ReadAll<Dictionary<string, object>>(reader, avro);

            result.Should().HaveCount(2);
            result.ForEach(ShouldBeTheOrder);
        }

        [Test]
        public void Entries_of_a_file_are_read_through_an_interface(
            [ValueSource(typeof(Formats), nameof(Formats.ContainerReaders))] ContainerReader reader)
        {
            var avro = RoundTrip.Write(ContainerWriter.Bytes, new[] { Value });

            var result = RoundTrip.ReadAll<IReadOnlyDictionary<string, object>>(reader, avro);

            ShouldBeTheOrder(result.Single());
        }

        [Test]
        public void Every_codec_gives_a_dictionary([ValueSource(typeof(Formats), nameof(Formats.Codecs))] CodecType codec)
        {
            var avro = AvroConvert.Serialize(Value, codec);

            ShouldBeTheOrder(AvroConvert.Deserialize<Dictionary<string, object>>(avro));
        }

        [Test]
        public void Options_do_not_change_the_result()
        {
            var options = new AvroConvertOptions();

            var result = AvroConvert.Deserialize<Dictionary<string, object>>(AvroConvert.Serialize(Value), options);

            ShouldBeTheOrder(result);
        }

        [Test]
        public void Read_without_a_header([ValueSource(typeof(Formats), nameof(Formats.HeadlessReaders))] HeadlessReader reader)
        {
            var schema = AvroConvert.GenerateSchema(typeof(Order));
            var avro = AvroConvert.SerializeHeadless(Value, schema);

            ShouldBeTheOrder(RoundTrip.ReadHeadless<Dictionary<string, object>>(reader, avro, schema));
        }

        [Test]
        public void Several_entries_do_not_fit_one_dictionary()
        {
            var avro = RoundTrip.Write(ContainerWriter.Bytes, new[] { Value, Value });

            Recorded.Exception(() => AvroConvert.Deserialize<Dictionary<string, object>>(avro)).Should().NotBeNull();
        }

        [Test]
        public void Merged_files_are_read_into_dictionaries()
        {
            var one = AvroConvert.Serialize(Value);

            var result = AvroConvert.Merge<Dictionary<string, object>>(new[] { one, one });

            AvroConvert.Deserialize<List<Dictionary<string, object>>>(result).Should().HaveCount(2).And.AllSatisfy(ShouldBeTheOrder);
        }

        // ------------------------------------------------------------------ nested records

        private static readonly Nested NestedValue = new()
        {
            One = new Address { City = "a", Zip = "1" },
            Maybe = new Address { City = "b", Zip = "2" },
            Many = new List<Address> { new() { City = "c", Zip = "3" } },
            ByName = new Dictionary<string, Address> { ["key"] = new() { City = "d", Zip = "4" } },
            Grid = new List<List<Address>> { new() { new() { City = "e", Zip = "5" } } },
            ListsByName = new Dictionary<string, List<Address>> { ["key"] = new() { new() { City = "f", Zip = "6" } } }
        };

        [Test]
        public void Nested_records_are_dictionaries_even_when_a_class_matches([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            var result = RoundTrip.ReadAs<Dictionary<string, object>>(NestedValue, format);

            Field(result, "One").Should().BeOfType<Dictionary<string, object>>();
            Field(result, "Maybe").Should().BeOfType<Dictionary<string, object>>();
            ((IEnumerable<object>)Field(result, "Many")).Single().Should().BeOfType<Dictionary<string, object>>();
            Field(Field(result, "ByName"), "key").Should().BeOfType<Dictionary<string, object>>();
            ((IEnumerable<object>)((IEnumerable<object>)Field(result, "Grid")).Single()).Single().Should().BeOfType<Dictionary<string, object>>();
            ((IEnumerable<object>)Field(Field(result, "ListsByName"), "key")).Single().Should().BeOfType<Dictionary<string, object>>();
        }

        [Test]
        public void Object_next_to_a_dictionary_stays_dynamic()
        {
            var avro = AvroConvert.Serialize(new TwoAddresses { Id = 1, Address = Value.Address, Billing = Value.Address });

            var result = AvroConvert.Deserialize<OrderWithObjectToo>(avro);

            result.Address.Should().BeOfType<Dictionary<string, object>>();
            result.Billing.Should().BeOfType<Address>();
        }

        [Test]
        public void Map_read_into_a_dictionary_is_not_changed([ValueSource(typeof(Formats), nameof(Formats.Evolution))] Format format)
        {
            var value = new Dictionary<string, long> { ["a"] = 1 };

            var result = format == Format.Headless
                ? RoundTrip.Evolve<Dictionary<string, object>>(value, format)
                : RoundTrip.ReadAs<Dictionary<string, object>>(value, format);

            result.Should().BeEquivalentTo(new Dictionary<string, object> { ["a"] = 1L });
        }

        // ------------------------------------------------------------------ the same result whatever is loaded

        private const string Manifest = @"{""type"":""record"",""name"":""manifest_entry"",""fields"":[" +
            @"{""name"":""status"",""type"":""int"",""field-id"":0}," +
            @"{""name"":""data_file"",""type"":{""type"":""record"",""name"":""r2"",""fields"":[" +
                @"{""name"":""partition"",""type"":{""type"":""record"",""name"":""r102"",""fields"":[" +
                    @"{""name"":""day"",""type"":[""null"",{""type"":""int"",""logicalType"":""date""}],""default"":null,""field-id"":1000}," +
                    @"{""name"":""region"",""type"":[""null"",""string""],""default"":null,""field-id"":1001}]},""field-id"":102}]},""field-id"":2}]}";

        private static readonly Dictionary<string, object> ManifestEntry = new()
        {
            ["status"] = 1,
            ["data_file"] = new Dictionary<string, object>
            {
                ["partition"] = new Dictionary<string, object> { ["day"] = new DateOnly(2026, 10, 30), ["region"] = "eu" }
            }
        };

        [Test]
        public void Record_with_fields_known_only_from_the_file(
            [ValueSource(typeof(Formats), nameof(Formats.ContainerWriters))] ContainerWriter writer,
            [ValueSource(typeof(Formats), nameof(Formats.ContainerReaders))] ContainerReader reader)
        {
            var schema = AvroSchema.Parse(Manifest);

            var avro = RoundTrip.Write(writer, new[] { ManifestEntry }, schema);

            RoundTrip.ReadAll<Dictionary<string, object>>(reader, avro).Single().Should().BeEquivalentTo(ManifestEntry);
        }

        [Test]
        public void Dictionary_written_with_a_record_schema_comes_back([ValueSource(typeof(Formats), nameof(Formats.Evolution))] Format format)
        {
            var schema = AvroSchema.Parse(Manifest);

            var result = RoundTrip.ReadAs<Dictionary<string, object>>(ManifestEntry, format, schema);

            result.Should().BeEquivalentTo(ManifestEntry);
        }

        // ------------------------------------------------------------------ values

        [Test]
        public void Values_are_the_values_of_dynamic()
        {
            var guid = System.Guid.NewGuid();
            var timestamp = new DateTime(2026, 10, 30, 1, 2, 3, DateTimeKind.Utc);
            var value = new Values
            {
                Int = 1, Long = 2, Float = 3.5f, Double = 4.5, Bool = true, String = "s", Bytes = new byte[] { 9 }, Decimal = 12.34m,
                Date = new DateOnly(2026, 10, 30), Timestamp = timestamp, Guid = guid, Enum = Color.Green
            };

            var result = AvroConvert.Deserialize<Dictionary<string, object>>(AvroConvert.Serialize(value));

            result["Int"].Should().Be(1);
            result["Long"].Should().Be(2L);
            result["Float"].Should().Be(3.5f);
            result["Double"].Should().Be(4.5);
            result["Bool"].Should().Be(true);
            result["String"].Should().Be("s");
            result["Bytes"].Should().BeEquivalentTo(new byte[] { 9 });
            result["Decimal"].Should().Be(12.34m);
            result["Date"].Should().Be(new DateOnly(2026, 10, 30));
            result["Timestamp"].Should().Be(timestamp);
            result["Guid"].Should().Be(guid);
            result["Enum"].Should().Be("Green");
            result["Missing"].Should().BeNull();
        }

        [Test]
        public void Keys_are_the_field_names_of_the_schema()
        {
            var result = AvroConvert.Deserialize<Dictionary<string, object>>(AvroConvert.Serialize(new Named { City = "a" }));

            result.Keys.Should().Equal("city_name");
        }

        // ------------------------------------------------------------------ reader schema

        private const string WriterRecord = @"{""type"":""record"",""name"":""Row"",""fields"":[{""name"":""Id"",""type"":""long""},{""name"":""Name"",""type"":""string""}]}";

        private static byte[] RowData() => AvroConvert.SerializeHeadless(new Row { Id = 1, Name = "a" }, WriterRecord);

        [Test]
        public void Record_reader_schema_chooses_the_fields()
        {
            const string reader = @"{""type"":""record"",""name"":""Row"",""fields"":[{""name"":""Name"",""type"":""string""},{""name"":""Added"",""type"":""int"",""default"":7}]}";

            var result = AvroConvert.DeserializeHeadless<Dictionary<string, object>>(RowData(), AvroSchema.Parse(WriterRecord), AvroSchema.Parse(reader));

            result.Should().BeEquivalentTo(new Dictionary<string, object> { ["Name"] = "a", ["Added"] = 7 });
        }

        [Test]
        public void Key_is_the_name_in_the_reader_schema()
        {
            const string reader = @"{""type"":""record"",""name"":""Row"",""fields"":[{""name"":""Label"",""type"":""string"",""aliases"":[""Name""]}]}";

            var result = AvroConvert.DeserializeHeadless<Dictionary<string, object>>(RowData(), AvroSchema.Parse(WriterRecord), AvroSchema.Parse(reader));

            result.Should().BeEquivalentTo(new Dictionary<string, object> { ["Label"] = "a" });
        }

        [Test]
        public void Union_of_records_into_a_dictionary()
        {
            const string schema = @"[""null"",{""type"":""record"",""name"":""A"",""fields"":[{""name"":""a"",""type"":""int""}]},{""type"":""record"",""name"":""B"",""fields"":[{""name"":""b"",""type"":""string""}]}]";
            var first = RoundTrip.ZigZag(1).Concat(RoundTrip.ZigZag(5)).ToArray();

            var result = AvroConvert.DeserializeHeadless<Dictionary<string, object>>(first, schema);

            result.Should().BeEquivalentTo(new Dictionary<string, object> { ["a"] = 5 });
        }

        [Test]
        public void Union_of_a_map_and_a_record_into_a_dictionary()
        {
            const string schema = @"[{""type"":""map"",""values"":""int""},{""type"":""record"",""name"":""A"",""fields"":[{""name"":""a"",""type"":""int""}]}]";
            var map = RoundTrip.ZigZag(0).Concat(RoundTrip.ZigZag(1)).Concat(RoundTrip.ZigZag(1)).Concat(new[] { (byte)'k' }).Concat(RoundTrip.ZigZag(3)).Concat(RoundTrip.ZigZag(0)).ToArray();
            var record = RoundTrip.ZigZag(1).Concat(RoundTrip.ZigZag(5)).ToArray();

            AvroConvert.DeserializeHeadless<Dictionary<string, object>>(map, schema).Should().BeEquivalentTo(new Dictionary<string, object> { ["k"] = 3 });
            AvroConvert.DeserializeHeadless<Dictionary<string, object>>(record, schema).Should().BeEquivalentTo(new Dictionary<string, object> { ["a"] = 5 });
        }

        // ------------------------------------------------------------------ what stays an error

        [Test]
        public void Dictionary_of_another_value_type_cannot_hold_a_record()
        {
            var avro = AvroConvert.Serialize(Value.Address);

            Recorded.Exception(() => AvroConvert.Deserialize<Dictionary<string, string>>(avro)).Should().NotBeNull();
            Recorded.Exception(() => AvroConvert.Deserialize<Dictionary<int, object>>(avro)).Should().NotBeNull();
        }

        [Test]
        public void Map_reader_schema_of_a_given_type_cannot_read_a_record()
        {
            const string reader = @"{""type"":""map"",""values"":""string""}";

            Recorded.Exception(() => AvroConvert.DeserializeHeadless<Dictionary<string, object>>(RowData(), AvroSchema.Parse(WriterRecord), AvroSchema.Parse(reader)))
                .Should().NotBeNull();
        }

        // ------------------------------------------------------------------ recursion

        private const string LinkedList = @"{""type"":""record"",""name"":""DictionaryNode"",""fields"":[" +
                                          @"{""name"":""value"",""type"":""int""},{""name"":""next"",""type"":[""null"",""DictionaryNode""],""default"":null}]}";

        private static readonly Dictionary<string, object> List = new()
        {
            ["value"] = 1,
            ["next"] = new Dictionary<string, object> { ["value"] = 2, ["next"] = null }
        };

        [Test]
        public void Recursive_record([ValueSource(typeof(Formats), nameof(Formats.ContainerReaders))] ContainerReader reader)
        {
            var avro = AvroConvert.Serialize(new[] { List }, AvroSchema.Parse(LinkedList));

            var result = RoundTrip.ReadAll<Dictionary<string, object>>(reader, avro).Single();

            result.Should().BeEquivalentTo(List);
        }

        [Test]
        public void Recursive_record_of_a_loaded_class()
        {
            var avro = AvroConvert.Serialize(new RecursiveTypesTests.Node { Value = 1, Next = new RecursiveTypesTests.Node { Value = 2 } });

            var result = AvroConvert.Deserialize<Dictionary<string, object>>(avro);

            Field(Field(result, "Next"), "Value").Should().Be(2);
            Field(result, "Next").Should().BeOfType<Dictionary<string, object>>();
        }

        [Test]
        public void Schema_without_an_end_is_an_exception()
        {
            const string endless = @"{""type"":""record"",""name"":""DictionaryEndless"",""fields"":[{""name"":""self"",""type"":""DictionaryEndless""}]}";

            var avro = RoundTrip.RawContainer(endless, Array.Empty<byte>(), 1);

            Recorded.Exception(() => AvroConvert.Deserialize<Dictionary<string, object>>(avro)).Should().NotBeNull();
        }

        [Test]
        public void Data_nested_too_deep_is_an_exception()
        {
            var data = Enumerable.Range(0, 1_000_000).SelectMany(_ => RoundTrip.ZigZag(1).Concat(RoundTrip.ZigZag(1))).ToArray();

            var avro = RoundTrip.RawContainer(LinkedList, data, 1);

            Recorded.Exception(() => AvroConvert.Deserialize<Dictionary<string, object>>(avro)).Should().NotBeNull();
        }
    }
}
