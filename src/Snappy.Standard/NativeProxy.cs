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
//            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
//            {
//                var assembly = typeof(SnappyCodec).GetTypeInfo().Assembly;
//                var folder = Path.Combine(Path.GetTempPath(), "Snappy.Core-" + assembly.GetName().Version.ToString());
//                Directory.CreateDirectory(folder);
//                var path = Path.Combine(folder, name);
//                byte[] contents;
//                using (var input = assembly.GetManifestResourceStream("Snappy.Standard.Resources." + name))
//                using (var buffer = new MemoryStream())
//                {
//                    byte[] block = new byte[4096];
//                    int copied;
//                    while ((copied = input.Read(block, 0, block.Length)) != 0)
//                        buffer.Write(block, 0, copied);
//                    contents = buffer.ToArray();
//                }
//                if (!File.Exists(path))
//                {
//                    using (var output = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None))
//                        output.Write(contents, 0, contents.Length);
//                }
//                IntPtr h = LoadLibrary(path);
//                if (h == IntPtr.Zero)
//                    throw new Exception(path);
//            }
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