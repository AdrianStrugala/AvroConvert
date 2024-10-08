﻿#region license
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

using System.IO;
using BrotliSharpLib;

namespace SolTechnology.Avro.AvroObjectServices.FileHeader.Codec
{
    internal class BrotliCodec : AbstractCodec
    {
        internal override string Name { get; } = CodecType.Brotli.ToString().ToLower();
        internal override byte[] Decompress(byte[] compressedData)
        {
            return Brotli.DecompressBuffer(compressedData, 0, compressedData.Length);
        }

        internal override MemoryStream Compress(MemoryStream toCompress)
        {
            var toCompressBytes = toCompress.ToArray();
            return new MemoryStream(Brotli.CompressBuffer(toCompressBytes, 0, toCompressBytes.Length, 4));
        }
    }
}
