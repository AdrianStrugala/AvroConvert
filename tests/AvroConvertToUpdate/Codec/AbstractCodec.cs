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

using System;
using System.Linq;

namespace SolTechnology.PerformanceBenchmark.AvroConvertToUpdate.Codec
{
    internal abstract class AbstractCodec
    {
        internal abstract string Name { get; }

        internal abstract byte[] Decompress(byte[] compressedData);

        internal abstract byte[] Compress(byte[] uncompressedData);

        internal static AbstractCodec CreateCodec(CodecType codecType)
        {
            switch (codecType)
            {
                case CodecType.Deflate:
                    return new DeflateCodec();
                case CodecType.Snappy:
                    return new SnappyCodec();
                case CodecType.GZip:
                    return new GZipCodec();
                case CodecType.Brotli:
                    return new BrotliCodec();
                default:
                    return new NullCodec();
            }
        }

        internal static AbstractCodec CreateCodecFromString(string codecName)
        {
            Type codecType = typeof(AbstractCodec).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(AbstractCodec)) && !t.IsAbstract)
                                              .First(t => t.Name.ToLower().Contains(codecName));

            if (codecType == null)
            {
                return new NullCodec();
            }

            return (AbstractCodec)Activator.CreateInstance(codecType);
        }
    }
}