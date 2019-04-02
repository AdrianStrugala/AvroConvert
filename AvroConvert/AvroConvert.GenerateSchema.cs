namespace AvroConvert
{
    using Microsoft.Hadoop.Avro;
    using System;

    public static partial class AvroConvert
    {
        public static string GenerateSchema(object obj)
        {
            var createMethod = typeof(AvroSerializer).GetMethod("Create", new Type[0]);
            var createGenericMethod = createMethod.MakeGenericMethod(obj.GetType());
            dynamic avroSerializer = createGenericMethod.Invoke(obj, null);

            string result = avroSerializer.GetType().GetProperty("WriterSchema").GetValue(avroSerializer, null).ToString();

            return result;
        }
    }
}
