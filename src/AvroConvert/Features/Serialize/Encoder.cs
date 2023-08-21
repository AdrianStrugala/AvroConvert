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
using SolTechnology.Avro.Infrastructure.Exceptions;

namespace SolTechnology.Avro.Features.Serialize
{
    internal class Encoder : IDisposable
    {
        internal delegate void WriteItem(object value, IWriter encoder);

        private readonly AbstractCodec _codec;
        private readonly Stream _outStream;
        private readonly Writer _writer;

        private MemoryStream _memoryChunk;
        private Writer _chunkWriter;

        private readonly WriteItem _writeItem;
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
            _chunkWriter = new Writer(_memoryChunk);

            GenerateSyncData();
            _header.AddMetadata(DataFileConstants.CodecMetadataKey, _codec.Name);
            _header.AddMetadata(DataFileConstants.SchemaMetadataKey, schema.ToString());

            _writeItem = WriteResolver.ResolveWriter(schema, options);

            _writer.WriteHeader(_header);
        }
        private void GenerateSyncData()
        {
            _header.SyncData = new byte[16];

            Random random = new Random();
            random.NextBytes(_header.SyncData);
        }

        internal void Append(object datum)
        {
            _writeItem(datum, _chunkWriter);

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
                _writer.WriteDataBlock(_codec.Compress(_memoryChunk), _header.SyncData, _blockCount);

                // reset memory buffer
                _blockCount = 0;
                _memoryChunk = new MemoryStream();
                _chunkWriter = new Writer(_memoryChunk);
            }
        }

        public void Dispose()
        {
            WriteBuffer();
            _memoryChunk.Flush();
            _memoryChunk.Dispose();
            _outStream.Flush();
            _outStream.Dispose();
        }
    }
}
