#region license
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

/** Modifications copyright(C) 2020 Adrian Struga³a **/
#endregion

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace SolTechnology.Avro.AvroObjectServices.Write
{
    /// <summary>
    /// Write leaf values into a pooled buffer. Without a target (memory mode) the buffer is the result; with a
    /// <see cref="Stream"/> or <see cref="IBufferWriter{T}"/> target it is copied there in big chunks by
    /// <see cref="Flush"/> or when it is full. Dispose returns the buffer to the pool.
    /// </summary>
    internal sealed partial class Writer : IWriter, IDisposable
    {
        internal const int TargetBufferSize = 4096;
        private const int MemoryBufferSize = 256;

        private readonly Stream _stream;
        private readonly IBufferWriter<byte> _output;
        private byte[] _buffer;
        private int _pos;

        /// <summary>Memory mode: the written bytes are available via <see cref="ToArray"/> / <see cref="WrittenSpan"/>.</summary>
        internal Writer(int initialSize = MemoryBufferSize)
        {
            _buffer = ArrayPool<byte>.Shared.Rent(initialSize);
        }

        /// <summary>Writes to <paramref name="stream"/>; call <see cref="Flush"/> before the stream is read or handed over.</summary>
        internal Writer(Stream stream)
        {
            _stream = stream;
            _buffer = ArrayPool<byte>.Shared.Rent(TargetBufferSize);
        }

        /// <summary>Writes to <paramref name="output"/>; call <see cref="Flush"/> before the output is read.</summary>
        internal Writer(IBufferWriter<byte> output)
        {
            _output = output;
            _buffer = ArrayPool<byte>.Shared.Rent(TargetBufferSize);
        }

        private bool HasTarget => _stream != null || _output != null;

        /// <summary>Bytes written and not flushed yet (in memory mode: all bytes written).</summary>
        internal int Length => _pos;

        internal ReadOnlySpan<byte> WrittenSpan => _buffer.AsSpan(0, _pos);

        internal ReadOnlyMemory<byte> WrittenMemory => _buffer.AsMemory(0, _pos);

        internal byte[] ToArray() => WrittenSpan.ToArray();

        internal void Reset() => _pos = 0;

        /// <summary>Moves the buffered bytes to the target (no-op in memory mode).</summary>
        internal void Flush()
        {
            if (_pos == 0 || !HasTarget)
            {
                return;
            }

            if (_stream != null)
            {
                _stream.Write(_buffer, 0, _pos);
            }
            else
            {
                _output.Write(WrittenSpan);
            }

            _pos = 0;
        }

        /// <summary><see cref="Flush"/>, then flushes the target stream itself.</summary>
        internal void Complete()
        {
            Flush();
            _stream?.Flush();
        }

        public void Dispose()
        {
            var buffer = _buffer;
            _buffer = Array.Empty<byte>();
            _pos = 0;
            if (buffer.Length > 0)
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Ensure(int size)
        {
            if (_buffer.Length - _pos < size)
            {
                Grow(size);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Grow(int size)
        {
            Flush();
            if (_buffer.Length - _pos >= size)
            {
                return;
            }

            var bigger = ArrayPool<byte>.Shared.Rent(Math.Max(_buffer.Length * 2, _pos + size));
            _buffer.AsSpan(0, _pos).CopyTo(bigger);
            if (_buffer.Length > 0)
            {
                ArrayPool<byte>.Shared.Return(_buffer);
            }
            _buffer = bigger;
        }

        /// <summary>
        /// null is written as zero bytes
        /// </summary>
        public void WriteNull()
        {
        }

        /// <summary>
        /// true is written as 1 and false 0.
        /// </summary>
        /// <param name="b">Boolean value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBoolean(bool b)
        {
            Ensure(1);
            _buffer[_pos++] = (byte)(b ? 1 : 0);
        }

        /// <summary>
        /// int and long values are written using variable-length, zig-zag coding.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt(int value)
        {
            WriteLong(value);
        }

        /// <summary>
        /// int and long values are written using variable-length, zig-zag coding.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteLong(long value)
        {
            Ensure(10);
            ulong n = (ulong)((value << 1) ^ (value >> 63));
            var buffer = _buffer;
            int pos = _pos;
            while (n >= 0x80)
            {
                buffer[pos++] = (byte)(n | 0x80);
                n >>= 7;
            }
            buffer[pos++] = (byte)n;
            _pos = pos;
        }

        public void WriteFloat(float value)
        {
            Ensure(4);
            BinaryPrimitives.WriteSingleLittleEndian(_buffer.AsSpan(_pos), value);
            _pos += 4;
        }

        public void WriteDouble(double value)
        {
            Ensure(8);
            BinaryPrimitives.WriteDoubleLittleEndian(_buffer.AsSpan(_pos), value);
            _pos += 8;
        }

        /// <summary>
        /// Bytes are encoded as a long followed by that many bytes of data.
        /// </summary>
        /// <param name="value"></param>
        /// 
        public void WriteBytes(byte[] value)
        {
            WriteLong(value.Length);
            WriteBytesRaw(value);
        }

        public void WriteBytes(ReadOnlySpan<byte> buffer)
        {
            WriteLong(buffer.Length);
            WriteBytesRaw(buffer);
        }

        public void WriteStream(MemoryStream stream)
        {
            WriteLong(stream.Length);
            if (stream.TryGetBuffer(out var buffer))
            {
                WriteBytesRaw(buffer);
            }
            else
            {
                WriteBytesRaw(stream.ToArray());
            }
        }

        /// <summary>
        /// A string is encoded as a long followed by
        /// that many bytes of UTF-8 encoded character data.
        /// </summary>
        /// <param name="value"></param>
        public void WriteString(string value)
        {
            int maxByteCount = Encoding.UTF8.GetMaxByteCount(value.Length);
            if (maxByteCount <= 63)
            {
                // At most 63 bytes: the length prefix is one byte, so the text is encoded right after it.
                Ensure(1 + maxByteCount);
                int length = Encoding.UTF8.GetBytes(value, _buffer.AsSpan(_pos + 1));
                _buffer[_pos] = (byte)(length << 1);
                _pos += 1 + length;
                return;
            }

            int byteCount = Encoding.UTF8.GetByteCount(value);
            WriteLong(byteCount);
            Ensure(byteCount);
            _pos += Encoding.UTF8.GetBytes(value, _buffer.AsSpan(_pos));
        }

        public void WriteEnum(int value)
        {
            WriteLong(value);
        }

        public void StartItem()
        {
        }

        public void WriteItemCount(long value)
        {
            if (value > 0) WriteLong(value);
        }

        public void WriteArrayStart()
        {
        }

        public void WriteArrayEnd()
        {
            WriteLong(0);
        }

        public void WriteMapStart()
        {
        }

        public void WriteMapEnd()
        {
            WriteLong(0);
        }

        public void WriteUnionIndex(int value)
        {
            WriteLong(value);
        }

        public void WriteFixed(byte[] data)
        {
            WriteBytesRaw(data);
        }

        public void WriteFixed(ReadOnlySpan<byte> bytes)
        {
            WriteBytesRaw(bytes);
        }

        public void WriteFixed(byte[] data, int start, int len)
        {
            WriteBytesRaw(data.AsSpan(start, len));
        }

        public void WriteBytesRaw(byte[] bytes)
        {
            WriteBytesRaw(bytes.AsSpan());
        }

        public void WriteBytesRaw(ReadOnlySpan<byte> bytes)
        {
            if (HasTarget && bytes.Length > _buffer.Length)
            {
                // Bigger than the buffer: no point in copying it twice.
                Flush();
                if (_stream != null)
                {
                    _stream.Write(bytes);
                }
                else
                {
                    _output.Write(bytes);
                }
                return;
            }

            Ensure(bytes.Length);
            bytes.CopyTo(_buffer.AsSpan(_pos));
            _pos += bytes.Length;
        }
    }
}
