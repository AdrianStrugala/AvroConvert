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

    public class Decoder : IDisposable
    {
        private readonly Resolver _resolver;
        private readonly IReader _reader;
        private IReader _datumReader;
        private readonly Header _header;
        private readonly Codec _codec;
        private DataBlock _currentBlock;
        private long _blockRemaining;
        private long _blockSize;
        private bool _availableBlock;
        private readonly byte[] _syncBuffer;
        private readonly Stream _stream;
        private static Schema _readerSchema;


        public static Decoder OpenReader(string filePath)
        {
            return OpenReader(new FileStream(filePath, FileMode.Open));
        }

        public static Decoder OpenReader(Stream inStream, string schema = null)
        {
            if (schema != null)
            {
                _readerSchema = Schema.Parse(schema);
            }
         
            return OpenReader(inStream);
        }


        private static Decoder OpenReader(Stream inStream)
        {
            if (!inStream.CanSeek)
                throw new AvroRuntimeException("Not a valid input stream - must be seekable!");

            return new Decoder(inStream);         // (not supporting 1.2 or below, format)           
        }

        Decoder(Stream stream)
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
                throw new InvalidAvroObjectException("Cannot read length of Avro Header");
            }
            if (!firstBytes.SequenceEqual(DataFileConstants.AvroHeader))
                throw new InvalidAvroObjectException("Cannot read Avro Header");

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
            _resolver = new Resolver(_header.Schema, _readerSchema ?? _header.Schema);
            _codec = Codec.CreateCodecFromString(GetMetaString(DataFileConstants.CodecMetadataKey));
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
                throw new AvroRuntimeException($"Error fetching meta data for key: {key}", e);
            }
        }


        public IEnumerable<object> GetEntries()
        {
            var result = new List<object>();
            long remainingBlocks = GetRemainingBlocksCount();

            for (int i = 0; i < remainingBlocks; i++)
            {
                result.Add(_resolver.Resolve(_datumReader));
            }

            return result;
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
                throw new AvroRuntimeException($"Error fetching next object from block: {e}");
            }
        }

        public void Dispose()
        {
            _stream.Dispose();
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
                throw new AvroRuntimeException($"Error ascertaining if data has next block: {e}");
            }
        }
    }
}