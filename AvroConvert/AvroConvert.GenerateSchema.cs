namespace AvroConvert
{
    using Microsoft.Hadoop.Avro;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Dynamic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using FastDeepCloner;
    using Newtonsoft.Json;

    public static partial class AvroConvert
    {
        public static string GenerateSchema(object obj)
        {
            var sth = new ExpandoObject() as IDictionary<string, object>;
            sth.Add(obj.GetType().Name, obj);

            AttributeCollection attributes = TypeDescriptor.GetAttributes(obj);

            var xd = new DataContractAttribute();
            xd.Name = "User";
            xd.Namespace = "water";

            TypeDescriptor.AddAttributes(obj.GetType(), xd);
            TypeDescriptor.AddAttributes(obj, xd);


            AttributeCollection attributes2 = TypeDescriptor.GetAttributes(obj);

            //            var inputObject = new InputObject();
            //            inputObject.input = obj;


            //       var lol = DeepCloner.GetFastDeepClonerFields(inputObject.GetType());

            //  var prop = DeepCloner.GetFastDeepClonerProperties(sth.GetType());


            // prop.Attributes.Add(new JsonIgnoreAttribute());


            //    obj.GetType().CustomAttributes

            var type = obj.GetType();

            var xd1 = (object) type == (object) typeof(IntPtr);
            var xd2 = (object) type == (object) typeof(UIntPtr);
            var xd3 = (object) type == (object) typeof(object);
            var xd4 = type.ContainsGenericParameters();
            // return true;

            var lol2 = CustomAttributeExtensions.GetCustomAttributes((MemberInfo) type.GetTypeInfo());

            DataContractAttribute contractAttribute = (DataContractAttribute)Enumerable.SingleOrDefault<DataContractAttribute>(Enumerable.OfType<DataContractAttribute>((IEnumerable)CustomAttributeExtensions.GetCustomAttributes((MemberInfo)type.GetTypeInfo(), false)));

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
