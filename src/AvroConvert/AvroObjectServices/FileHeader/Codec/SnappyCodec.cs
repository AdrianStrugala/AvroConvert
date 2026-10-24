#region license
/**Copyright (c) 2020 Adrian Strugała
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
using System.Buffers.Binary;
using System.IO;
using IronSnappy;

namespace SolTechnology.Avro.AvroObjectServices.FileHeader.Codec
{
    internal class SnappyCodec : AbstractCodec
    {
        internal override string Name { get; } = CodecType.Snappy.ToString().ToLower();

        internal override void Compress(ReadOnlySpan<byte> data, Stream output)
        {
            var compressed = Snappy.Encode(data);
            output.Write(compressed);

            // Avro spec: 4-byte big-endian CRC-32 of the *uncompressed* data appended to the block.
            Span<byte> crc = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(crc, Crc32.Get(data));
            output.Write(crc);
        }

        internal override byte[] Decompress(byte[] compressedData)
        {
            // Trailing CRC is not validated: files written by AvroConvert 3.x carry a non-spec checksum.
            return Snappy.Decode(compressedData.AsSpan(0, compressedData.Length - 4));
        }
    }
}
