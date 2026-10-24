#region license
/**Copyright (c) 2019-2026 Adrian Strugała (SolTechnology)
*
* Licensed under the PolyForm Noncommercial License 1.0.0, the PolyForm Small Business License 1.0.0,
* or the SolTechnology Commercial Licence – pick the one that applies to you. See LICENSE.md:
*
* https://github.com/AdrianStrugala/AvroConvert/blob/master/LICENSE.md
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
*
* Required Notice: Copyright Adrian Strugała (https://soltechnology.dev)
*/
#endregion

using System;
using System.IO;
using ZstdSharp;

namespace SolTechnology.Avro.AvroObjectServices.FileHeader.Codec
{
    /// <summary><c>zstandard</c> codec from the Avro specification; one zstd frame per block.</summary>
    internal class ZstandardCodec : AbstractCodec
    {
        /// <summary>Same default level as the Java implementation.</summary>
        private const int Level = 3;

        internal override string Name { get; } = "zstandard";

        internal override byte[] Decompress(byte[] compressedData)
        {
            // Streaming decompression also accepts frames without a content-size header (e.g. written by zstd-jni streams).
            using var input = new MemoryStream(compressedData);
            using var zstd = new DecompressionStream(input);
            using var output = new MemoryStream(compressedData.Length * 3);
            zstd.CopyTo(output);
            return output.ToArray();
        }

        internal override void Compress(ReadOnlySpan<byte> data, Stream output)
        {
            using var compressor = new Compressor(Level);
            output.Write(compressor.Wrap(data));
        }
    }
}
