namespace EhwarSoft.Avro
{
    using Microsoft.Hadoop.Avro;
    using System;
    using System.Collections;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Runtime.Serialization;

    public static partial class AvroConvert
    {
        private static ModuleBuilder _moduleBuilder;

        public static string GenerateSchema(object obj)
        {
            var assemblyName = new AssemblyName("InMemory");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName,
                AssemblyBuilderAccess.Run);

            _moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);

            object inMemoryInstance = DecorateObjectWithAvroAttributes(obj.GetType());

            //invoke Create method of AvroSerializer
            var createMethod = typeof(AvroSerializer).GetMethod("Create", new Type[0]);
            var createGenericMethod = createMethod.MakeGenericMethod(inMemoryInstance.GetType());
            dynamic avroSerializer = createGenericMethod.Invoke(inMemoryInstance, null);

            string result = avroSerializer.GetType().GetProperty("WriterSchema").GetValue(avroSerializer, null)
                .ToString();

            return result;
        }

        private static object DecorateObjectWithAvroAttributes(Type objType)
        {
            var existingType = _moduleBuilder.GetType(objType.Name);
            if (existingType != null)
            {
                return Activator.CreateInstance(existingType);
            }


            TypeBuilder typeBuilder = _moduleBuilder.DefineType(objType.Name, TypeAttributes.Public);          

            if (objType.IsArray && objType.FullName.EndsWith("[]"))
            {
                string fullName = objType.FullName.Substring(0, objType.FullName.Length - 2);
                var field = Type.GetType($"{fullName},{objType.Assembly.GetName().Name}");

                var avroFieldType = DecorateObjectWithAvroAttributes(field).GetType();

                var avroArray = Array.CreateInstance(avroFieldType, 1);
                objType = avroArray.GetType();
            }

            else if (typeof(IList).IsAssignableFrom(objType))
            {
                var field = objType.GetProperties()[2];
                var avroFieldType = DecorateObjectWithAvroAttributes(field.PropertyType).GetType();

                var avroArray = Array.CreateInstance(avroFieldType, 1);
                objType = avroArray.GetType();
            }

            else if (objType == typeof(Guid))
            {
                objType = typeof(string);
            }

            else if (!(objType.GetTypeInfo().IsValueType ||
                       objType == typeof(string)))
            {
                //complex type          
                PropertyInfo[] properties = objType.GetProperties();
                foreach (var prop in properties)
                {
                    var propertyType = prop.PropertyType;
                    propertyType = DecorateObjectWithAvroAttributes(propertyType).GetType();
                    typeBuilder = AddPropertyToTypeBuilder(typeBuilder, propertyType, prop.Name);
                }
            }

            else
            {
                //simple type
            }

            //            if (typeof(IDictionary).IsAssignableFrom(objType) || objType.GetTypeInfo().IsValueType)
            //            {
            //                return Activator.CreateInstance(objType);
            //            }
            //



            var attributeBuilder = GenerateCustomAttributeBuilder<DataContractAttribute>(objType.Name);
            typeBuilder.SetCustomAttribute(attributeBuilder);

            var inMemoryType = typeBuilder.CreateType();
            var inMemoryInstance = Activator.CreateInstance(inMemoryType);

            return inMemoryInstance;
        }



        private static TypeBuilder AddPropertyToTypeBuilder(TypeBuilder typeBuilder, Type properType, string name)
        {
            //mimic property 
            PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(name, PropertyAttributes.None, properType, null);

            var attributeBuilder = GenerateCustomAttributeBuilder<DataMemberAttribute>(name);
            propertyBuilder.SetCustomAttribute(attributeBuilder);

            //Add nullable attribute
            if (Nullable.GetUnderlyingType(properType) != null)
            {
                var nullableAttributeConstructor = typeof(NullableSchemaAttribute).GetConstructor(new Type[] { });
                var nullableAttributeBuilder = new CustomAttributeBuilder(nullableAttributeConstructor, new string[] { }, new PropertyInfo[] { }, new object[] { });

                propertyBuilder.SetCustomAttribute(nullableAttributeBuilder);
            }

            // Define field
            FieldBuilder fieldBuilder = typeBuilder.DefineField(name, properType, FieldAttributes.Public);

            // Define "getter" for property
            MethodBuilder getterBuilder = typeBuilder.DefineMethod("get_" + name,
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                properType,
                Type.EmptyTypes);
            ILGenerator getterIL = getterBuilder.GetILGenerator();
            getterIL.Emit(OpCodes.Ldarg_0);
            getterIL.Emit(OpCodes.Ldfld, fieldBuilder);
            getterIL.Emit(OpCodes.Ret);


            // Define "setter" for property
            MethodBuilder setterBuilder = typeBuilder.DefineMethod("set_" + name,
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                null,
                new Type[] { properType });
            ILGenerator setterIL = setterBuilder.GetILGenerator();
            setterIL.Emit(OpCodes.Ldarg_0);
            setterIL.Emit(OpCodes.Ldarg_1);
            setterIL.Emit(OpCodes.Stfld, fieldBuilder);
            setterIL.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getterBuilder);
            propertyBuilder.SetSetMethod(setterBuilder);


            return typeBuilder;
        }

        private static CustomAttributeBuilder GenerateCustomAttributeBuilder<T>(string name)
        {
            var attributeConstructor = typeof(T).GetConstructor(new Type[] { });
            var attributeProperties = typeof(T).GetProperties();
            var attributeBuilder = new CustomAttributeBuilder(attributeConstructor, new string[] { }, attributeProperties.Where(p => p.Name == "Name").ToArray(), new object[] { name });

            return attributeBuilder;
        }
    }
}