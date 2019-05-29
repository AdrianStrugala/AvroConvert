namespace AvroConvert.Read
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Constants;
    using Exceptions;
    using Helpers;
    using Schema;
    using Codec = Helpers.Codec.Codec;

    public class Decoder
    {
        public delegate DataReader CreateDatumReader(Schema writerSchema, Schema readerSchema);

        private DataReader _dataReader;
        private IReader _reader, _datumReader;
        private Header _header;
        private Codec _codec;
        private DataBlock _currentBlock;
        private long _blockRemaining;
        private long _blockSize;
        private bool _availableBlock;
        private byte[] _syncBuffer;
        private long _blockStart;
        private Stream _stream;
        private readonly Schema _readerSchema;
        private readonly CreateDatumReader _datumReaderFactory;


        public static Decoder OpenReader(string filePath)
        {
            return OpenReader(new FileStream(filePath, FileMode.Open), CreateDefaultReader);
        }

        public static Decoder OpenReader(Stream inStream)
        {
            return OpenReader(inStream, CreateDefaultReader);
        }


        private static Decoder OpenReader(Stream inStream, CreateDatumReader datumReaderFactory)
        {
            if (!inStream.CanSeek)
                throw new AvroRuntimeException("Not a valid input stream - must be seekable!");

            return new Decoder(inStream, datumReaderFactory);         // (not supporting 1.2 or below, format)           
        }

        Decoder(Stream stream, CreateDatumReader datumReaderFactory)
        {
            _datumReaderFactory = datumReaderFactory;
            Init(stream);
            BlockFinished();
        }

        public Header GetHeader()
        {
            return _header;
        }

        public Schema GetSchema()
        {
            return _header.Schema;
        }

        public ICollection<string> GetMetaKeys()
        {
            return _header.MetaData.Keys;
        }

        public byte[] GetMeta(string key)
        {
            try
            {
                return _header.MetaData[key];
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }

        public long GetMetaLong(string key)
        {
            return long.Parse(GetMetaString(key));
        }

        public string GetMetaString(string key)
        {
            byte[] value = GetMeta(key);
            if (value == null)
            {
                return null;
            }
            try
            {
                return System.Text.Encoding.UTF8.GetString(value);
            }
            catch (Exception e)
            {
                throw new AvroRuntimeException(string.Format("Error fetching meta data for key: {0}", key), e);
            }
        }

        public void Seek(long position)
        {
            _stream.Position = position;
            _reader = new Reader(_stream);
            _datumReader = null;
            _blockRemaining = 0;
            _blockStart = position;
        }

        public void Sync(long position)
        {
            Seek(position);
            // work around an issue where 1.5.4 C stored sync in metadata
            if ((position == 0) && (GetMeta(DataFileConstants.SyncMetadataKey) != null))
            {
                Init(_stream); // re-init to skip header
                return;
            }

            try
            {
                bool done = false;

                do // read until sync mark matched
                {
                    _reader.ReadFixed(_syncBuffer);
                    if (Enumerable.SequenceEqual(_syncBuffer, _header.SyncData))
                        done = true;
                    else
                        _stream.Position = _stream.Position - (DataFileConstants.SyncSize - 1);
                } while (!done);
            }
            catch (Exception) { } // could not find .. default to EOF

            _blockStart = _stream.Position;
        }

        public bool PastSync(long position)
        {
            return ((_blockStart >= position + DataFileConstants.SyncSize) || (_blockStart >= _stream.Length));
        }

        public long PreviousSync()
        {
            return _blockStart;
        }

        public long Tell()
        {
            return _stream.Position;
        }

        public IEnumerable<object> GetEntries()
        {
            var result = new List<object>();
            long remainingBlocks = GetRemainingBlocksCount();

            for (int i = 0; i < remainingBlocks; i++)
            {
                result.Add(Next());
            }

            return result;
        }

        public bool HasNext()
        {
            try
            {
                if (_blockRemaining == 0)
                {
                    if (HasNextBlock())
                    {
                        _currentBlock = NextRawBlock(_currentBlock);
                        _currentBlock.Data = _codec.Decompress(_currentBlock.Data);
                        _datumReader = new Reader(_currentBlock.GetDataAsStream());
                    }
                }
                return _blockRemaining != 0;
            }
            catch (Exception e)
            {
                throw new AvroRuntimeException(string.Format("Error fetching next object from block: {0}", e));
            }
        }

        public long GetRemainingBlocksCount()
        {
            try
            {
                if (_blockRemaining == 0)
                {
                    if (HasNextBlock())
                    {
                        _currentBlock = NextRawBlock(_currentBlock);
                        _currentBlock.Data = _codec.Decompress(_currentBlock.Data);
                        _datumReader = new Reader(_currentBlock.GetDataAsStream());
                    }
                }

                return _blockRemaining;
            }
            catch (Exception e)
            {
                throw new AvroRuntimeException(string.Format("Error fetching next object from block: {0}", e));
            }
        }

        public void Reset()
        {
            Init(_stream);
        }

        public void Dispose()
        {
            _stream.Dispose();
        }

        private void Init(Stream stream)
        {
            _stream = stream;
            _header = new Header();
            _reader = new Reader(stream);
            _syncBuffer = new byte[DataFileConstants.SyncSize];

            // validate header 
            byte[] firstBytes = new byte[DataFileConstants.AvroHeader.Length];
            try
            {
                _reader.ReadFixed(firstBytes);
            }
            catch (Exception)
            {
                throw new InvalidAvroHeaderException();
            }
            if (!firstBytes.SequenceEqual(DataFileConstants.AvroHeader))
                throw new InvalidAvroHeaderException();

            // read meta data 
            long len = _reader.ReadMapStart();
            if (len > 0)
            {
                do
                {
                    for (long i = 0; i < len; i++)
                    {
                        string key = _reader.ReadString();
                        byte[] val = _reader.ReadBytes();
                        _header.MetaData.Add(key, val);
                    }
                } while ((len = _reader.ReadMapNext()) != 0);
            }

            // read in sync data 
            _reader.ReadFixed(_header.SyncData);

            // parse schema and set codec 
            _header.Schema = Schema.Parse(GetMetaString(DataFileConstants.SchemaMetadataKey));
            _dataReader = _datumReaderFactory(_header.Schema, _readerSchema ?? _header.Schema);
            _codec = ResolveCodec();
        }

        private static DataReader CreateDefaultReader(Schema writerSchema, Schema readerSchema)
        {
            var reader = new DataReader(writerSchema, readerSchema);

            return reader;
        }

        private Codec ResolveCodec()
        {
            return Codec.CreateCodecFromString(GetMetaString(DataFileConstants.CodecMetadataKey));
        }

        private object Next()
        {
            try
            {
                var result = _dataReader.Read(_datumReader);
                if (--_blockRemaining == 0)
                {
                    BlockFinished();
                }
                return result;
            }
            catch (Exception e)
            {
                throw new AvroRuntimeException(string.Format("Error fetching next object from block: {0}", e));
            }
        }

        private void BlockFinished()
        {
            _blockStart = _stream.Position;
        }

        private DataBlock NextRawBlock(DataBlock reuse)
        {
            if (!HasNextBlock())
                throw new AvroRuntimeException("No data remaining in block!");

            if (reuse == null || reuse.Data.Length < _blockSize)
            {
                reuse = new DataBlock(_blockRemaining, _blockSize);
            }
            else
            {
                reuse.NumberOfEntries = _blockRemaining;
                reuse.BlockSize = _blockSize;
            }

            _reader.ReadFixed(reuse.Data, 0, (int)reuse.BlockSize);
            _reader.ReadFixed(_syncBuffer);

            if (!Enumerable.SequenceEqual(_syncBuffer, _header.SyncData))
                throw new AvroRuntimeException("Invalid sync!");

            _availableBlock = false;
            return reuse;
        }


        private bool HasNextBlock()
        {
            try
            {
                // block currently being read 
                if (_availableBlock)
                    return true;

                // check to ensure still data to read 
                if (_stream.Position == _stream.Length)
                    return false;

                _blockRemaining = _reader.ReadLong();      // read block count
                _blockSize = _reader.ReadLong();           // read block size
                if (_blockSize > int.MaxValue || _blockSize < 0)
                {
                    throw new AvroRuntimeException("Block size invalid or too large for this " +
                                                   "implementation: " + _blockSize);
                }
                _availableBlock = true;
                return true;
            }
            catch (Exception e)
            {
                throw new AvroRuntimeException(string.Format("Error ascertaining if data has next block: {0}", e));
            }
        }

    }
}