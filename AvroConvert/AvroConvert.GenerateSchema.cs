namespace AvroConvert
{
    using FastDeepCloner;
    using Microsoft.Hadoop.Avro;
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Runtime.Serialization;

    public static partial class AvroConvert
    {
        public static string GenerateSchema(object obj)
        {
            object lól = SthWorkinmg(obj.GetType());

            object inMemoryInstance = AddAvroRequiredAttributesToObject(obj.GetType());

            var createMethod = typeof(AvroSerializer).GetMethod("Create", new Type[0]);
            var createGenericMethod = createMethod.MakeGenericMethod(inMemoryInstance.GetType());
            dynamic avroSerializer = createGenericMethod.Invoke(inMemoryInstance, null);

            string result = avroSerializer.GetType().GetProperty("WriterSchema").GetValue(avroSerializer, null).ToString();

            return result;
        }

        private static object AddAvroRequiredAttributesToObject(Type objType)
        {
            var inMemoryInstance = AddCustomAttributeToObject<DataContractAttribute>(objType);

            PropertyInfo[] properties = inMemoryInstance.GetType().GetProperties();
            var clonerProperties = DeepCloner.GetFastDeepClonerProperties(inMemoryInstance.GetType());

            //            var prop = 
            //            prop.Attributes.Add(new JsonIgnoreAttribute());

            foreach (var prop in properties)
            {
                //    var clonedAttribute = clonerProperties.Single(n => n.Name == prop.Name);
                //    clonedAttribute.Attributes.Add(new DataMemberAttribute());
                //    prop.SetValue(inMemoryInstance, AddCustomAttributeToObject<DataMemberAttribute>(prop.PropertyType));
                // prop.SetValue(inMemoryInstance, clonedAttribute.GetValue());

                //  properties.Append(AddCustomAttributeToObject<DataMemberAttribute>(prop.PropertyType).GetType().);
                //   prop.Attributes.Add(new DataMemberAttribute());

                if (!(prop.PropertyType.GetTypeInfo().IsValueType ||
                      prop.PropertyType == typeof(string)))
                {
                    //Its complex type

                    prop.SetValue(inMemoryInstance, AddCustomAttributeToObject<DataContractAttribute>(prop.PropertyType));
                    prop.SetValue(inMemoryInstance, AddAvroRequiredAttributesToObject(prop.PropertyType));
                }


                //  PropertyInfo originalProp = inMemoryInstance.GetType().GetProperty(prop.Name);
                //     PropertyInfo originalProp = inMemoryInstance.GetType().GetProperty(prop.Name);

                //     inMemoryInstance.GetType().pro

                //     originalProp.SetValue(inMemoryInstance, prop.GetValue(inMemoryInstance), null);
            }


            return inMemoryInstance;
        }



        private static object AddCustomAttributeToObject<T>(Type objType)
        {
            var assemblyName = new System.Reflection.AssemblyName("InMemory");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName,
                AssemblyBuilderAccess.Run);

            var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);

            TypeBuilder typeBuilder;

            if (objType.GetTypeInfo().IsValueType ||
                objType == typeof(string))
            {
                var assembly = typeof(string).Assembly;

                var sth = objType.Attributes;


                //                var valueBase = assembly.GetType("string");
                //                  valueBase.typea &= ~TypeAttributes.Sealed;

                typeBuilder =
                    moduleBuilder.DefineType(objType.Name, System.Reflection.TypeAttributes.Public);
            }
            else
            {
                typeBuilder =
                    moduleBuilder.DefineType(objType.Name, System.Reflection.TypeAttributes.Public);
            }

            PropertyInfo[] properties = objType.GetProperties();
            foreach (var prop in properties)
            {
                typeBuilder.DefineProperty(prop.Name, PropertyAttributes.None, typeof(void), Type.EmptyTypes);
                //                typeBuilder.defa
                typeBuilder.DefineField(prop.Name,
                    typeof(string), FieldAttributes.Public);

            }

            var attributeConstructor = typeof(T).GetConstructor(new Type[] { });
            var attributeProperties = typeof(T).GetProperties();

            var attributeBuilder = new CustomAttributeBuilder(attributeConstructor, new string[] { }, attributeProperties.Where(p => p.Name == "Name").ToArray(), new object[] { objType.Name });

            typeBuilder.SetCustomAttribute(attributeBuilder);

            var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName, CallingConventions.Standard, Type.EmptyTypes);
            var ilGenerator = constructorBuilder.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);


            var inMemoryType = typeBuilder.CreateType();

            var inMemoryInstance = Activator.CreateInstance(inMemoryType);


            return inMemoryInstance;
        }

        private static object SthWorkinmg(Type objType)
        {
            var assemblyName = new System.Reflection.AssemblyName("InMemory");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName,
                AssemblyBuilderAccess.Run);

            var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);
            TypeBuilder typeBuilder = moduleBuilder.DefineType(objType.Name, System.Reflection.TypeAttributes.Public);

            PropertyInfo[] properties = objType.GetProperties();

            foreach (var prop in properties)
            {

                TypeBuilder childBuilder = typeBuilder.DefineNestedType(prop.PropertyType.Name, TypeAttributes.NestedPublic);
                PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(prop.Name, PropertyAttributes.None, childBuilder, null);

                // Define field
                FieldBuilder fieldBuilder = typeBuilder.DefineField(prop.Name, childBuilder, FieldAttributes.Public);
                // Define "getter" for MyChild property
                MethodBuilder getterBuilder = typeBuilder.DefineMethod("get_" + prop.Name,
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                    childBuilder,
                    Type.EmptyTypes);
                ILGenerator getterIL = getterBuilder.GetILGenerator();
                getterIL.Emit(OpCodes.Ldarg_0);
                getterIL.Emit(OpCodes.Ldfld, fieldBuilder);
                getterIL.Emit(OpCodes.Ret);

                // Define "setter" for MyChild property
                MethodBuilder setterBuilder = typeBuilder.DefineMethod("set_" + prop.Name,
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                    null,
                    new Type[] { childBuilder });
                ILGenerator setterIL = setterBuilder.GetILGenerator();
                setterIL.Emit(OpCodes.Ldarg_0);
                setterIL.Emit(OpCodes.Ldarg_1);
                setterIL.Emit(OpCodes.Stfld, fieldBuilder);
                setterIL.Emit(OpCodes.Ret);

                propertyBuilder.SetGetMethod(getterBuilder);
                propertyBuilder.SetSetMethod(setterBuilder);
            }


            var inMemoryType = typeBuilder.CreateType();

            var inMemoryInstance = Activator.CreateInstance(inMemoryType);

            return inMemoryInstance;
        }
    }
}
