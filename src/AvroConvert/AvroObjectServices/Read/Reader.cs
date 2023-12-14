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
using System.IO;
using SolTechnology.Avro.Infrastructure.Exceptions;

namespace SolTechnology.Avro.AvroObjectServices.Read
{
    /// <summary>
    /// IDecoder for Avro binary format
    /// </summary>
    internal partial class Reader : IReader
    {
        private readonly Stream _stream;

        internal Reader(Stream stream)
        {
            this._stream = stream;
        }

        /// <summary>
        /// null is written as zero bytes
        /// </summary>
        public void ReadNull()
        {
        }

        public bool IsReadToEnd()
        {
            return this._stream.Position == this._stream.Length;
        }

        /// <summary>
        /// a boolean is written as a single byte 
        /// whose value is either 0 (false) or 1 (true).
        /// </summary>
        /// <returns></returns>
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
        /// <param name="?"></param>
        /// <returns></returns>
        public int ReadInt()
        {
            return (int)ReadLong();
        }
        /// <summary>
        /// int and long values are written using variable-length, zig-zag coding.
        /// </summary>
        /// <param name="?"></param>
        /// <returns></returns>
        public long ReadLong()
        {
            byte b = Read();
            ulong n = b & 0x7FUL;
            int shift = 7;
            while ((b & 0x80) != 0)
            {
                b = Read();
                n |= (b & 0x7FUL) << shift;
                shift += 7;
            }
            long value = (long)n;
            return (-(value & 0x01L)) ^ ((value >> 1) & 0x7fffffffffffffffL);
        }

        /// <summary>
        /// A float is written as 4 bytes.
        /// The float is converted into a 32-bit integer using a method equivalent to
        /// Java's floatToIntBits and then encoded in little-endian format.
        /// </summary>
        /// <returns></returns>
        public float ReadFloat()
        {
            byte[] buffer = Read(4);

            if (!BitConverter.IsLittleEndian)
                Array.Reverse(buffer);

            return BitConverter.ToSingle(buffer, 0);

            //int bits = (Stream.ReadByte() & 0xff |
            //(Stream.ReadByte()) & 0xff << 8 |
            //(Stream.ReadByte()) & 0xff << 16 |
            //(Stream.ReadByte()) & 0xff << 24);
            //return intBitsToFloat(bits);
        }

        /// <summary>
        /// A double is written as 8 bytes.
        /// The double is converted into a 64-bit integer using a method equivalent to
        /// Java's doubleToLongBits and then encoded in little-endian format.
        /// </summary>
        /// <param name="?"></param>
        /// <returns></returns>
        public double ReadDouble()
        {
            long bits = (_stream.ReadByte() & 0xffL) |
              (_stream.ReadByte() & 0xffL) << 8 |
              (_stream.ReadByte() & 0xffL) << 16 |
              (_stream.ReadByte() & 0xffL) << 24 |
              (_stream.ReadByte() & 0xffL) << 32 |
              (_stream.ReadByte() & 0xffL) << 40 |
              (_stream.ReadByte() & 0xffL) << 48 |
              (_stream.ReadByte() & 0xffL) << 56;
            return BitConverter.Int64BitsToDouble(bits);
        }

        /// <summary>
        /// Bytes are encoded as a long followed by that many bytes of data. 
        /// </summary>
        /// <returns></returns>
        public byte[] ReadBytes()
        {
            return Read(ReadLong());
        }

        public string ReadString()
        {
            int length = ReadInt();
#if NET6_0_OR_GREATER
            if (length <= 512)
            {
                Span<byte> buffer = stackalloc byte[length];
                ReadFixed(buffer);
                return System.Text.Encoding.UTF8.GetString(buffer);
            }
            else
            {
                byte[] bufferArray = ArrayPool<byte>.Shared.Rent(length);
                Span<byte> buffer = bufferArray;
                ReadFixed(buffer.Slice(0, length));
                string result = System.Text.Encoding.UTF8.GetString(buffer);
                ArrayPool<byte>.Shared.Return(bufferArray);
                return result;
            }
#else
            byte[] buffer = new byte[length];
            ReadFixed(buffer);
            return System.Text.Encoding.UTF8.GetString(buffer);
#endif
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

#if NET6_0_OR_GREATER
        public void ReadFixed(Span<byte> buffer)
        {
            Read(buffer);
        }
#endif

        public void ReadFixed(byte[] buffer)
        {
            ReadFixed(buffer, 0, buffer.Length);
        }

        public void ReadFixed(byte[] buffer, int start, int length)
        {
            Read(buffer, start, length);
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
            Read(buffer, 0, buffer.Length);
            return buffer;
        }

        private static float IntBitsToFloat(int value)
        {
            return BitConverter.ToSingle(BitConverter.GetBytes(value), 0);
        }

        private byte Read()
        {
            int n = _stream.ReadByte();
            if (n >= 0) return (byte)n;
            throw new EndOfStreamException();
        }

        private void Read(byte[] buffer, int start, int len)
        {
            while (len > 0)
            {
                int n = _stream.Read(buffer, start, len);
                if (n <= 0) throw new EndOfStreamException();
                start += n;
                len -= n;
            }
        }

#if NET6_0_OR_GREATER
        private void Read(Span<byte> buffer)
        {
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
#endif

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

        private void Skip(int p)
        {
            _stream.Seek(p, SeekOrigin.Current);
        }

        private void Skip(long p)
        {
            _stream.Seek(p, SeekOrigin.Current);
        }

        internal byte[] ReadToEnd()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                _stream.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }
}
