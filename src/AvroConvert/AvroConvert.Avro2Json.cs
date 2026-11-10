#region license
/**Copyright (c) 2019-2026 Adrian Strugała (SolTechnology)
*
* Licensed under the PolyForm Noncommercial License 1.0.0, the PolyForm Small Business License 1.0.0,
* or the SolTechnology Commercial Licence – pick the one that applies to you. See LICENSE.md:
*
* https://github.com/AdrianStrugala/AvroConvert/blob/master/LICENSE.md
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
*
* Required Notice: Copyright Adrian Strugała (https://soltechnology.dev)
*/
#endregion
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using SolTechnology.Avro.AvroObjectServices.FileHeader;
using SolTechnology.Avro.AvroObjectServices.Read;
using SolTechnology.Avro.Features.AvroToJson;
using System.Diagnostics.CodeAnalysis;
using SolTechnology.Avro.Infrastructure;

namespace SolTechnology.Avro
{
    public static partial class AvroConvert
    {
        /// <summary>
        /// Converts Avro object directly to JSON format
        /// </summary>
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        public static string Avro2Json(byte[] avro)
        {
            using (var stream = new MemoryStream(avro))
            {
                var decoder = new Decoder();
                var deserialized = decoder.Decode(stream, null);
                var json = JsonOutput.Write(deserialized);

                return json;
            }
        }


        /// <summary>
        /// Converts Avro object compatible with given <paramref name="avroSchema"/> directly to JSON format
        /// </summary>
        /// <remarks>Kept for 3.x code and hidden from IntelliSense. Use the overload taking <see cref="AvroSchema"/>.</remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        public static string Avro2Json(byte[] avro, string avroSchema)
        {
            return Avro2Json(avro, AvroSchema.Parse(avroSchema));
        }

        /// <summary>
        /// Converts Avro object compatible with given <paramref name="avroSchema"/> directly to JSON format
        /// </summary>
        /// <remarks>
        /// For data with a header (an Avro file) <paramref name="avroSchema"/> is the reader schema: the data is read with the
        /// schema in the header and resolved to it, as the specification defines (fields added with a default, fields left
        /// out, promoted types, aliases). For data without a header it is the schema the data was written with.
        /// </remarks>
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        public static string Avro2Json(byte[] avro, AvroSchema avroSchema)
        {
            if (avroSchema != null && FileOf(avro) is { Entries: > 0 } file && file.Writer.Json != avroSchema.Json)
            {
                // One entry is the value, more are an array, as without a reader schema.
                using var data = new MemoryStream(avro);
                var decoder = new Features.Deserialize.Decoder(SchemaData);
                return JsonOutput.Write(file.Entries == 1
                    ? decoder.Decode<object>(data, avroSchema.TypeSchema)
                    : decoder.Decode<List<object>>(data, avroSchema.TypeSchema));
            }

            using (var stream = new MemoryStream(avro))
            {
                var decoder = new Decoder();
                var deserialized = decoder.Decode(stream, avroSchema.TypeSchema);
                var json = JsonOutput.Write(deserialized);

                return json;
            }
        }

        private static readonly AvroConvertOptions SchemaData = new() { RecordsAsSchemaData = true };

        /// <summary>The schema in the header of an Avro file and the number of its entries; null for data without a header.</summary>
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        private static (AvroSchema Writer, long Entries)? FileOf(byte[] avro)
        {
            if (avro.Length < DataFileConstants.AvroHeader.Length || !avro.AsSpan(0, DataFileConstants.AvroHeader.Length).SequenceEqual(DataFileConstants.AvroHeader))
            {
                return null;
            }

            var reader = new Reader(avro);
            reader.SkipFixed(DataFileConstants.AvroHeader.Length);
            var header = reader.ReadHeader();
            reader.SkipFixed(header.SyncData.Length);
            long entries = 0;
            while (!reader.IsReadToEnd())
            {
                entries += reader.ReadLong(); // a block: the count of its entries, its size and bytes, the sync marker
                reader.SkipBytes();
                reader.SkipFixed(header.SyncData.Length);
            }

            return (AvroSchema.Parse(header.GetMetadata(DataFileConstants.SchemaMetadataKey)), entries);
        }
    }
}
