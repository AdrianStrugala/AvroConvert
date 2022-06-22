#region license
/**Copyright (c) 2021 Adrian Strugala
*
* Licensed under the CC BY-NC-SA 3.0 License(the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* https://creativecommons.org/licenses/by-nc-sa/3.0/
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*
* You are free to use or modify the code for personal usage.
* For commercial usage purchase the product at
*
* https://xabe.net/product/avroconvert/
*/
#endregion

using System.IO;
using SolTechnology.Avro.AvroObjectServices.FileHeader.Codec;
using SolTechnology.Avro.AvroObjectServices.Read;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.Features.DeserializeByLine.LineReaders;

namespace SolTechnology.Avro.Features.DeserializeByLine
{
    internal class BaseLineReader<T> : ILineReader<T>
    {
        private readonly Reader _reader;
        private readonly byte[] _syncDate;
        private readonly AbstractCodec _codec;
        private readonly TypeSchema _writeSchema;
        private readonly TypeSchema _readSchema;
        private ILineReader<T> _lineReaderInternal;

        internal BaseLineReader(Reader reader, byte[] syncDate, AbstractCodec codec, TypeSchema writeSchema, TypeSchema readSchema)
        {
            _reader = reader;
            _syncDate = syncDate;
            _codec = codec;
            _writeSchema = writeSchema;
            _readSchema = readSchema;

            if (_reader.IsReadToEnd())
            {
                return;
            }

            LoadNextDataBlock();
        }


        public bool HasNext()
        {
            var hasNext = _lineReaderInternal != null && _lineReaderInternal.HasNext();

            if (!hasNext)
            {
                hasNext = !_reader.IsReadToEnd();

                if (hasNext)
                {
                    LoadNextDataBlock();
                    return _lineReaderInternal.HasNext();
                }
            }

            return hasNext;
        }

        private void LoadNextDataBlock()
        {
            var resolver = new Resolver(_writeSchema, _readSchema);

            var itemsCount = _reader.ReadLong();

            var dataBlock = _reader.ReadDataBlock(_syncDate, _codec);
            var dataReader = new Reader(new MemoryStream(dataBlock));


            if (itemsCount > 1)
            {
                _lineReaderInternal = new BlockLineReader<T>(dataReader, resolver, itemsCount);
                return;
            }

            if (_writeSchema.Type == AvroType.Array)
            {
                _lineReaderInternal = new ListLineReader<T>(dataReader, new Resolver(((ArraySchema)_writeSchema).ItemSchema, _readSchema));
                return;
            }

            _lineReaderInternal = new RecordLineReader<T>(dataReader, resolver);
        }

        public T ReadNext()
        {
            return _lineReaderInternal.ReadNext();
        }

        public void Dispose()
        {
            _lineReaderInternal?.Dispose();
        }
    }
}
