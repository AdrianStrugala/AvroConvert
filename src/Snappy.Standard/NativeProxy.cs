
/** Modifications copyright(C) 2020 Adrian Struga³a **/

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Snappy
{
    abstract class NativeProxy
    {
        public static readonly NativeProxy Instance = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? (IntPtr.Size == 4 ? (NativeProxy)new Native32() : new Native64()) : (NativeProxy)new Native();

        protected NativeProxy(string name)
        {
        }

        public unsafe abstract SnappyStatus Compress(byte* input, int inLength, byte* output, ref int outLength);
        public unsafe abstract SnappyStatus Uncompress(byte* input, int inLength, byte* output, ref int outLength);
        public abstract int GetMaxCompressedLength(int inLength);
        public unsafe abstract SnappyStatus GetUncompressedLength(byte* input, int inLength, out int outLength);
        public unsafe abstract SnappyStatus ValidateCompressedBuffer(byte* input, int inLength);

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern IntPtr LoadLibrary(string lpFileName);
    }
}