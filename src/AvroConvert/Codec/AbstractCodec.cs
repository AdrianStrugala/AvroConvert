using System;
using System.Linq;

namespace SolTechnology.Avro.Codec
{
    public abstract class AbstractCodec
    {
        public abstract string Name { get; }

        public abstract byte[] Decompress(byte[] compressedData);

        public abstract byte[] Compress(byte[] uncompressedData);

        public abstract override int GetHashCode();

        public static AbstractCodec CreateCodec(CodecType codecType)
        {
            switch (codecType)
            {
                case CodecType.Deflate:
                    return new DeflateCodec();
                case CodecType.Snappy:
                    return new SnappyCodec();
                default:
                    return new NullCodec();
            }
        }

        public static AbstractCodec CreateCodecFromString(string codecName)
        {
            System.Type codecType = typeof(AbstractCodec).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(AbstractCodec)) && !t.IsAbstract)
                                              .First(t => t.Name.ToLower().Contains(codecName));

            if (codecType == null)
            {
                return new NullCodec();
            }

            return (AbstractCodec)Activator.CreateInstance(codecType);
        }
    }
}