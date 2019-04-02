namespace AvroConvert
{
    using Avro;
    using Encoder;
    using Microsoft.Hadoop.Avro;
    using System;
    using System.IO;

    public static partial class AvroConvert
    {
        public static byte[] Serialize(object obj)
        {

            var createMethod = typeof(AvroSerializer).GetMethod("Create", new Type[0]);
            var createGenericMethod = createMethod.MakeGenericMethod(obj.GetType());
            dynamic avroSerializer = createGenericMethod.Invoke(obj, null);

            var schema = avroSerializer.GetType().GetProperty("WriterSchema").GetValue(avroSerializer, null).ToString();


            MemoryStream resultStream = new MemoryStream();
            var writer = Writer.OpenWriter(new GenericDatumWriter(Schema.Parse(schema)), resultStream);


            writer.Append(obj);

            writer.Close();

            //   var writer2 = Encoder.Writer.OpenWriter(new Encoder.GenericDatumWriter(Schema.Parse(xd)), "result.avro");
            //            writer2.Append(dupa);
            //            writer2.Append(dupa2);
            //            writer2.Close();

            //            using (MemoryStream ms = new MemoryStream())
            //            {
            //                resultStream.CopyTo(ms);
            //
            //                var xd3 = resultStream;
            //
            //                return ms.ToArray();
            //            }

            var result = resultStream.ToArray();



            return result;
        }
    }


}
