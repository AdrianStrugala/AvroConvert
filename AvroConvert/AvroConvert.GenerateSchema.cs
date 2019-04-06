namespace AvroConvert
{
    using Microsoft.Hadoop.Avro;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Runtime.Serialization;

    public static partial class AvroConvert
    {
        public static string GenerateSchema(object obj)
        {
            var inMemoryInstance = AddCustomAttributeToObject<DataContractAttribute>(obj);


            PropertyInfo[] properties = inMemoryInstance.GetType().GetProperties();

            foreach (PropertyInfo prop in properties)
            {
                if (typeof(IList).IsAssignableFrom(prop.PropertyType) &&
                    prop.PropertyType.GetTypeInfo().IsGenericType)
                {
                    // We have a List<T> or array
                    result.Add(prop.Name, SplitKeyValues(prop.GetValue(item)));
                }

                else if (prop.PropertyType.GetTypeInfo().IsValueType ||
                         prop.PropertyType == typeof(string))
                {
                    // We have a simple type

                    result.Add(prop.Name, prop.GetValue(item));
                }
                else
                {
                    result.Add(prop.Name, SplitKeyValues(prop.GetValue(item)));
                }
            }

            var createMethod = typeof(AvroSerializer).GetMethod("Create", new Type[0]);
            var createGenericMethod = createMethod.MakeGenericMethod(inMemoryInstance.GetType());
            dynamic avroSerializer = createGenericMethod.Invoke(inMemoryInstance, null);

            string result = avroSerializer.GetType().GetProperty("WriterSchema").GetValue(avroSerializer, null).ToString();

            return result;
        }

        private static object AddCustomAttributeToObject<T>(object obj)
        {
            var objType = obj.GetType();

            var assemblyName = new System.Reflection.AssemblyName("InMemory");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(objType.Assembly.GetName(),
                AssemblyBuilderAccess.Run);

            var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);
            var typeBuilder = moduleBuilder.DefineType(objType.Name, System.Reflection.TypeAttributes.Public, objType);

            var attributeConstructor = typeof(T).GetConstructor(new Type[] { });
            var attributeProperties = typeof(T).GetProperties();

            var attributeBuilder = new CustomAttributeBuilder(attributeConstructor, new string[] { }, attributeProperties.Where(p => p.Name == "Name").ToArray(), new object[] { objType.Name });

            typeBuilder.SetCustomAttribute(attributeBuilder);

            var inMemoryType = typeBuilder.CreateType();
            var inMemoryInstance = Activator.CreateInstance(inMemoryType);

            return inMemoryInstance;
        }

    }
}
