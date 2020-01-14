using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SolTechnology.Avro.Codec;
using SolTechnology.Avro.Constants;
using SolTechnology.Avro.Exceptions;
using SolTechnology.Avro.Helpers;

namespace SolTechnology.Avro.Read
{
    public class Decoder : IDisposable
    {
        private readonly Resolver _resolver;
        private readonly IReader _reader;
        private IReader _datumReader;
        private readonly Header _header;
        private readonly AbstractCodec _codec;
        private byte[] _currentBlock;
        private long _blockRemaining;
        private long _blockSize;
        private bool _availableBlock;
        private readonly byte[] _syncBuffer;
        private readonly Stream _stream;
        private static Schema.Schema _readerSchema;


        public static Decoder OpenReader(string filePath)
        {
            return OpenReader(new FileStream(filePath, FileMode.Open));
        }

        public static Decoder OpenReader(Stream inStream, string schema)
        {
            if (schema != null)
            {
                _readerSchema = Schema.Schema.Parse(schema);
            }

            return OpenReader(inStream);
        }


        public static Decoder OpenReader(Stream inStream)
        {
            if (!inStream.CanSeek)
                throw new AvroRuntimeException("Not a valid input stream - must be seekable!");

            return new Decoder(inStream);         // (not supporting 1.2 or below, format)           
        }

        private Decoder(Stream stream)
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
            _header.Schema = Schema.Schema.Parse(GetMetaString(DataFileConstants.SchemaMetadataKey));
            _resolver = new Resolver(_header.Schema, _readerSchema ?? _header.Schema);
            _codec = AbstractCodec.CreateCodecFromString(GetMetaString(DataFileConstants.CodecMetadataKey));
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


        public object Read()
        {
            long remainingBlocks = GetRemainingBlocksCount();
            var result = _resolver.Resolve(_datumReader, remainingBlocks);

            return result;
        }

        public long GetRemainingBlocksCount()
        {
            if (_blockRemaining == 0)
            {
                if (HasNextBlock())
                {
                    _currentBlock = NextRawBlock();
                    _currentBlock = _codec.Decompress(_currentBlock);
                    _datumReader = new Reader(new MemoryStream(_currentBlock));
                }
            }

            return _blockRemaining;
        }

        public void Dispose()
        {
            _stream.Dispose();
        }

        private byte[] NextRawBlock()
        {
            if (!HasNextBlock())
                throw new AvroRuntimeException("No data remaining in block!");

            var dataBlock = new byte[_blockSize];

            _reader.ReadFixed(dataBlock, 0, (int)_blockSize);
            _reader.ReadFixed(_syncBuffer);

            if (!_syncBuffer.SequenceEqual(_header.SyncData))
                throw new AvroRuntimeException("Invalid sync!");

            _availableBlock = false;
            return dataBlock;
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