using System.Collections.Generic;

namespace SolTechnology.Avro.Helpers
{
    public class Header
    {
        public IDictionary<string, byte[]> MetaData { get; }

        public byte[] SyncData { get; }

        public Schema.Schema Schema { get; set; }

        public Header()
        {
            MetaData = new Dictionary<string, byte[]>();
            SyncData = new byte[16];
        }
    }
}
