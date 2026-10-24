using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using NUnit.Framework;
using SolTechnology.Avro;
using SolTechnology.Avro.Infrastructure.Exceptions;
using SolTechnology.Avro.Policies;

namespace AvroConvertComponentTests.Streaming
{
    public class StreamingTests
    {
        public class Item
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public DateTime At { get; set; }
        }

        // Large enough for several data blocks (a block is flushed every ~64 KB).
        private static List<Item> Items(int count = 5_000) =>
            Enumerable.Range(0, count)
                .Select(i => new Item { Id = i, Name = new string('x', i % 50), At = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(i) })
                .ToList();

        private static readonly CodecType[] Codecs = { CodecType.Null, CodecType.Deflate, CodecType.Snappy, CodecType.Zstandard };

        [Test]
        public void Serialize_to_stream_writes_one_entry_per_item_and_leaves_the_stream_open([ValueSource(nameof(Codecs))] CodecType codec)
        {
            var items = Items();
            using var output = new MemoryStream();

            AvroConvert.Serialize(items.Select(i => i), output, new AvroConvertOptions { Codec = codec });

            output.CanWrite.Should().BeTrue();
            AvroConvert.Deserialize<List<Item>>(output.ToArray()).Should().BeEquivalentTo(items);
        }

        [Test]
        public async Task Serialize_async_writes_blocks_while_enumerating([ValueSource(nameof(Codecs))] CodecType codec)
        {
            var items = Items();
            using var output = new MemoryStream();
            long writtenBeforeLastItem = 0;

            async IAsyncEnumerable<Item> Source()
            {
                for (var i = 0; i < items.Count; i++)
                {
                    await Task.Yield();
                    if (i == items.Count - 1)
                    {
                        writtenBeforeLastItem = output.Length;
                    }

                    yield return items[i];
                }
            }

            await AvroConvert.Serialize(Source(), output, new AvroConvertOptions { Codec = codec });

            writtenBeforeLastItem.Should().BeGreaterThan(0);
            output.CanWrite.Should().BeTrue();
            AvroConvert.Deserialize<List<Item>>(output.ToArray()).Should().BeEquivalentTo(items);
        }

        [Test]
        public async Task Deserialize_stream_returns_every_entry([ValueSource(nameof(Codecs))] CodecType codec)
        {
            var items = Items();
            var bytes = AvroConvert.Serialize(items, new AvroConvertOptions { Codec = codec });

            var read = await ToListAsync(AvroConvert.Deserialize<Item>(new TrickleStream(bytes, 4096)));

            read.Should().BeEquivalentTo(items, o => o.WithStrictOrdering());
        }

        [Test]
        public async Task Deserialize_stream_survives_reads_of_a_few_bytes()
        {
            var items = Items(20);
            var bytes = AvroConvert.Serialize(items);

            var read = await ToListAsync(AvroConvert.Deserialize<Item>(new TrickleStream(bytes, 3)));

            read.Should().BeEquivalentTo(items, o => o.WithStrictOrdering());
        }

        [Test]
        public async Task Deserialize_stream_returns_items_of_arrays_written_as_one_entry()
        {
            var items = Items(100);
            var bytes = AvroConvert.Serialize(items, new AvroConvertOptions { CollectionMode = AvroCollectionMode.SingleArray });

            (await ToListAsync(AvroConvert.Deserialize<Item>(new MemoryStream(bytes)))).Should().BeEquivalentTo(items);
            (await ToListAsync(AvroConvert.Deserialize<List<Item>>(new MemoryStream(bytes)))).Should().ContainSingle()
                .Which.Should().BeEquivalentTo(items);
        }

        [Test]
        public async Task Empty_sequence_round_trips_to_an_empty_sequence()
        {
            using var output = new MemoryStream();
            await AvroConvert.Serialize(AsyncEnumerable.Empty<Item>(), output);

            output.Length.Should().BeGreaterThan(0, "the header is always written");
            (await ToListAsync(AvroConvert.Deserialize<Item>(new MemoryStream(output.ToArray())))).Should().BeEmpty();
        }

        [Test]
        public async Task Deserialize_stream_honours_cancellation()
        {
            var bytes = AvroConvert.Serialize(Items());
            using var cts = new CancellationTokenSource();
            var read = 0;

            Func<Task> act = async () =>
            {
                await foreach (var _ in AvroConvert.Deserialize<Item>(new MemoryStream(bytes), cancellationToken: cts.Token))
                {
                    if (++read == 10)
                    {
                        cts.Cancel();
                    }
                }
            };

            await act.Should().ThrowAsync<OperationCanceledException>();
            read.Should().BeLessThan(5_000);
        }

