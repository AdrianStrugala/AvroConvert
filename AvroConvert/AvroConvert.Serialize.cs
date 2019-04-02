namespace AvroConvert
{
    using Avro;
    using Encoder;
    using Microsoft.Hadoop.Avro;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public static partial class AvroConvert
    {
        public static byte[] Serialize(object obj)
        {
            MemoryStream resultStream = new MemoryStream();

            Type[] typeArguments;
            if (obj.GetType().TryGetInterfaceGenericParameters(typeof(IEnumerable<>), out typeArguments))
            {
                var ienumerableObj = obj as IList;
                var innerType = typeArguments.FirstOrDefault();

                var createMethod = typeof(AvroSerializer).GetMethod("Create", new Type[0]);
                var createGenericMethod = createMethod.MakeGenericMethod(innerType);
                dynamic avroSerializer = createGenericMethod.Invoke(ienumerableObj[0], null);

                var schema = avroSerializer.GetType().GetProperty("WriterSchema").GetValue(avroSerializer, null).ToString();


                var writer = Writer.OpenWriter(new GenericDatumWriter(Schema.Parse(schema)), resultStream);

                foreach (var @object in ienumerableObj)
                {
                    writer.Append(@object);
                }

                writer.Close();

            }
            else
            {
                var createMethod = typeof(AvroSerializer).GetMethod("Create", new Type[0]);
                var createGenericMethod = createMethod.MakeGenericMethod(obj.GetType());
                dynamic avroSerializer = createGenericMethod.Invoke(obj, null);

                var schema = avroSerializer.GetType().GetProperty("WriterSchema").GetValue(avroSerializer, null).ToString();
                var writer = Writer.OpenWriter(new GenericDatumWriter(Schema.Parse(schema)), resultStream);
                writer.Append(obj);
                writer.Close();
            }

            var result = resultStream.ToArray();
            return result;
        }

        public static bool TryGetInterfaceGenericParameters(this Type type, Type @interface, out Type[] typeParameters)
        {
            typeParameters = null;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == @interface)
            {
                typeParameters = type.GetGenericArguments();
                return true;
            }

            var implements = type.FindInterfaces((ty, obj) => ty.IsGenericType && ty.GetGenericTypeDefinition() == @interface, null).FirstOrDefault();
            if (implements == null)
                return false;

            typeParameters = implements.GetGenericArguments();
            return true;
        }
    }


}
