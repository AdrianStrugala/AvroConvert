using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using AwesomeAssertions;
using NUnit.Framework;
using SolTechnology.Avro;

namespace AvroConvertComponentTests.FullSerializationAndDeserialization
{
    /// <summary>
    /// A schema comes with the file, so a recursive record does not need a class. A stack overflow ends the process and no
    /// catch stops it, so whatever a file holds must end in a value or in an exception.
    /// </summary>
    public class RecursiveSchemaWithoutClassTests
    {
        private const string LinkedList = @"{""type"":""record"",""name"":""NoClassNode"",""fields"":[" +
                                          @"{""name"":""value"",""type"":""int""},{""name"":""next"",""type"":[""null"",""NoClassNode""],""default"":null}]}";

        private const string Tree = @"{""type"":""record"",""name"":""NoClassTree"",""fields"":[" +
                                    @"{""name"":""value"",""type"":""int""},{""name"":""children"",""type"":{""type"":""array"",""items"":""NoClassTree""}}]}";

        private const string Keyed = @"{""type"":""record"",""name"":""NoClassKeyed"",""fields"":[" +
                                     @"{""name"":""value"",""type"":""int""},{""name"":""children"",""type"":{""type"":""map"",""values"":""NoClassKeyed""}}]}";

        private const string Mutual = @"{""type"":""record"",""name"":""NoClassA"",""fields"":[{""name"":""value"",""type"":""int""},{""name"":""b"",""type"":[""null"",{""type"":""record"",""name"":""NoClassB"",""fields"":[{""name"":""a"",""type"":[""null"",""NoClassA""]}]}]}]}";

        private const string Endless = @"{""type"":""record"",""name"":""NoClassEndless"",""fields"":[{""name"":""self"",""type"":""NoClassEndless""}]}";

        private static readonly Dictionary<string, object> List = new()
        {
            ["value"] = 1,
            ["next"] = new Dictionary<string, object> { ["value"] = 2, ["next"] = null }
        };

        private static AvroSchema Schema(string json) => AvroSchema.Parse(json);

        private static byte[] Entry(string schema, object value) => AvroConvert.Serialize(value, Schema(schema));

        private static object Next(object record) => ((IDictionary<string, object>)record)["next"];

        private static object Value(object record) => ((IDictionary<string, object>)record)["value"];

        private static byte[] DeepList(int depth)
        {
            var data = new List<byte>(depth * 2);
            for (int i = 0; i < depth; i++)
            {
                data.AddRange(RoundTrip.ZigZag(1));
                data.AddRange(RoundTrip.ZigZag(i == depth - 1 ? 0 : 1));
            }

            return data.ToArray();
        }

        [Test]
        public void Read_as_dynamic([ValueSource(typeof(Formats), nameof(Formats.ContainerReaders))] ContainerReader reader)
        {
            var result = RoundTrip.ReadAll<object>(reader, Entry(LinkedList, new[] { List })).Single();

            result.Should().BeOfType<ExpandoObject>();
            Value(Next(result)).Should().Be(2);
            Next(Next(result)).Should().BeNull();
        }

        [Test]
        public void Read_as_ExpandoObject([ValueSource(typeof(Formats), nameof(Formats.ContainerReaders))] ContainerReader reader)
        {
            var result = RoundTrip.ReadAll<ExpandoObject>(reader, Entry(LinkedList, new[] { List })).Single();

            Value(Next(result)).Should().Be(2);
        }

