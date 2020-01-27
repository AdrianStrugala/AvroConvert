using System;
using System.IO;

namespace Snappy
{
    /// <summary>
    /// Straightforward wrapper around the underlying native Snappy library.
    /// You can compress, uncompress, and validate fixed-size buffers without framing overhead.
    /// Methods of this class forward their parameters directly to the native code without unnecessary buffer copying.
    /// </summary>
    public static class SnappyCodec
    {
        /// <summary>
        /// Compresses byte buffer using Snappy compression.
        /// </summary>
        /// <param name="input">Input buffer containing data to be compressed.</param>
        /// <param name="offset">Offset into the input buffer where input data is located.</param>
        /// <param name="length">Size of input data in the input buffer. Zero-length input is allowed. There's no maximum input size.</param>
        /// <param name="output">
        /// Output buffer where compressed data will be stored.
        /// Buffer length minus outOffset must be equal or higher than return value of GetMaxCompressedLength method.
        /// </param>
        /// <param name="outOffset">Offset into the output buffer where compressed data will be stored.</param>
        /// <returns>Length of compressed data written into the output buffer.</returns>
        public static unsafe int Compress(byte[] input, int offset, int length, byte[] output, int outOffset)
        {
            if (input == null || output == null)
                throw new ArgumentNullException();
            if (offset < 0 || length < 0 || offset + length > input.Length)
                throw new ArgumentOutOfRangeException("Selected range is outside the bounds of the input array");
            if (outOffset < 0 || outOffset >= output.Length)
                throw new ArgumentOutOfRangeException("Output offset is outside the bounds of the output array");
            int outLength = output.Length - outOffset;
            if (offset == input.Length)
            {
                input = new byte[1];
                offset = 0;
            }
            fixed (byte *inputPtr = &input[offset])
            fixed (byte* outputPtr = &output[outOffset])
            {
                var status = NativeProxy.Instance.Compress(inputPtr, length, outputPtr, ref outLength);
                if (status == SnappyStatus.Ok)
                    return outLength;
                else if (status == SnappyStatus.BufferTooSmall)
                    throw new ArgumentOutOfRangeException("Output array is too small");
                else
                    throw new InvalidDataException("Invalid input");
            }
        }

        /// <summary>
        /// Compresses byte buffer using Snappy compression.
        /// </summary>
        /// <param name="input">Input buffer containing data to be compressed. Zero-length input is allowed.</param>
        /// <returns>Compressed data.</returns>
        public static byte[] Compress(byte[] input)
        {
            if (input == null)
                throw new ArgumentNullException();
            var max = GetMaxCompressedLength(input.Length);
            var output = new byte[max];
            int outLength = Compress(input, 0, input.Length, output, 0);
            if (outLength == max)
                return output;
            var truncated = new byte[outLength];
            Array.Copy(output, truncated, outLength);
            return truncated;
        }

        /// <summary>
        /// Decompresses data previously compressed with Snappy.
        /// </summary>
        /// <param name="input">Input buffer containing compressed data.</param>
        /// <param name="offset">Offset of compressed data within the input buffer.</param>
        /// <param name="length">Length of compressed data within the input buffer.</param>
        /// <param name="output">
        /// Output buffer where decompressed data will be written.
        /// Buffer length minus outOffset must be equal or higher than return value of GetUncompressedLength method.
        /// </param>
        /// <param name="outOffset">Offset into the output buffer where decompressed data will be stored.</param>
        /// <returns>Length of decompressed data written into the output buffer.</returns>
        public static unsafe int Uncompress(byte[] input, int offset, int length, byte[] output, int outOffset)
        {
            if (input == null || output == null)
                throw new ArgumentNullException();
            if (offset < 0 || length < 0 || offset + length > input.Length)
                throw new ArgumentOutOfRangeException("Selected range is outside the bounds of the input array");
            if (length == 0)
                throw new InvalidDataException("Compressed block cannot be empty");
            if (outOffset < 0 || outOffset > output.Length)
                throw new ArgumentOutOfRangeException("Output offset is outside the bounds of the output array");
            int outLength = output.Length - outOffset;
            if (outOffset == output.Length)
            {
                output = new byte[1];
                outOffset = 0;
            }
            fixed (byte* inputPtr = &input[offset])
            fixed (byte* outputPtr = &output[outOffset])
            {
                var status = NativeProxy.Instance.Uncompress(inputPtr, length, outputPtr, ref outLength);
                if (status == SnappyStatus.Ok)
                    return outLength;
                else if (status == SnappyStatus.BufferTooSmall)
                    throw new ArgumentOutOfRangeException("Output array is too small");
                else
                    throw new InvalidDataException("Input is not a valid snappy-compressed block");
            }
        }

