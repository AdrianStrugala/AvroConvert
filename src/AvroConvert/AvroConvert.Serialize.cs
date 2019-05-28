namespace AvroConvert
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Write;

    public static partial class AvroConvert
    {
        public static byte[] Serialize(object obj)
        {
            //TODO refactor this to reuse as much as possible
            MemoryStream resultStream = new MemoryStream();


            if (obj is IList listObj)
            {
                //serialize IEnumerable

                string schema = AvroConvert.GenerateSchema(listObj[0]);

                using (var writer = new Encoder(Schema.Schema.Parse(schema), resultStream))
                {
                    foreach (var @object in listObj)
                    {
                        writer.Append(@object);
                    }
                }
            }
            else //serialize single object
            {
                string schema = AvroConvert.GenerateSchema(obj);
                using (var writer = new Encoder(Schema.Schema.Parse(schema), resultStream))
                {
                    writer.Append(obj);
                }
            }

            var result = resultStream.ToArray();
            return result;
        }
    }
}
