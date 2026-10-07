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
using System.IO;
using System.IO.Compression;

namespace SolTechnology.Avro.AvroObjectServices.FileHeader.Codec
{
    internal class GZipCodec : AbstractCodec
    {
        internal override string Name { get; } = "gzip";

        internal override byte[] Decompress(byte[] compressedData)
        {
            using var input = new MemoryStream(compressedData);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream(compressedData.Length * 3);
            gzip.CopyTo(output);
            return output.ToArray();
        }

        internal override void Compress(ReadOnlySpan<byte> data, Stream output)
        {
            using var gzip = new GZipStream(output, CompressionMode.Compress, leaveOpen: true);
            gzip.Write(data);
        }
    }
}