        /// <summary>
        /// Decompresses data previously compressed with Snappy.
        /// </summary>
        /// <param name="input">Input buffer containing compressed data.</param>
        /// <returns>Decompressed data.</returns>
        public static byte[] Uncompress(byte[] input)
        {
            var max = GetUncompressedLength(input);
            var output = new byte[max];
            int outLength = Uncompress(input, 0, input.Length, output, 0);
            if (outLength == max)
                return output;
            var truncated = new byte[outLength];
            Array.Copy(output, truncated, outLength);
            return truncated;
        }

        /// <summary>
        /// Estimates maximum length of compressed data.
        /// Note that compressed data may be slightly larger than uncompressed data in some extreme cases of uncompressible data.
        /// </summary>
        /// <param name="inLength">Length of uncompressed data used as a basis for calculation of maximum length of compressed data.</param>
        /// <returns>Maximum length of compressed data given input of length inLength.</returns>
        public static int GetMaxCompressedLength(int inLength)
        {
            return NativeProxy.Instance.GetMaxCompressedLength(inLength);
        }

        /// <summary>
        /// Retrieves length of uncompressed data for given buffer of compressed data. This is O(1) lookup
        /// that merely parses first few bytes of the compressed buffer where the length has been recorded during compression.
        /// </summary>
        /// <param name="input">Input buffer containing compressed data.</param>
        /// <param name="offset">Offset where compressed data is located in the input buffer.</param>
        /// <param name="length">Length of compressed data in the input buffer.</param>
        /// <returns>Exact length of uncompressed data encoded in the input buffer.</returns>
        public static unsafe int GetUncompressedLength(byte[] input, int offset, int length)
        {
            if (input == null)
                throw new ArgumentNullException();
            if (offset < 0 || length < 0 || offset + length > input.Length)
                throw new ArgumentOutOfRangeException("Selected range is outside the bounds of the input array");
            if (length == 0)
                throw new InvalidDataException("Compressed block cannot be empty");
            fixed (byte* inputPtr = &input[offset])
            {
                int outLength;
                var status = NativeProxy.Instance.GetUncompressedLength(inputPtr, length, out outLength);
                if (status == SnappyStatus.Ok)
                    return outLength;
                else
                    throw new InvalidDataException("Input is not a valid snappy-compressed block");
            }
        }

        /// <summary>
        /// Retrieves length of uncompressed data for given buffer of compressed data. This is O(1) lookup
        /// that merely parses first few bytes of the compressed buffer where the length has been recorded during compression.
        /// </summary>
        /// <param name="input">Input buffer containing compressed data.</param>
        /// <returns>Exact length of uncompressed data encoded in the input buffer.</returns>
        public static int GetUncompressedLength(byte[] input)
        {
            if (input == null)
                throw new ArgumentNullException();
            return GetUncompressedLength(input, 0, input.Length);
        }

        /// <summary>
        /// Checks integrity of compressed data. This method performs sanity checks that ensure that the buffer can be decompressed.
        /// It doesn't check integrity of data. It merely ensures that decompression will succeed with _some_ result.
        /// CRC or other data integrity checks can be provided by higher level protocols like the Snappy framing format.
        /// </summary>
        /// <param name="input">Input buffer containing compressed data.</param>
        /// <param name="offset">Offset where compressed data is located in the input buffer.</param>
        /// <param name="length">Length of compressed data in the input buffer.</param>
        /// <returns>True if the buffer contains valid Snappy compressed block. False otherwise.</returns>
        public static unsafe bool Validate(byte[] input, int offset, int length)
        {
            if (input == null)
                throw new ArgumentNullException();
            if (offset < 0 || length < 0 || offset + length > input.Length)
                throw new ArgumentOutOfRangeException("Selected range is outside the bounds of the input array");
            if (length == 0)
                return false;
            fixed (byte* inputPtr = &input[offset])
                return NativeProxy.Instance.ValidateCompressedBuffer(inputPtr, length) == SnappyStatus.Ok;
        }

        /// <summary>
        /// Checks integrity of compressed data. This method performs sanity checks that ensure that the buffer can be decompressed.
        /// It doesn't check integrity of data. It merely ensures that decompression will succeed with _some_ result.
        /// CRC or other data integrity checks can be provided by higher level protocols like the Snappy framing format.
        /// </summary>
        /// <param name="input">Input buffer containing compressed data.</param>
        /// <returns>True if the buffer contains valid Snappy compressed block. False otherwise.</returns>
        public static bool Validate(byte[] input)
        {
            if (input == null)
                throw new ArgumentNullException();
            return Validate(input, 0, input.Length);
        }
    }
}