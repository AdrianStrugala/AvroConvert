using System.Runtime.InteropServices;

namespace Snappy
{
    class Native64 : NativeProxy
    {
        public Native64() : base("snappy64.dll") { }

        public unsafe override SnappyStatus Compress(byte *input, int inLength, byte *output, ref int outLength)
        {
            checked
            {
                ulong refLength = (ulong)outLength;
                var status = snappy_compress(input, (ulong)inLength, output, ref refLength);
                outLength = (int)refLength;
                return status;
            }
        }

        public unsafe override SnappyStatus Uncompress(byte* input, int inLength, byte* output, ref int outLength)
        {
            checked
            {
                ulong refLength = (ulong)outLength;
                var status = snappy_uncompress(input, (ulong)inLength, output, ref refLength);
                outLength = (int)refLength;
                return status;
            }
        }

        public override int GetMaxCompressedLength(int inLength)
        {
            return checked((int)snappy_max_compressed_length((ulong)inLength));
        }

        public unsafe override SnappyStatus GetUncompressedLength(byte* input, int inLength, out int outLength)
        {
            checked
            {
                ulong unsignedLength;
                var status = snappy_uncompressed_length(input, (ulong)inLength, out unsignedLength);
                outLength = (int)unsignedLength;
                return status;
            }
        }

        public unsafe override SnappyStatus ValidateCompressedBuffer(byte* input, int inLength)
        {
            return checked(snappy_validate_compressed_buffer(input, (ulong)inLength));
        }

        [DllImport("snappy64.dll", CallingConvention = CallingConvention.Cdecl)]
        static unsafe extern SnappyStatus snappy_compress(byte *input, ulong input_length, byte *output, ref ulong output_length);

        [DllImport("snappy64.dll", CallingConvention = CallingConvention.Cdecl)]
        static unsafe extern SnappyStatus snappy_uncompress(byte* input, ulong input_length, byte* output, ref ulong output_length);

        [DllImport("snappy64.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern ulong snappy_max_compressed_length(ulong input_length);

        [DllImport("snappy64.dll", CallingConvention = CallingConvention.Cdecl)]
        static unsafe extern SnappyStatus snappy_uncompressed_length(byte* input, ulong input_length, out ulong output_length);

        [DllImport("snappy64.dll", CallingConvention = CallingConvention.Cdecl)]
        static unsafe extern SnappyStatus snappy_validate_compressed_buffer(byte* input, ulong input_length);
    }
}
