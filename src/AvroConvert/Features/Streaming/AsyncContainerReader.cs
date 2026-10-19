#region license
/**Copyright (c) 2019-2026 Adrian Strugała (SolTechnology)
*
* Licensed under the PolyForm Noncommercial License 1.0.0, the PolyForm Small Business License 1.0.0,
* or the SolTechnology Commercial Licence – pick the one that applies to you. See LICENSE.md:
*
* https://github.com/AdrianStrugala/AvroConvert/blob/master/LICENSE.md
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
*
* Required Notice: Copyright Adrian Strugała (https://soltechnology.dev)
*/
#endregion
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using SolTechnology.Avro.AvroObjectServices.FileHeader;
using SolTechnology.Avro.AvroObjectServices.FileHeader.Codec;
using SolTechnology.Avro.AvroObjectServices.Read;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.Features.DeserializeByLine.LineReaders;
using SolTechnology.Avro.Infrastructure.Exceptions;
using System.Diagnostics.CodeAnalysis;
using SolTechnology.Avro.Infrastructure;

namespace SolTechnology.Avro.Features.Streaming
{
    /// <summary>
    /// Reads an Avro object container file block by block with asynchronous I/O; only the current block is held in memory.
    /// </summary>
    internal sealed class AsyncContainerReader
    {
        private const int InitialBufferSize = 64 * 1024;

        private readonly Stream _input;
        private byte[] _buffer = new byte[InitialBufferSize];
        private int _start;
        private int _end;
        private bool _endOfInput;

        private AsyncContainerReader(Stream input)
        {
            _input = input;
        }

        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        internal static async IAsyncEnumerable<T> Read<T>(Stream input, AvroConvertOptions options, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var container = new AsyncContainerReader(input);
            var header = await container.ReadHeaderAsync(cancellationToken).ConfigureAwait(false);

            var writerSchema = Schema.Parse(header.GetMetadata(DataFileConstants.SchemaMetadataKey));
            var readerSchema = Schema.Create(typeof(T), options);
            if (readerSchema == null || readerSchema.IsEmpty())
            {
                readerSchema = writerSchema;
            }

            // A container holding whole arrays (CollectionMode.Array) streams their items when T is the item type.
            var itemsOfArray = writerSchema.Type == AvroType.Array && readerSchema.Type != AvroType.Array;
            var resolver = itemsOfArray
                ? new Resolver(((ArraySchema)writerSchema).ItemSchema, readerSchema, options)
                : new Resolver(writerSchema, readerSchema, options);
            var codec = AbstractCodec.CreateCodecFromString(header.GetMetadata(DataFileConstants.CodecMetadataKey));

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var block = await container.ReadBlockAsync(header.SyncData, codec, cancellationToken).ConfigureAwait(false);
                if (block.Data == null)
                {
                    yield break;
                }

                var reader = new Reader(block.Data);
                for (long i = 0; i < block.Count; i++)
                {
                    if (itemsOfArray)
                    {
                        var items = new ListLineReader<T>(reader, resolver);
                        while (items.HasNext())
                        {
                            yield return items.ReadNext();
                        }
                    }
                    else
                    {
                        yield return resolver.Resolve<T>(reader);
                    }
                }
            }
        }

        private async Task<Header> ReadHeaderAsync(CancellationToken cancellationToken)
        {
            var magic = DataFileConstants.AvroHeader;
            while (_end - _start < magic.Length && await ReadMoreAsync(cancellationToken).ConfigureAwait(false))
            {
            }

            if (_end - _start < magic.Length || !_buffer.AsSpan(_start, magic.Length).SequenceEqual(magic))
            {
                throw new InvalidAvroObjectException("Object does not contain Avro Header");
            }

            _start += magic.Length;
            return await ParseAsync(reader =>
            {
                var header = reader.ReadHeader();
                reader.ReadFixed(header.SyncData);
                return header;
            }, cancellationToken).ConfigureAwait(false);
        }

        private async Task<(long Count, byte[] Data)> ReadBlockAsync(byte[] syncData, AbstractCodec codec, CancellationToken cancellationToken)
        {
            if (_end == _start && !await ReadMoreAsync(cancellationToken).ConfigureAwait(false))
            {
                return (0, null);
            }

            var (count, size) = await ParseAsync(reader => (reader.ReadLong(), reader.ReadLong()), cancellationToken).ConfigureAwait(false);
            if (count < 0 || size < 0 || size > int.MaxValue - DataFileConstants.SyncSize)
            {
                throw new AvroRuntimeException($"Invalid block: {count} entries, {size} bytes.");
            }

            var length = (int)size + DataFileConstants.SyncSize;
            while (_end - _start < length)
            {
                if (!await ReadMoreAsync(cancellationToken).ConfigureAwait(false))
                {
                    throw new EndOfStreamException();
                }
            }

            var data = _buffer.AsSpan(_start, (int)size).ToArray();
            if (!_buffer.AsSpan(_start + (int)size, DataFileConstants.SyncSize).SequenceEqual(syncData))
            {
                throw new AvroRuntimeException("Invalid sync!");
            }

            _start += length;
            return (count, codec.Decompress(data));
        }

        /// <summary>Runs <paramref name="parse"/> over the buffered bytes, reading more input until it has enough.</summary>
        private async Task<TResult> ParseAsync<TResult>(Func<Reader, TResult> parse, CancellationToken cancellationToken)
        {
            while (true)
            {
                var reader = new Reader(_buffer, _start, _end - _start);
                try
                {
                    var result = parse(reader);
                    _start = (int)reader.Position;
                    return result;
                }
                catch (EndOfStreamException) when (!_endOfInput)
                {
                }

                if (!await ReadMoreAsync(cancellationToken).ConfigureAwait(false))
                {
                    throw new EndOfStreamException();
                }
            }
        }

        private async Task<bool> ReadMoreAsync(CancellationToken cancellationToken)
        {
            if (_endOfInput)
            {
                return false;
            }

            if (_end == _buffer.Length)
            {
                if (_start > 0)
                {
                    Buffer.BlockCopy(_buffer, _start, _buffer, 0, _end - _start);
                    _end -= _start;
                    _start = 0;
                }
                else
                {
                    var size = (int)Math.Min((long)_buffer.Length * 2, Array.MaxLength);
                    if (size <= _buffer.Length)
                    {
                        throw new AvroRuntimeException($"A data block does not fit into {Array.MaxLength} bytes.");
                    }

                    Array.Resize(ref _buffer, size);
                }
            }

            var read = await _input.ReadAsync(_buffer.AsMemory(_end), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                _endOfInput = true;
                return false;
            }

            _end += read;
            return true;
        }
    }
}
