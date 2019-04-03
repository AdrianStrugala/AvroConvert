namespace AvroConvert
{
    using Microsoft.Hadoop.Avro;
    using System;
    using System.Linq;
    using FastDeepCloner;
    using Newtonsoft.Json;

    public static partial class AvroConvert
    {
        public static string GenerateSchema(object obj)
        {
            var inputObject = new InputObject();
            inputObject.input = obj;


     //       var lol = DeepCloner.GetFastDeepClonerFields(inputObject.GetType());

            var prop = DeepCloner.GetFastDeepClonerProperties(inputObject.GetType());
            // prop.Attributes.Add(new JsonIgnoreAttribute());


            //    obj.GetType().CustomAttributes

            var createMethod = typeof(AvroSerializer).GetMethod("Create", new Type[0]);
            var createGenericMethod = createMethod.MakeGenericMethod(obj.GetType());
            dynamic avroSerializer = createGenericMethod.Invoke(obj, null);

            string result = avroSerializer.GetType().GetProperty("WriterSchema").GetValue(avroSerializer, null).ToString();

            return result;
        }

        class InputObject
        {
            public object input { get; set; }
        }
    }
}
