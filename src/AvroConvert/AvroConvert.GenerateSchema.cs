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
                AssemblyBuilderAccess.RunAndCollect);

            _moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);

            object inMemoryInstance = DecorateObjectWithAvroAttributes(obj.GetType());

            //invoke Create method of AvroSerializer
            var createMethod = typeof(AvroSerializer).GetMethod("Create", new Type[0]);
            var createGenericMethod = createMethod.MakeGenericMethod(inMemoryInstance.GetType());
            dynamic avroSerializer = createGenericMethod.Invoke(inMemoryInstance, null);

            string result = avroSerializer.GetType().GetProperty("WriterSchema").GetValue(avroSerializer, null).ToString();

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

            if (typeof(IDictionary).IsAssignableFrom(objType))
            {
                return Activator.CreateInstance(objType);
            }
            else
            {
                PropertyInfo[] properties = objType.GetProperties();
                foreach (var prop in properties)
                {
                    Type properType = prop.PropertyType;

                    if (typeof(IList).IsAssignableFrom(properType))
                    {
                        var field = properType.GetProperties()[2];
                        var avroFieldType = DecorateObjectWithAvroAttributes(field.PropertyType).GetType();

                        var avroArray = Array.CreateInstance(avroFieldType, 1);
                        properType = avroArray.GetType();

                        typeBuilder = AddPropertyToTypeBuilder(typeBuilder, properType, prop.Name);
                    }
                    else if (properType == typeof(Guid))
                    {
                        properType = typeof(string);
                        typeBuilder = AddPropertyToTypeBuilder(typeBuilder, properType, prop.Name);
                    }
                    else
                    {
                        typeBuilder = AddPropertyToTypeBuilder(typeBuilder, properType, prop.Name);
                    }
                }
            }

            var attributeBuilder = GenerateCustomAttributeBuilder<DataContractAttribute>(objType.Name);
            typeBuilder.SetCustomAttribute(attributeBuilder);

            var inMemoryType = typeBuilder.CreateType();
            var inMemoryInstance = Activator.CreateInstance(inMemoryType);

            return inMemoryInstance;
        }

        private static TypeBuilder AddPropertyToTypeBuilder(TypeBuilder typeBuilder, Type properType, string name)
        {
            if (typeof(IList).IsAssignableFrom(properType) && !typeof(IDictionary).IsAssignableFrom(properType))
            {
                //Do not add properties of IList
            }

            //if complex type - use recurrence
            else if (!(properType.GetTypeInfo().IsValueType ||
            properType == typeof(string)))
            {
                properType = DecorateObjectWithAvroAttributes(properType).GetType();
            }

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