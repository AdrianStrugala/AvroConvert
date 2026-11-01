using System.Collections.Generic;
using System.IO;
using System.Linq;
using SolTechnology.Avro.AvroObjectServices.FileHeader;
using SolTechnology.Avro.AvroObjectServices.Read;
using SolTechnology.Avro.Infrastructure.Exceptions;

namespace SolTechnology.Avro.Features.GetSchema
{
    internal class HeaderDecoder
    {
        internal string GetSchema(Stream stream) => ReadHeader(stream).GetMetadata(DataFileConstants.SchemaMetadataKey);

        internal IReadOnlyDictionary<string, byte[]> GetMetadata(Stream stream) => ReadHeader(stream).GetRawMetadata();

        private static Header ReadHeader(Stream stream)
        {
            var reader = new Reader(stream);

            // validate header 
            byte[] firstBytes = new byte[DataFileConstants.AvroHeader.Length];

            try
            {
                reader.ReadFixed(firstBytes);
            }
            catch (EndOfStreamException)
            {
                //stream shorter than AvroHeader
            }

            //does not contain header
            if (!firstBytes.SequenceEqual(DataFileConstants.AvroHeader))
            {
                throw new InvalidAvroObjectException("Object does not contain Avro Header");
            }

            return reader.ReadHeader();
        }
    }
}
