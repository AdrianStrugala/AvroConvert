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
using System.IO;
using SolTechnology.Avro.AvroObjectServices.FileHeader;
using SolTechnology.Avro.AvroObjectServices.FileHeader.Codec;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.AvroObjectServices.Write;
using SolTechnology.Avro.AvroObjectServices.Write.Typed;

namespace SolTechnology.Avro.Features.Serialize
{
    internal class Encoder : IDisposable
    {
        internal delegate void WriteItem(object value, IWriter encoder);

        private readonly AbstractCodec _codec;
        private readonly Stream _outStream;
        private readonly Writer _writer;

        private readonly MemoryStream _memoryChunk;
        private readonly MemoryStream _compressedChunk;
        private readonly Writer _chunkWriter;

        private WriteItem _writeItem;
        private readonly TypeSchema _schema;
        private readonly AvroConvertOptions _options;
        private Type _typedRootType;
        private WriteItem _typedWriteItem;
        private int _blockCount;
        private readonly int _syncInterval;
        private readonly Header _header;


        internal Encoder(TypeSchema schema, Stream outOutStream, CodecType codecType, AvroConvertOptions options = null)
        {
            _codec = AbstractCodec.CreateCodec(codecType);
            _outStream = outOutStream;
            _header = new Header();
            _syncInterval = DataFileConstants.DefaultSyncInterval;

            _blockCount = 0;
            _writer = new Writer(_outStream);
            _memoryChunk = new MemoryStream();
            _compressedChunk = _codec is NullCodec ? null : new MemoryStream();
            _chunkWriter = new Writer(_memoryChunk);

            GenerateSyncData();
            _header.AddMetadata(DataFileConstants.CodecMetadataKey, _codec.Name);
            _header.AddMetadata(DataFileConstants.SchemaMetadataKey, schema.ToString());

            _schema = schema;
            _options = options;

            _writer.WriteHeader(_header);
        }
        private void GenerateSyncData()
        {
            _header.SyncData = new byte[16];
            System.Security.Cryptography.RandomNumberGenerator.Fill(_header.SyncData);
        }

        internal void Append(object datum)
        {
            if (datum == null)
            {
                _writeItem ??= new WriteResolver(_options).ResolveWriter(_schema);
                _writeItem(datum, _chunkWriter);
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
            if (_memoryChunk.Position >= _syncInterval)
            {
                WriteBuffer();
            }
        }

        private void WriteBuffer()
        {
            if (_blockCount > 0)
            {
                if (_compressedChunk == null)
                {
                    _writer.WriteDataBlock(_memoryChunk, _header.SyncData, _blockCount);
                }
                else
                {
                    _compressedChunk.SetLength(0);
                    _memoryChunk.TryGetBuffer(out var raw);
                    _codec.Compress(raw, _compressedChunk);
                    _writer.WriteDataBlock(_compressedChunk, _header.SyncData, _blockCount);
                }

                _blockCount = 0;
                _memoryChunk.SetLength(0);
            }
        }

        public void Dispose()
        {
            WriteBuffer();
            _memoryChunk.Dispose();
            _compressedChunk?.Dispose();
            _outStream.Flush();
            _outStream.Dispose();
        }
    }
}