        [Test]
        public void Read_through_every_format([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            object result = RoundTrip.ReadAs<dynamic>(List, format, Schema(LinkedList));

            Convert.ToInt32(Value(Next(result))).Should().Be(2);
        }

        [Test]
        public void Read_without_a_header([ValueSource(typeof(Formats), nameof(Formats.HeadlessReaders))] HeadlessReader reader)
        {
            var avro = AvroConvert.SerializeHeadless(List, LinkedList);

            object result = RoundTrip.ReadHeadless<object>(reader, avro, LinkedList);

            Value(Next(result)).Should().Be(2);
        }

        [Test]
        public void Recursion_through_an_array()
        {
            var value = new Dictionary<string, object>
            {
                ["value"] = 1,
                ["children"] = new List<object> { new Dictionary<string, object> { ["value"] = 2, ["children"] = new List<object>() } }
            };

            var result = AvroConvert.Deserialize<ExpandoObject>(Entry(Tree, value));

            var child = ((IList<object>)((IDictionary<string, object>)result)["children"]).Single();
            Value(child).Should().Be(2);
        }

        [Test]
        public void Recursion_through_a_map()
        {
            var value = new Dictionary<string, object>
            {
                ["value"] = 1,
                ["children"] = new Dictionary<string, object> { ["k"] = new Dictionary<string, object> { ["value"] = 2, ["children"] = new Dictionary<string, object>() } }
            };

            var result = AvroConvert.Deserialize<ExpandoObject>(Entry(Keyed, value));

            var child = ((IDictionary<string, object>)((IDictionary<string, object>)result)["children"])["k"];
            Value(child).Should().Be(2);
        }

        [Test]
        public void Mutual_recursion()
        {
            var value = new Dictionary<string, object>
            {
                ["value"] = 1,
                ["b"] = new Dictionary<string, object> { ["a"] = new Dictionary<string, object> { ["value"] = 2, ["b"] = null } }
            };

            var result = AvroConvert.Deserialize<ExpandoObject>(Entry(Mutual, value));

            var b = ((IDictionary<string, object>)result)["b"];
            Value(((IDictionary<string, object>)b)["a"]).Should().Be(2);
        }

        public class EndlessClass
        {
            public EndlessClass self { get; set; }
        }

        public class IgnoresTheField
        {
            public int value { get; set; }
        }

        public class TypedNode
        {
            public int value { get; set; }
            public TypedNode next { get; set; }
        }

        private static IEnumerable<(string Name, Action<byte[]> Read)> ReadersOfDynamicData() => new (string, Action<byte[]>)[]
        {
            ("dynamic", avro => AvroConvert.Deserialize<dynamic>(avro)),
            ("ExpandoObject", avro => AvroConvert.Deserialize<ExpandoObject>(avro)),
            ("Avro2Json", avro => AvroConvert.Avro2Json(avro)),
            ("Avro2JsonEncoding", avro => AvroConvert.Avro2JsonEncoding(avro)),
            ("skipped field", avro => AvroConvert.Deserialize<IgnoresTheField>(avro)),
            ("line by line", avro =>
            {
                using var lines = AvroConvert.OpenDeserializer<object>(new System.IO.MemoryStream(avro));
                lines.ReadNext();
            })
        };

        [Test]
        public void Schema_without_an_end_is_an_exception()
        {
            var avro = RoundTrip.RawContainer(Endless, Array.Empty<byte>(), 1);

            foreach (var (name, read) in ReadersOfDynamicData())
            {
                Recorded.Exception(() => read(avro)).Should().NotBeNull(name);
            }

            Recorded.Exception(() => AvroConvert.Deserialize<EndlessClass>(avro)).Should().NotBeNull();
        }

        [Test]
        public void Headless_schema_without_an_end_is_an_exception([ValueSource(typeof(Formats), nameof(Formats.HeadlessReaders))] HeadlessReader reader)
        {
            Recorded.Exception(() => RoundTrip.ReadHeadless<object>(reader, Array.Empty<byte>(), Endless)).Should().NotBeNull();
        }

        [TestCase(1_000_000)]
        public void Data_nested_too_deep_is_an_exception(int depth)
        {
            var avro = RoundTrip.RawContainer(LinkedList, DeepList(depth), 1);

            foreach (var (name, read) in ReadersOfDynamicData())
            {
                Recorded.Exception(() => read(avro)).Should().NotBeNull(name);
            }

            Recorded.Exception(() => AvroConvert.Deserialize<TypedNode>(avro)).Should().NotBeNull();
        }

        [TestCase(100)]
        [TestCase(1_000)]
        [TestCase(3_000)]
        [TestCase(10_000)]
        [TestCase(30_000)]
        public void Depth_of_the_data_never_ends_the_process(int depth)
        {
            var avro = RoundTrip.RawContainer(LinkedList, DeepList(depth), 1);

            foreach (var (_, read) in ReadersOfDynamicData())
            {
                Recorded.Exception(() => read(avro));
            }

            Recorded.Exception(() => AvroConvert.Deserialize<TypedNode>(avro));
        }

        private static IEnumerable<string> DynamicReaders => ReadersOfDynamicData().Select(reader => reader.Name);

        [Test]
        public void Depth_of_the_data_never_ends_the_process_on_a_thread_with_a_small_stack(
            [ValueSource(nameof(DynamicReaders))] string reader, [Values(256, 1024)] int stackKb)
        {
            var read = ReadersOfDynamicData().Single(r => r.Name == reader).Read;
            // Every depth up to where reading stops: the depth at which the stack runs out depends on the thread, and data a
            // decoder still reads must not overflow the stack later (e.g. Avro2Json writing ~1200 levels on 1 MB ended the process).
            var files = Enumerable.Range(1, 60).Select(i => RoundTrip.RawContainer(LinkedList, DeepList(i * 50), 1)).ToList();
            var results = new List<Exception>();

            var thread = new System.Threading.Thread(() => results.AddRange(files.Select(avro => Recorded.Exception(() => read(avro)))), stackKb * 1024);
            thread.Start();
            thread.Join();

            // Each depth is read, or stops because the stack is short – never another error.
            results.Should().HaveCount(files.Count);
            results.Where(e => e != null && !IsShortOfStack(e)).Should().BeEmpty();
            if (stackKb >= 1024)
            {
                results.First().Should().BeNull("50 levels fit a 1 MB stack");
            }
        }

        private static bool IsShortOfStack(Exception exception) =>
            exception is InsufficientExecutionStackException || exception.InnerException is { } inner && IsShortOfStack(inner);

        [Test]
        public void Data_of_usual_depth_is_read()
        {
            var avro = RoundTrip.RawContainer(LinkedList, DeepList(100), 1);

            object record = AvroConvert.Deserialize<dynamic>(avro);
            var length = 0;
            for (; record != null; record = Next(record))
            {
                length++;
            }

            length.Should().Be(100);
            AvroConvert.Deserialize<TypedNode>(avro).next.next.value.Should().Be(1);
        }
    }
}
