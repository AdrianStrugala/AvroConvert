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

namespace SolTechnology.Avro.Write
{
    internal class Encoder : IDisposable
    {
        internal delegate void WriteItem(object value, IWriter encoder);

        private readonly Schema.Schema _schema;
        private readonly AbstractCodec _codec;
        private readonly Stream _stream;
        private MemoryStream _blockStream;
        private readonly IWriter _encoder;
        private IWriter _blockEncoder;
        private readonly WriteItem _writer;
        private byte[] _syncData;
        private bool _isOpen;
        private bool _headerWritten;
        private int _blockCount;
        private readonly int _syncInterval;
        private readonly Metadata _metadata;


        internal Encoder(Schema.Schema schema, Stream outStream, CodecType codecType)
        {
            _codec = AbstractCodec.CreateCodec(codecType);
            _stream = outStream;
            _metadata = new Metadata();
            _schema = schema;
            _syncInterval = DataFileConstants.DefaultSyncInterval;

            _blockCount = 0;
            _encoder = new Writer(_stream);
            _blockStream = new MemoryStream();
            _blockEncoder = new Writer(_blockStream);

            _writer = Resolver.ResolveWriter(schema);

            _isOpen = true;
        }

        internal void Append(object datum)
        {
            AssertOpen();
            EnsureHeader();

            _writer(datum, _blockEncoder);

            _blockCount++;
            WriteIfBlockFull();
        }

        private void EnsureHeader()
        {
            if (!_headerWritten)
            {
                WriteHeader();
                _headerWritten = true;
            }
        }

        internal long Sync()
        {
            AssertOpen();
            WriteBlock();
            return _stream.Position;
        }

        private void WriteHeader()
        {
            _encoder.WriteFixed(DataFileConstants.AvroHeader);
            WriteMetaData();
            WriteSyncData();
        }

        private void AssertOpen()
        {
            if (!_isOpen) throw new AvroRuntimeException("Cannot complete operation: avro file/stream not open");
        }

        private void WriteMetaData()
        {
            // Add sync, code & schema to metadata
            GenerateSyncData();
            _metadata.Add(DataFileConstants.CodecMetadataKey, _codec.Name);
            _metadata.Add(DataFileConstants.SchemaMetadataKey, _schema.ToString());

            // write metadata 
            int size = _metadata.GetSize();
            _encoder.WriteInt(size);

            foreach (KeyValuePair<string, byte[]> metaPair in _metadata.GetValue())
            {
                _encoder.WriteString(metaPair.Key);
                _encoder.WriteBytes(metaPair.Value);
            }
            _encoder.WriteMapEnd();
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
                _encoder.WriteFixed(_syncData);

                // reset / re-init block
                _blockCount = 0;
                _blockStream = new MemoryStream();
                _blockEncoder = new Writer(_blockStream);
            }
        }

        private void WriteSyncData()
        {
            _encoder.WriteFixed(_syncData);
        }

        private void GenerateSyncData()
        {
            _syncData = new byte[16];

            Random random = new Random();
            random.NextBytes(_syncData);
        }

        public void Dispose()
        {
            EnsureHeader();
            Sync();
            _stream.Flush();
            _stream.Dispose();
            _isOpen = false;
        }
    }
}
