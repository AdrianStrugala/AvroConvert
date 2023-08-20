using System;
using System.Collections.Generic;
using System.Text;
using SolTechnology.Avro.Converters;

namespace SolTechnology.Avro
{
    public class AvroConvertOptions
    {
        public IEnumerable<IAvroConverter> AvroConverters { get; set; }

        public CodecType Codec { get; set; }
    }
}
