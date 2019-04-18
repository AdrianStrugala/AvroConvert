namespace EhwarSoft.Avro
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Encoder;

    public static partial class AvroConvert
    {
        public static byte[] Serialize(object obj)
        {
            //TODO refactor this to reuse as much as possible
            MemoryStream resultStream = new MemoryStream();


            if (obj.GetType().TryGetInterfaceGenericParameters(typeof(IEnumerable<>)))
            {
                //serialize IEnumerable

                var ienumerableObj = obj as IList;

                string schema = AvroConvert.GenerateSchema(ienumerableObj[0]);

                var writer = Writer.OpenWriter(new GenericDatumWriter(Schema.Schema.Parse(schema)), resultStream);

                foreach (var @object in ienumerableObj)
                {
                    writer.Append(@object);
                }

                writer.Close();

            }
            else //serialize single object
            {
                string schema = AvroConvert.GenerateSchema(obj);
                var writer = Writer.OpenWriter(new GenericDatumWriter(Schema.Schema.Parse(schema)), resultStream);
                writer.Append(obj);
                writer.Close();
            }

            var result = resultStream.ToArray();
            return result;
        }

        public static bool TryGetInterfaceGenericParameters(this Type type, Type @interface)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return true;
            }

            var implements = type.FindInterfaces((ty, obj) => ty.IsGenericType && ty.GetGenericTypeDefinition() == @interface, null).FirstOrDefault();
            if (implements == null)
                return false;

            return true;
        }
    }


}
