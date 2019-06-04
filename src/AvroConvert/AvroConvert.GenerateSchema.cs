namespace AvroConvert
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Runtime.Serialization;
    using Attributes;
    using AvroSerializerSettings = BuildSchema.AvroSerializerSettings;
    using ReflectionSchemaBuilder = BuildSchema.ReflectionSchemaBuilder;

    public static partial class AvroConvert
    {
        private static ModuleBuilder _moduleBuilder;

        public static string GenerateSchema(object obj)
        {
            var assemblyName = new AssemblyName("InMemory");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName,
                AssemblyBuilderAccess.Run);

            _moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);

            Type inMemoryType = ConvertToAvroType(obj.GetType());

            var reader = new ReflectionSchemaBuilder(new AvroSerializerSettings()).BuildSchema(inMemoryType);

            return reader.ToString();
        }

        private static Type ConvertToAvroType(Type objType)
        {
            var existingType = _moduleBuilder.GetType(objType.Name);
            if (existingType != null)
            {
                return existingType;
            }

            if (objType.IsArray && objType.FullName.EndsWith("[]"))
            {
                string fullName = objType.FullName.Substring(0, objType.FullName.Length - 2);
                var field = Type.GetType($"{fullName},{objType.Assembly.GetName().Name}");

                var avroFieldType = ConvertToAvroType(field);

                var avroArray = Array.CreateInstance(avroFieldType, 1);
                objType = avroArray.GetType();
            }

            else if (typeof(IList).IsAssignableFrom(objType))
            {
                var field = objType.GetProperties()[2];
                var avroFieldType = ConvertToAvroType(field.PropertyType);

                var avroArray = Array.CreateInstance(avroFieldType, 1);
                objType = avroArray.GetType();
            }

            else if (typeof(IDictionary).IsAssignableFrom(objType))
            {
                Type keyType = objType.GetGenericArguments()[0];
                Type valueType = objType.GetGenericArguments()[1];

                objType = typeof(Dictionary<,>).MakeGenericType(ConvertToAvroType(keyType), ConvertToAvroType(valueType));
            }

            else if (objType == typeof(Guid))
            {
                objType = typeof(string);
            }

            else if (!(objType.GetTypeInfo().IsValueType ||
                       objType == typeof(string)))
            {
                //complex type 
                TypeBuilder typeBuilder = _moduleBuilder.DefineType(objType.Name, TypeAttributes.Public);

                PropertyInfo[] properties = objType.GetProperties();
                foreach (var prop in properties)
                {
                    var propertyType = prop.PropertyType;
                    propertyType = ConvertToAvroType(propertyType);
                    typeBuilder = AddPropertyToTypeBuilder(typeBuilder, propertyType, prop.Name);
                }

                var attributeBuilder = GenerateCustomAttributeBuilder<DataContractAttribute>(objType.Name);
                typeBuilder.SetCustomAttribute(attributeBuilder);

                objType = typeBuilder.CreateTypeInfo();
            }

            else
            {
                //simple type
            }

            return objType;
        }



        private static TypeBuilder AddPropertyToTypeBuilder(TypeBuilder typeBuilder, Type propertyType, string name)
        {
            //mimic property 
            PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(name, PropertyAttributes.None, propertyType, null);

            var attributeBuilder = GenerateCustomAttributeBuilder<DataMemberAttribute>(name);
            propertyBuilder.SetCustomAttribute(attributeBuilder);

            //Add nullable attribute
            if (Nullable.GetUnderlyingType(propertyType) != null || propertyType == typeof(string))
            {
                var nullableAttributeConstructor = typeof(NullableSchemaAttribute).GetConstructor(new Type[] { });
                var nullableAttributeBuilder = new CustomAttributeBuilder(nullableAttributeConstructor, new string[] { }, new PropertyInfo[] { }, new object[] { });

                propertyBuilder.SetCustomAttribute(nullableAttributeBuilder);
            }

            // Define field
            FieldBuilder fieldBuilder = typeBuilder.DefineField(name, propertyType, FieldAttributes.Public);

            // Define "getter" for property
            MethodBuilder getterBuilder = typeBuilder.DefineMethod("get_" + name,
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                propertyType,
                Type.EmptyTypes);
            ILGenerator getterIL = getterBuilder.GetILGenerator();
            getterIL.Emit(OpCodes.Ldarg_0);
            getterIL.Emit(OpCodes.Ldfld, fieldBuilder);
            getterIL.Emit(OpCodes.Ret);


            // Define "setter" for property
            MethodBuilder setterBuilder = typeBuilder.DefineMethod("set_" + name,
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                null,
                new Type[] { propertyType });
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