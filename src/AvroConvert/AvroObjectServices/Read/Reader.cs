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

/** Modifications copyright(C) 2020 Adrian Strugala **/

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using SolTechnology.Avro.Infrastructure.Exceptions;

namespace SolTechnology.Avro.AvroObjectServices.Read
{
    /// <summary>
    /// IDecoder for Avro binary format. Reads either from a <see cref="Stream"/> or, on the hot path, directly from an
    /// in-memory block (<see cref="Reader(byte[], int, int)"/>) without virtual per-byte calls or intermediate copies.
    /// </summary>
    internal partial class Reader : IReader
    {
        private readonly Stream _stream;
        private readonly byte[] _buffer;
        private int _pos;
        private readonly int _end;

        internal Reader(Stream stream)
        {
            this._stream = stream;
        }

        internal Reader(byte[] data) : this(data, 0, data.Length)
        {
        }

        internal Reader(byte[] data, int offset, int count)
        {
            _buffer = data;
            _pos = offset;
            _end = offset + count;
        }

        /// <summary>
        /// null is written as zero bytes
        /// </summary>
        public void ReadNull()
        {
        }

        public bool IsReadToEnd()
        {
            return _buffer != null ? _pos >= _end : _stream.Position == _stream.Length;
        }

        /// <summary>
        /// a boolean is written as a single byte 
        /// whose value is either 0 (false) or 1 (true).
        /// </summary>
        public bool ReadBoolean()
        {
            byte b = Read();
            if (b == 0) return false;
            if (b == 1) return true;
            throw new AvroException("Not a boolean value in the stream: " + b);
        }

        /// <summary>
        /// int and long values are written using variable-length, zig-zag coding.
        /// </summary>
        public int ReadInt()
        {
            return (int)ReadLong();
        }

        /// <summary>
        /// int and long values are written using variable-length, zig-zag coding.
        /// </summary>
        public long ReadLong()
        {
            ulong n;
            if (_buffer != null)
            {
                var buffer = _buffer;
                int pos = _pos;
                if (pos >= _end) throw new EndOfStreamException();

                byte b = buffer[pos++];
                n = b & 0x7FUL;
                int shift = 7;
                while ((b & 0x80) != 0)
                {
                    if (pos >= _end) throw new EndOfStreamException();
                    b = buffer[pos++];
                    n |= (b & 0x7FUL) << shift;
                    shift += 7;
                }
                _pos = pos;
            }
            else
            {
                byte b = Read();
                n = b & 0x7FUL;
                int shift = 7;
                while ((b & 0x80) != 0)
                {
                    b = Read();
                    n |= (b & 0x7FUL) << shift;
                    shift += 7;
                }
            }

            long value = (long)n;
            return (-(value & 0x01L)) ^ ((value >> 1) & 0x7fffffffffffffffL);
        }

        /// <summary>
        /// A float is written as 4 bytes, little-endian (Java floatToIntBits).
        /// </summary>
        public float ReadFloat()
        {
            Span<byte> buffer = stackalloc byte[4];
            Read(buffer);
            return BinaryPrimitives.ReadSingleLittleEndian(buffer);
        }

        /// <summary>
        /// A double is written as 8 bytes, little-endian (Java doubleToLongBits).
        /// </summary>
        public double ReadDouble()
        {
            Span<byte> buffer = stackalloc byte[8];
            Read(buffer);
            return BinaryPrimitives.ReadDoubleLittleEndian(buffer);
        }

        /// <summary>
        /// Bytes are encoded as a long followed by that many bytes of data. 
        /// </summary>
        public byte[] ReadBytes()
        {
            return Read(ReadLong());
        }

        public string ReadString()
        {
            int length = ReadInt();
            if (_buffer != null)
            {
                EnsureAvailable(length);
                var result = Encoding.UTF8.GetString(_buffer, _pos, length);
                _pos += length;
                return result;
            }

            if (length <= 512)
            {
                Span<byte> buffer = stackalloc byte[length];
                Read(buffer);
                return Encoding.UTF8.GetString(buffer);
            }
            else
            {
                byte[] bufferArray = ArrayPool<byte>.Shared.Rent(length);
                Span<byte> buffer = bufferArray.AsSpan()[..length];
                Read(buffer);
                string result = Encoding.UTF8.GetString(buffer);
                ArrayPool<byte>.Shared.Return(bufferArray);
                return result;
            }
        }

        public int ReadEnum()
        {
            return ReadInt();
        }

        public long ReadArrayStart()
        {
            return DoReadItemCount();
        }

        public long ReadArrayNext()
        {
            return DoReadItemCount();
        }

        public long ReadMapStart()
        {
            return DoReadItemCount();
        }

        public long ReadMapNext()
        {
            return DoReadItemCount();
        }

        public int ReadUnionIndex()
        {
            return ReadInt();
        }

        public void ReadFixed(Span<byte> buffer)
        {
            Read(buffer);
        }

        public void ReadFixed(byte[] buffer)
        {
            Read(buffer.AsSpan());
        }

        public void ReadFixed(byte[] buffer, int start, int length)
        {
            Read(buffer.AsSpan(start, length));
        }

        public void SkipNull()
        {
            ReadNull();
        }

        public void SkipBoolean()
        {
            ReadBoolean();
        }

        public void SkipInt()
        {
            ReadInt();
        }

        public void SkipLong()
        {
            ReadLong();
        }

        public void SkipFloat()
        {
            Skip(4);
        }

        public void SkipDouble()
        {
            Skip(8);
        }

        public void SkipBytes()
        {
            Skip(ReadLong());
        }

        public void SkipString()
        {
            SkipBytes();
        }

        public void SkipEnum()
        {
            ReadLong();
        }

        public void SkipUnionIndex()
        {
            ReadLong();
        }

        public void SkipFixed(int len)
        {
            Skip(len);
        }

        // Read p bytes into a new byte buffer
        private byte[] Read(long p)
        {
            byte[] buffer = new byte[p];
            Read(buffer.AsSpan());
            return buffer;
        }

        private byte Read()
        {
            if (_buffer != null)
            {
                if (_pos >= _end) throw new EndOfStreamException();
                return _buffer[_pos++];
            }

            int n = _stream.ReadByte();
            if (n >= 0) return (byte)n;
            throw new EndOfStreamException();
        }

        private void Read(Span<byte> buffer)
        {
            if (_buffer != null)
            {
                EnsureAvailable(buffer.Length);
                _buffer.AsSpan(_pos, buffer.Length).CopyTo(buffer);
                _pos += buffer.Length;
                return;
            }

            int length = buffer.Length;
            int offset = 0;

            while (length > 0)
            {
                int bytesWritten = _stream.Read(buffer.Slice(offset));
                if (bytesWritten <= 0) throw new EndOfStreamException();
                offset += bytesWritten;
                length -= bytesWritten;
            }
        }

        private void EnsureAvailable(int count)
        {
            if (count < 0 || _pos + count > _end) throw new EndOfStreamException();
        }

        private long DoReadItemCount()
        {
            long result = ReadLong();
            if (result < 0)
            {
                ReadLong(); // Consume byte-count if present
                result = -result;
            }
            return result;
        }

        private void Skip(long p)
        {
            if (_buffer != null)
            {
                EnsureAvailable((int)p);
                _pos += (int)p;
                return;
            }

            _stream.Seek(p, SeekOrigin.Current);
        }
    }
}
