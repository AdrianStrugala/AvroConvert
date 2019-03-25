namespace AvroConvert.Constants
{
    public class DataFileConstants
    {
        public const string MetaDataSync = "avro.sync";
        public const string MetaDataCodec = "avro.codec";
        public const string MetaDataSchema = "avro.schema";
        public const string NullCodec = "null";
        public const string DeflateCodec = "deflate";
        public const string MetaDataReserved = "avro";

        public static byte[] AvroHeader = { (byte)'O',
                                            (byte)'b',
                                            (byte)'j',
                                            1 };

        public const int NullCodecHash = 2;
        public const int DeflateCodecHash = 0;

        public const int SyncSize = 16;
        public const int DefaultSyncInterval = 4000 * SyncSize;
    }
}
