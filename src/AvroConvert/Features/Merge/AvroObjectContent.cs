using System.Collections.Generic;
using SolTechnology.Avro.AvroObjectServices.FileHeader;
using SolTechnology.Avro.AvroObjectServices.FileHeader.Codec;

namespace SolTechnology.Avro.Features.Merge
{
    internal class AvroObjectContent
    {
        internal Header Header { get; set; }
        internal List<DataBlock> DataBlocks { get; set; }
        internal AbstractCodec Codec { get; set; }

        internal AvroObjectContent()
        {
            DataBlocks = new List<DataBlock>();
        }
    }

    internal class DataBlock
    {
        internal long ItemsCount { get; set; }
        internal  byte[] Data { get; set; }
    }
}