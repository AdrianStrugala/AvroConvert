#region license
/**Copyright (c) 2021 Adrian Strugala
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* https://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SolTechnology.Avro.AvroObjectServices.FileHeader;
using SolTechnology.Avro.AvroObjectServices.FileHeader.Codec;
using SolTechnology.Avro.AvroObjectServices.Schema.Abstract;
using SolTechnology.Avro.AvroObjectServices.Write;
using SolTechnology.Avro.Infrastructure.Exceptions;

namespace SolTechnology.Avro.Features.Merge
{
    internal class MergeEncoder : IDisposable
    {
        private readonly AbstractCodec _codec;
        private readonly Stream _stream;
        private MemoryStream _tempBuffer;
        private readonly Writer _writer;
        private readonly Writer _tempWriter;
        private bool _isOpen;
        private int _blockCount;
        private readonly int _syncInterval;
        private readonly Header _header;


        internal MergeEncoder(Stream outStream)
        {
            _codec = new NullCodec();
            _stream = outStream;

            _syncInterval = DataFileConstants.DefaultSyncInterval;

            _blockCount = 0;
            _writer = new Writer(_stream);

            _tempBuffer = new MemoryStream();
            _tempWriter = new Writer(_tempBuffer);

            _isOpen = true;
            _header = new Header();
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

            _writer.WriteHeader(_header);
        }

        internal void WriteData(IEnumerable<byte[]> data)
        {
            var dataAsList = data.ToList();
            _tempWriter.WriteArrayStart();
            _tempWriter.SetItemCount(dataAsList.Count);

            foreach (var bytes in dataAsList)
            {
                _tempWriter.WriteBytesRaw(bytes);
            }

            _tempWriter.WriteArrayEnd();

            _blockCount++;
            WriteIfBlockFull();
        }

        private void AssertOpen()
        {
            if (!_isOpen) throw new AvroRuntimeException("Cannot complete operation: avro file/stream not open");
        }

        private void WriteIfBlockFull()
        {
            if (_tempBuffer.Position >= _syncInterval)
                WriteBlock();
        }

        private void WriteBlock()
        {
            if (_blockCount > 0)
            {
                byte[] dataToWrite = _tempBuffer.ToArray();

                _writer.WriteDataBlock(_codec.Compress(dataToWrite), _header.SyncData, _blockCount);

                // reset block buffer
                _blockCount = 0;
                _tempBuffer = new MemoryStream();
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
