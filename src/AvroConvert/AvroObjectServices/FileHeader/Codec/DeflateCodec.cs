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

/** Modifications copyright(C) 2022 Adrian Strugala **/

using System;
using System.IO;
using System.IO.Compression;

namespace SolTechnology.Avro.AvroObjectServices.FileHeader.Codec
{
    internal class DeflateCodec : AbstractCodec
    {
        internal override string Name { get; } = CodecType.Deflate.ToString().ToLower();

        internal override void Compress(ReadOnlySpan<byte> data, Stream output)
        {
            using var deflate = new DeflateStream(output, CompressionMode.Compress, leaveOpen: true);
            deflate.Write(data);
        }

        internal override byte[] Decompress(byte[] compressedData)
        {
            using var input = new MemoryStream(compressedData);
            using var deflate = new DeflateStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream(compressedData.Length * 3);
            deflate.CopyTo(output);
            return output.ToArray();
        }
    }
}
