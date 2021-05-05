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
using SolTechnology.Avro.Exceptions;
using SolTechnology.Avro.FileHeader;
using SolTechnology.Avro.FileHeader.Codec;
using SolTechnology.Avro.Schema.Abstract;
using SolTechnology.Avro.Write;

namespace SolTechnology.Avro.Merge
{
    internal class MergeEncoder : IDisposable
    {
        private readonly AbstractCodec _codec;
        private readonly Stream _stream;
        private MemoryStream _blockStream;
        private readonly Writer _encoder;
        private IWriter _blockEncoder;
        private readonly Encoder.WriteItem _writer;
        private bool _isOpen;
        private int _blockCount;
        private readonly int _syncInterval;
        private readonly Header _header;


        internal MergeEncoder(TypeSchema schema, Stream outStream)
        {
            _codec = new NullCodec();
            _stream = outStream;

            _syncInterval = DataFileConstants.DefaultSyncInterval;

            _blockCount = 0;
            _encoder = new Writer(_stream);
            _blockStream = new MemoryStream();
            _blockEncoder = new Writer(_blockStream);

            _writer = Resolver.ResolveWriter(schema);

            _isOpen = true;
            _header = new Header();
        }

        internal void Append(object datum)
        {
            AssertOpen();

            _writer(datum, _blockEncoder);

            _blockCount++;
            WriteIfBlockFull();
        }
        internal long Sync()
        {
            AssertOpen();
            WriteBlock();
            return _stream.Position;
        }

        internal void WriteHeader(string schema, CodecType codecType)
        {
            GenerateSyncData();
            _header.AddMetadata(DataFileConstants.CodecMetadataKey, AbstractCodec.CreateCodec(codecType).Name);
            _header.AddMetadata(DataFileConstants.SchemaMetadataKey, schema);

            _encoder.WriteHeader(_header);
        }

        private void AssertOpen()
        {
            if (!_isOpen) throw new AvroRuntimeException("Cannot complete operation: avro file/stream not open");
        }

        private void WriteIfBlockFull()
        {
            if (_blockStream.Position >= _syncInterval)
                WriteBlock();
        }

        private void WriteBlock()
        {
            if (_blockCount > 0)
            {
                byte[] dataToWrite = _blockStream.ToArray();

                // write count 
                _encoder.WriteLong(_blockCount);

                // write data 
                _encoder.WriteBytes(_codec.Compress(dataToWrite));

                // write sync marker 
                _encoder.WriteFixed(_header.SyncData);

                // reset / re-init block
                _blockCount = 0;
                _blockStream = new MemoryStream();
                _blockEncoder = new Writer(_blockStream);
            }
        }

        private void GenerateSyncData()
        {
            _header.SyncData = new byte[16];

            Random random = new Random();
            random.NextBytes(_header.SyncData);
        }

        public void Dispose()
        {
            Sync();
            _stream.Flush();
            _stream.Dispose();
            _isOpen = false;
        }
    }
}
