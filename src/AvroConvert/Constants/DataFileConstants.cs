namespace AvroConvert.Constants
{
    public class DataFileConstants
    {
        public const string SyncMetadataKey = "avro.sync";
        public const string CodecMetadataKey = "avro.codec";
        public const string SchemaMetadataKey = "avro.schema";
        public const string NullCodec = "null";
        public const string DeflateCodec = "deflate";
        public const string MetaDataReserved = "avro";

        public static byte[] AvroHeader = { (byte)'O',
                                            (byte)'b',
                                            (byte)'j',
                                            (byte) 1 };

        public const int NullCodecHash = 2;
        public const int DeflateCodecHash = 0;

        public const int SyncSize = 16;
        public const int DefaultSyncInterval = 4000 * SyncSize;
    }
}
