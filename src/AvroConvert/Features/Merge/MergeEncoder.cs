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
using SolTechnology.Avro.AvroObjectServices.Write;
using SolTechnology.Avro.Infrastructure.Exceptions;

namespace SolTechnology.Avro.Features.Merge
{
    /// <summary>Writes an object container file from data blocks of other files, copied as they are (no re-encoding).</summary>
    internal class MergeEncoder : IDisposable
    {
        private readonly Stream _stream;
        private readonly Writer _writer;
        private readonly Header _header;

        internal MergeEncoder(Stream outStream)
        {
            _stream = outStream;
            _writer = new Writer(_stream);
            _header = new Header();
        }

        internal void WriteHeader(string schema, CodecType codecType)
        {
            GenerateSyncData();
            _header.AddMetadata(DataFileConstants.CodecMetadataKey, AbstractCodec.CreateCodec(codecType).Name);
            _header.AddMetadata(DataFileConstants.SchemaMetadataKey, schema);

            _writer.WriteHeader(_header);
            _writer.Flush();
        }

        /// <summary>Each block keeps its entries and their count, so the result has the layout of the files it comes from.</summary>
        internal void WriteData(List<DataBlock> dataBlocks)
        {
            foreach (var dataBlock in dataBlocks)
            {
                _writer.WriteDataBlock(dataBlock.Data, _header.SyncData, checked((int)dataBlock.ItemsCount));
            }

            _writer.Flush();
        }

        private void GenerateSyncData()
        {
            _header.SyncData = new byte[16];
            System.Security.Cryptography.RandomNumberGenerator.Fill(_header.SyncData);
        }

        public void Dispose()
        {
            _writer.Dispose();
            _stream.Flush();
            _stream.Dispose();
        }
    }
}
