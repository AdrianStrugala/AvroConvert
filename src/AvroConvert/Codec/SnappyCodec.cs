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
using System.Linq;

namespace SolTechnology.Avro.Codec
{
    public class SnappyCodec : AbstractCodec
    {
        public override string Name { get; } = CodecType.Snappy.ToString().ToLower();

        public override byte[] Compress(byte[] uncompressedData)
        {
            var compressedData = Snappy.SnappyCodec.Compress(uncompressedData);
            uint checksumUint = Crc32.Get(compressedData);
            byte[] checksumBytes = BitConverter.GetBytes(checksumUint);

            byte[] result = compressedData.Concat(checksumBytes).ToArray();
            return result;
        }

        public override byte[] Decompress(byte[] compressedData)
        {
            byte[] dataToDecompress = new byte[compressedData.Length - 4]; // last 4 bytes are CRC
            Array.Copy(compressedData, dataToDecompress, dataToDecompress.Length);

            return Snappy.SnappyCodec.Uncompress(dataToDecompress);
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }
}