        [Test]
        public async Task Deserialize_stream_rejects_data_without_header()
        {
            Func<Task> act = () => ToListAsync(AvroConvert.Deserialize<Item>(new MemoryStream(new byte[] { 1, 2, 3, 4, 5 })));

            await act.Should().ThrowAsync<InvalidAvroObjectException>();
        }

        [Test]
        public void Serialize_into_buffer_writer()
        {
            var items = Items(100);
            var output = new ArrayBufferWriter<byte>();

            AvroConvert.Serialize(items, output, new AvroConvertOptions { Codec = CodecType.Deflate });

            AvroConvert.Deserialize<List<Item>>(output.WrittenSpan.ToArray()).Should().BeEquivalentTo(items);
        }

        [Test]
        public void SerializeHeadless_into_buffer_writer_appends_the_same_bytes()
        {
            var schema = AvroSchema.Create(typeof(Item));
            var first = Items(1)[0];
            var output = new ArrayBufferWriter<byte>();

            AvroConvert.SerializeHeadless(first, output, schema);
            AvroConvert.SerializeHeadless(first, output, schema);

            var single = AvroConvert.SerializeHeadless(first, schema);
            output.WrittenSpan.ToArray().Should().Equal(single.Concat(single));
            AvroConvert.DeserializeHeadless<List<Item>>(output.WrittenSpan.ToArray(), schema).Should().HaveCount(2);
        }

        [Test]
        public async Task Items_without_a_static_schema_use_their_common_runtime_type()
        {
            var items = new List<object> { new Item { Id = 1, Name = "a" }, new Item { Id = 2, Name = "b" } };
            using var output = new MemoryStream();
            using var asyncOutput = new MemoryStream();

            AvroConvert.Serialize<object>(items, output);
            await AvroConvert.Serialize(ToAsync<object>(items), asyncOutput);

            AvroConvert.Deserialize<List<Item>>(output.ToArray()).Should().BeEquivalentTo(items);
            AvroConvert.Deserialize<List<Item>>(asyncOutput.ToArray()).Should().BeEquivalentTo(items);
        }

        [Test]
        public void Items_of_different_runtime_types_are_rejected()
        {
            var items = new List<object> { new Item { Id = 1 }, "text" };

            Action act = () => AvroConvert.Serialize<object>(items, new MemoryStream());

            act.Should().Throw<ArgumentException>().WithMessage("*one runtime type*");
        }

        [Test]
        public void Dictionary_is_written_as_one_map_like_the_byte_array_api()
        {
            var map = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };
            using var output = new MemoryStream();

            AvroConvert.Serialize(map, output);

            AvroConvert.Deserialize<Dictionary<string, int>>(output.ToArray()).Should().BeEquivalentTo(map);
        }

        [Test]
        public async Task Empty_arrays_do_not_swallow_the_next_entry()
        {
            var arrays = new List<List<int>> { new(), new() { 1, 2 }, new() };
            using var output = new MemoryStream();
            AvroConvert.Serialize<List<int>>(arrays, output);
            var bytes = output.ToArray();

            (await ToListAsync(AvroConvert.Deserialize<int>(new MemoryStream(bytes)))).Should().Equal(1, 2);
            (await ToListAsync(AvroConvert.Deserialize<List<int>>(new MemoryStream(bytes)))).Should().BeEquivalentTo(arrays);
        }

        [Test]
        public void OpenDeserializer_reads_an_empty_single_array_file()
        {
            var bytes = AvroConvert.Serialize(new List<int>(), new AvroConvertOptions { CollectionMode = AvroCollectionMode.SingleArray });

            using var reader = AvroConvert.OpenDeserializer<int>(new MemoryStream(bytes));

            reader.HasNext().Should().BeFalse();
        }

        private static async IAsyncEnumerable<T> ToAsync<T>(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                await Task.Yield();
                yield return item;
            }
        }

        private static async Task<List<T>> ToListAsync<T>(IAsyncEnumerable<T> source)
        {
            var list = new List<T>();
            await foreach (var item in source)
            {
                list.Add(item);
            }

            return list;
        }

        /// <summary>Returns at most <c>chunk</c> bytes per read, like a network stream.</summary>
        private sealed class TrickleStream : MemoryStream
        {
            private readonly int _chunk;

            public TrickleStream(byte[] data, int chunk) : base(data)
            {
                _chunk = chunk;
            }

            public override int Read(byte[] buffer, int offset, int count) => base.Read(buffer, offset, Math.Min(count, _chunk));

            public override int Read(Span<byte> buffer) => base.Read(buffer[..Math.Min(buffer.Length, _chunk)]);

            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
                base.ReadAsync(buffer[..Math.Min(buffer.Length, _chunk)], cancellationToken);
        }
    }
}
