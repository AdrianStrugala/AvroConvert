#region license
/**
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

/** Modifications copyright(C) 2020 Adrian Strugała **/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using SolTechnology.Avro.AvroObjectServices.FileHeader;
using SolTechnology.Avro.AvroObjectServices.FileHeader.Codec;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.AvroObjectServices.Write;
using SolTechnology.Avro.AvroObjectServices.Write.Typed;
using System.Diagnostics.CodeAnalysis;
using SolTechnology.Avro.Infrastructure;

namespace SolTechnology.Avro.Features.Serialize
{
    internal class Encoder : IDisposable
    {
        internal delegate void WriteItem(object value, Writer encoder);

        private readonly AbstractCodec _codec;
        private readonly Writer _writer;

        private readonly MemoryStream _compressedChunk;
        private readonly Writer _chunkWriter;

        private readonly TypeSchema _schema;
        private readonly AvroConvertOptions _options;
        private Type _typedRootType;
        private WriteItem _typedWriteItem;
        private int _blockCount;
        private readonly int _syncInterval;
        private readonly Header _header;

        /// <summary>Writes a container to <paramref name="output"/>; disposing the encoder writes the last block and flushes it.</summary>
        internal Encoder(TypeSchema schema, Writer output, CodecType codecType, AvroConvertOptions options = null)
        {
            _codec = AbstractCodec.CreateCodec(codecType);
            _header = new Header();
            _syncInterval = DataFileConstants.DefaultSyncInterval;

            _blockCount = 0;
            _writer = output;
            _compressedChunk = _codec is NullCodec ? null : new MemoryStream();
            _chunkWriter = new Writer();

            GenerateSyncData();
            _header.AddMetadata(DataFileConstants.CodecMetadataKey, _codec.Name);
            _header.AddMetadata(DataFileConstants.SchemaMetadataKey, schema.ToString());

            _schema = schema;
            _options = options;

            _writer.WriteHeader(_header);
            _writer.Flush();
        }
        private void GenerateSyncData()
        {
            _header.SyncData = new byte[16];
            System.Security.Cryptography.RandomNumberGenerator.Fill(_header.SyncData);
        }

        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        internal void Append(object datum)
        {
            if (datum == null)
            {
                WriteHelpers.WriteRootNull(_schema, _chunkWriter, _options);
            }
            else
            {
                var type = datum.GetType();
                if (!ReferenceEquals(type, _typedRootType))
                {
                    _typedRootType = type;
                    _typedWriteItem = WritePlanCache.Get(_schema, type, _options);
                }

                _typedWriteItem(datum, _chunkWriter);
            }

            _blockCount++;

            //write buffer if bigger than sync interval
            if (_chunkWriter.Length >= _syncInterval)
            {
                WriteBuffer();
            }
        }

        /// <summary>Appends one container entry encoded directly by <paramref name="write"/>.</summary>
        internal void AppendEncoded(Action<Writer> write)
        {
            write(_chunkWriter);
            _blockCount++;

            if (_chunkWriter.Length >= _syncInterval)
            {
                WriteBuffer();
            }
        }

        /// <summary>Appends every element as its own container entry using a typed writer (no boxing per element).</summary>
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        internal void AppendAll<T>(IEnumerable<T> items)
        {
            var write = WritePlanCache.GetTyped<T>(_schema, _options);
            foreach (var item in items)
            {
                write(_chunkWriter, item);
                _blockCount++;

                if (_chunkWriter.Length >= _syncInterval)
                {
                    WriteBuffer();
                }
            }
        }

        /// <summary>Typed writer for <see cref="Append{T}"/>; fetch once and reuse for every item.</summary>
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        internal Action<Writer, T> TypedWriter<T>() => WritePlanCache.GetTyped<T>(_schema, _options);

        /// <summary>Appends one container entry; returns true when the entry completed a data block written to the output.</summary>
        internal bool Append<T>(Action<Writer, T> write, T item)
        {
            write(_chunkWriter, item);
            _blockCount++;
            if (_chunkWriter.Length >= _syncInterval)
            {
                WriteBuffer();
                return true;
            }

            return false;
        }

        private void WriteBuffer()
        {
            if (_blockCount > 0)
            {
                if (_compressedChunk == null)
                {
                    _writer.WriteDataBlock(_chunkWriter.WrittenSpan, _header.SyncData, _blockCount);
                }
                else
                {
                    _compressedChunk.SetLength(0);
                    _codec.Compress(_chunkWriter.WrittenSpan, _compressedChunk);
                    _writer.WriteDataBlock(_compressedChunk, _header.SyncData, _blockCount);
                }

                _writer.Flush();
                _blockCount = 0;
                _chunkWriter.Reset();
            }
        }

        public void Dispose()
        {
            WriteBuffer();
            _writer.Complete();
            _chunkWriter.Dispose();
            _compressedChunk?.Dispose();
        }
    }
}
