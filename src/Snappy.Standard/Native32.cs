using System.Runtime.InteropServices;

namespace Snappy
{
    class Native32 : NativeProxy
    {
        public Native32() : base("snappy32.dll") { }

        public unsafe override SnappyStatus Compress(byte *input, int inLength, byte *output, ref int outLength)
        {
            checked
            {
                uint refLength = (uint)outLength;
                var status = snappy_compress(input, (uint)inLength, output, ref refLength);
                outLength = (int)refLength;
                return status;
            }
        }

        public unsafe override SnappyStatus Uncompress(byte* input, int inLength, byte* output, ref int outLength)
        {
            checked
            {
                uint refLength = (uint)outLength;
                var status = snappy_uncompress(input, (uint)inLength, output, ref refLength);
                outLength = (int)refLength;
                return status;
            }
        }

        public override int GetMaxCompressedLength(int inLength)
        {
            return checked((int)snappy_max_compressed_length((uint)inLength));
        }

        public unsafe override SnappyStatus GetUncompressedLength(byte* input, int inLength, out int outLength)
        {
            checked
            {
                uint unsignedLength;
                var status = snappy_uncompressed_length(input, (uint)inLength, out unsignedLength);
                outLength = (int)unsignedLength;
                return status;
            }
        }

        public unsafe override SnappyStatus ValidateCompressedBuffer(byte* input, int inLength)
        {
            return checked(snappy_validate_compressed_buffer(input, (uint)inLength));
        }

        [DllImport("snappy32.dll", CallingConvention = CallingConvention.Cdecl)]
        static unsafe extern SnappyStatus snappy_compress(byte* input, uint input_length, byte* output, ref uint output_length);

        [DllImport("snappy32.dll", CallingConvention = CallingConvention.Cdecl)]
        static unsafe extern SnappyStatus snappy_uncompress(byte* input, uint input_length, byte* output, ref uint output_length);

        [DllImport("snappy32.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern uint snappy_max_compressed_length(uint input_length);

        [DllImport("snappy32.dll", CallingConvention = CallingConvention.Cdecl)]
        static unsafe extern SnappyStatus snappy_uncompressed_length(byte* input, uint input_length, out uint output_length);

        [DllImport("snappy32.dll", CallingConvention = CallingConvention.Cdecl)]
        static unsafe extern SnappyStatus snappy_validate_compressed_buffer(byte* input, uint input_length);
    }
}
