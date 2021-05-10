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

using System.Linq;
using SolTechnology.Avro.Exceptions;
using SolTechnology.Avro.FileHeader;

namespace SolTechnology.Avro.Read
{
    internal partial class Reader : IReader
    {
        internal Header ReadHeader()
        {
            var header = new Header();

            long len = ReadMapStart();
            if (len > 0)
            {
                do
                {
                    for (long i = 0; i < len; i++)
                    {
                        string key = ReadString();
                        byte[] val = ReadBytes();
                        header.AddMetadata(key, val);
                    }
                } while ((len = ReadMapNext()) != 0);
            }

            return header;
        }

        internal byte[] ReadDataBlock()
        {
            var blockSize = ReadLong();

            var dataBlock = new byte[blockSize];
            ReadFixed(dataBlock, 0, (int)blockSize);

            return dataBlock;
        }

        internal void ReadAndValidateSync(byte[] expectedSync)
        {
            var syncBuffer = new byte[DataFileConstants.SyncSize];
            ReadFixed(syncBuffer);

            if (!syncBuffer.SequenceEqual(expectedSync))
                throw new AvroRuntimeException("Invalid sync!");
        }
    }
}
