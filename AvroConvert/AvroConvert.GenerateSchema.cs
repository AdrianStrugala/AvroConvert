namespace AvroConvert
{
    using Microsoft.Hadoop.Avro;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Dynamic;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Runtime.Serialization;

    public static partial class AvroConvert
    {
        public static string GenerateSchema(object obj)
        {
            var sth = new ExpandoObject() as IDictionary<string, object>;
            sth.Add(obj.GetType().Name, obj);

            AttributeCollection attributes = TypeDescriptor.GetAttributes(obj);

            var dataContractAttribute = new DataContractAttribute();
            dataContractAttribute.Name = "User";
            dataContractAttribute.Namespace = "water";

            TypeDescriptor.AddAttributes(obj.GetType(), dataContractAttribute);
            TypeDescriptor.AddAttributes(obj, dataContractAttribute);


            AttributeCollection attributes2 = TypeDescriptor.GetAttributes(obj);


            //            var constructor = typeof(EntityElementIdAttribute).GetConstructors().First();
            //            var customAttributeBuilder = new CustomAttributeBuilder(constructor, new object[] { id });
            //
            //            typeBuilder.SetCustomAttribute(customAttributeBuilder);



//            TypeBuilder tb = mb.DefineType(
//                "MyDynamicType",
//                TypeAttributes.Public);
//
//            // Add a private field of type int (Int32).
//            FieldBuilder fbNumber = tb.DefineField(
//                "m_number",
//                typeof(int),
//                FieldAttributes.Private);


            //            var inputObject = new InputObject();
            //            inputObject.input = obj;


            //       var lol = DeepCloner.GetFastDeepClonerFields(inputObject.GetType());

            //  var prop = DeepCloner.GetFastDeepClonerProperties(sth.GetType());


            // prop.Attributes.Add(new JsonIgnoreAttribute());


            //    obj.GetType().CustomAttributes

            var yp34 = obj.GetType();
            

            var aName = new System.Reflection.AssemblyName("SomeNamespace");
            var ab =  AssemblyBuilder.DefineDynamicAssembly(yp34.Assembly.GetName(),
                  AssemblyBuilderAccess.Run);
            var mb = ab.DefineDynamicModule(aName.Name);
            var tb = mb.DefineType(yp34.Name, System.Reflection.TypeAttributes.Public, yp34);

           // var attrCtorParams = new Type[] { typeof(string) };
           var attrCtorInfo = typeof(DataContractAttribute).GetConstructor(new Type[]{});

            var sth2 = typeof(DataContractAttribute).GetProperties();

            var attrBuilder = new CustomAttributeBuilder(attrCtorInfo, new string[]{}, sth2.Where(p => p.Name == "Name").ToArray(), new string[]{"User"});
           
            tb.SetCustomAttribute(attrBuilder);

            //   tb.SetCustomAttribute(dataContractAttribute);


            //
            //          var ab =  AssemblyBuilder.DefineDynamicAssembly(yp34.Assembly.GetName(),
            //                AssemblyBuilderAccess.Run);
            //
            //
            //          ModuleBuilder tb2 = ab.DefineDynamicModule("Temp");
            //
            //          TypeBuilder tb3 = tb2.

            // For a single-module assembly, the module name is usually
            // the assembly name plus an extension.
            //            ModuleBuilder mb =
            //                ab.DefineDynamicModule(aName.Name, aName.Name + ".dll");
            //
            //            TypeBuilder tb = mb.DefineType(
            //                "MyDynamicType",
            //                TypeAttributes.Public);

            var newType = tb.CreateType();
            var instance = Activator.CreateInstance(newType);

            var xd1 = yp34 == (object)typeof(IntPtr);
            var xd2 = yp34 == (object)typeof(UIntPtr);
            var xd3 = yp34 == (object)typeof(object);
            var xd4 = yp34.ContainsGenericParameters();
            // return true;

            var lol2 = CustomAttributeExtensions.GetCustomAttributes(newType.GetTypeInfo());

//            DataContractAttribute contractAttribute = Enumerable.OfType<DataContractAttribute>(CustomAttributeExtensions.GetCustomAttributes(yp34.GetTypeInfo(), false)).SingleOrDefault<DataContractAttribute>();

            var createMethod = typeof(AvroSerializer).GetMethod("Create", new Type[0]);
            var createGenericMethod = createMethod.MakeGenericMethod(instance.GetType());
            dynamic avroSerializer = createGenericMethod.Invoke(instance, null);

            string result = avroSerializer.GetType().GetProperty("WriterSchema").GetValue(avroSerializer, null).ToString();

            return result;
        }

        class InputObject
        {
            public object input { get; set; }
        }
    }
}
