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
            var dataReader = new Reader(dataBlock);


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
