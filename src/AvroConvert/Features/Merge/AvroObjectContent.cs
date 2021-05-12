using System.Collections.Generic;
using SolTechnology.Avro.AvroObjectServices.FileHeader;

namespace SolTechnology.Avro.Features.Merge
{
    internal class AvroObjectContent
    {
        public Header Header { get; set; }
        public List<byte[]> Data { get; set; }

        public AvroObjectContent()
        {
            Data = new List<byte[]>();
        }
    }
}