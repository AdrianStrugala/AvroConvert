using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using AwesomeAssertions;
using NUnit.Framework;
using SolTechnology.Avro;

namespace AvroConvertUnitTests
{
    /// <summary>
    /// The public surface of <see cref="AvroConvert"/>. A change here is a decision: the visible list is what users are offered,
    /// and the kept lists are what compiled code and older packages bind to.
    /// </summary>
    public class PublicApiTests
    {
        private static readonly MethodInfo[] Methods = typeof(AvroConvert)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => !method.IsSpecialName)
            .ToArray();

        private static bool IsHidden(MethodInfo method) =>
            method.GetCustomAttribute<EditorBrowsableAttribute>()?.State == EditorBrowsableState.Never;

        private static string TypeName(Type type)
        {
            if (type.IsByRef)
            {
                return "ref " + TypeName(type.GetElementType());
            }

            if (type.IsArray)
            {
                return TypeName(type.GetElementType()) + "[]";
            }

            if (!type.IsGenericType)
            {
                return type.Name;
            }

            var name = type.Name.Substring(0, type.Name.IndexOf('`'));
            return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(TypeName))}>";
        }

        // With the return type and in/ref/out: compiled code binds to them too.
        private static string Signature(MethodInfo method) =>
            $"{TypeName(method.ReturnType)} {method.Name}{(method.IsGenericMethod ? "<T>" : "")}({string.Join(", ", method.GetParameters().Select(ParameterName))})";

        private static string ParameterName(ParameterInfo parameter)
        {
            if (!parameter.ParameterType.IsByRef)
            {
                return TypeName(parameter.ParameterType);
            }

            var element = TypeName(parameter.ParameterType.GetElementType());
            return parameter.IsOut ? "out " + element : parameter.IsIn ? "in " + element : "ref " + element;
        }

        private static readonly string[] Visible =
        {
            "String Avro2Json(Byte[])",
            "String Avro2Json(Byte[], AvroSchema)",
            "String Avro2JsonEncoding(Byte[])",
            "String Avro2JsonEncoding(Byte[], AvroSchema)",
            "Object Deserialize(Byte[], Type)",
            "Object Deserialize(Byte[], Type, AvroConvertOptions)",
            "T Deserialize<T>(Byte[])",
            "T Deserialize<T>(Byte[], AvroConvertOptions)",
            "T Deserialize<T>(ReadOnlySpan<Byte>)",
            "IAsyncEnumerable<T> Deserialize<T>(Stream, AvroConvertOptions, CancellationToken)",
            "Object DeserializeHeadless(Byte[], AvroSchema, Type, AvroSchema, AvroConvertOptions, Nullable<Int32>)",
            "Object DeserializeHeadless(Byte[], Type)",
            "T DeserializeHeadless<T>(Byte[])",
            "T DeserializeHeadless<T>(Byte[], AvroSchema, AvroSchema, AvroConvertOptions, Nullable<Int32>)",
            "T DeserializeHeadless<T>(ReadOnlySpan<Byte>, AvroSchema, AvroSchema, AvroConvertOptions)",
            "String GenerateModel(AvroSchema)",
            "String GenerateModel(Byte[])",
            "String GenerateSchema(Type)",
            "String GenerateSchema(Type, AvroConvertOptions)",
            "IReadOnlyDictionary<String, Byte[]> GetMetadata(Byte[])",
            "IReadOnlyDictionary<String, Byte[]> GetMetadata(Stream)",
            "String GetSchema(Byte[])",
            "String GetSchema(Stream)",
            "Byte[] Json2Avro(String)",
            "Byte[] Json2Avro(String, AvroConvertOptions)",
            "Byte[] Json2Avro<T>(String)",
            "Byte[] Json2Avro<T>(String, AvroConvertOptions)",
            "Byte[] JsonEncoding2Avro(String, AvroSchema, AvroConvertOptions)",
            "Byte[] Merge<T>(IEnumerable<Byte[]>)",
            "ILineReader<T> OpenDeserializer<T>(Stream)",
            "Byte[] Serialize(Object)",
            "Byte[] Serialize(Object, AvroConvertOptions)",
            "Byte[] Serialize(Object, AvroSchema, AvroConvertOptions, IReadOnlyDictionary<String, AvroMetadataValue>)",
            "Void Serialize(Object, IBufferWriter<Byte>, AvroConvertOptions)",
            "Void Serialize(Object, IBufferWriter<Byte>, AvroSchema, AvroConvertOptions, IReadOnlyDictionary<String, AvroMetadataValue>)",
            "Void Serialize<T>(IEnumerable<T>, Stream, AvroConvertOptions)",
            "Void Serialize<T>(IEnumerable<T>, Stream, AvroSchema, AvroConvertOptions, IReadOnlyDictionary<String, AvroMetadataValue>)",
            "Task Serialize<T>(IAsyncEnumerable<T>, Stream, AvroConvertOptions, CancellationToken)",
            "Task Serialize<T>(IAsyncEnumerable<T>, Stream, AvroSchema, AvroConvertOptions, IReadOnlyDictionary<String, AvroMetadataValue>, CancellationToken)",
            "Byte[] SerializeHeadless(Object, AvroSchema, AvroConvertOptions)",
            "Void SerializeHeadless(Object, IBufferWriter<Byte>, AvroSchema, AvroConvertOptions)",
            "Byte[] SerializeHeadless(Object, Type, AvroConvertOptions)"
        };

        /// <summary>Hidden from IntelliSense, not removed: code compiled against older versions keeps working.</summary>
        private static readonly string[] Hidden =
        {
            "String Avro2Json(Byte[], String)",
            "Object DeserializeHeadless(Byte[], String, String, Type)",
            "Object DeserializeHeadless(Byte[], String, Type)",
            "T DeserializeHeadless<T>(Byte[], String)",
            "T DeserializeHeadless<T>(Byte[], String, Int32)",
            "T DeserializeHeadless<T>(Byte[], String, String, Int32)",
            "T DeserializeHeadless<T>(ReadOnlySpan<Byte>, String)",
            "String GenerateModel(String)",
            "String GenerateSchema(Type, Boolean)",
            "Byte[] Json2Avro(String, CodecType)",
            "Byte[] Json2Avro<T>(String, CodecType)",
            "String GetSchema(String)",
            "Byte[] Serialize(Object, CodecType)",
            "Byte[] SerializeHeadless(Object, String, AvroConvertOptions)"
        };

        /// <summary>The public methods of 3.x.</summary>
        private static readonly string[] Version3 =
        {
            "String Avro2Json(Byte[])",
            "String Avro2Json(Byte[], String)",
            "Object Deserialize(Byte[], Type)",
            "T Deserialize<T>(Byte[])",
            "T Deserialize<T>(Byte[], AvroConvertOptions)",
            "T Deserialize<T>(ReadOnlySpan<Byte>)",
            "Object DeserializeHeadless(Byte[], String, String, Type)",
            "Object DeserializeHeadless(Byte[], String, Type)",
            "Object DeserializeHeadless(Byte[], Type)",
            "T DeserializeHeadless<T>(Byte[])",
            "T DeserializeHeadless<T>(Byte[], String)",
            "T DeserializeHeadless<T>(Byte[], String, Int32)",
            "T DeserializeHeadless<T>(Byte[], String, String, Int32)",
            "T DeserializeHeadless<T>(ReadOnlySpan<Byte>, String)",
            "String GenerateModel(Byte[])",
            "String GenerateModel(String)",
            "String GenerateSchema(Type)",
            "String GenerateSchema(Type, AvroConvertOptions)",
            "String GenerateSchema(Type, Boolean)",
            "String GetSchema(Byte[])",
            "String GetSchema(Stream)",
            "String GetSchema(String)",
            "Byte[] Json2Avro(String)",
            "Byte[] Json2Avro(String, CodecType)",
            "Byte[] Json2Avro<T>(String)",
            "Byte[] Json2Avro<T>(String, CodecType)",
            "Byte[] Merge<T>(IEnumerable<Byte[]>)",
            "ILineReader<T> OpenDeserializer<T>(Stream)",
            "Byte[] Serialize(Object)",
            "Byte[] Serialize(Object, AvroConvertOptions)",
            "Byte[] Serialize(Object, CodecType)",
            "Byte[] SerializeHeadless(Object, String, AvroConvertOptions)",
            "Byte[] SerializeHeadless(Object, Type, AvroConvertOptions)"
        };

        /// <summary>
        /// What the Kafka and Http packages of 4.0.0-preview.3 call. They depend on AvroConvert with a lower bound only, so they
        /// can meet a newer AvroConvert at runtime.
        /// </summary>
        private static readonly string[] CalledByPreviewPackages =
        {
            "Object Deserialize(Byte[], Type, AvroConvertOptions)",
            "T Deserialize<T>(Byte[])",
            "T Deserialize<T>(Byte[], AvroConvertOptions)",
            "IAsyncEnumerable<T> Deserialize<T>(Stream, AvroConvertOptions, CancellationToken)",
            "T DeserializeHeadless<T>(ReadOnlySpan<Byte>, AvroSchema, AvroSchema, AvroConvertOptions)",
            "Byte[] Serialize(Object)",
            "Byte[] Serialize(Object, AvroConvertOptions)",
            "Void Serialize(Object, IBufferWriter<Byte>, AvroConvertOptions)",
            "Task Serialize<T>(IAsyncEnumerable<T>, Stream, AvroConvertOptions, CancellationToken)",
            "Void SerializeHeadless(Object, IBufferWriter<Byte>, AvroSchema, AvroConvertOptions)"
        };

        [Test]
        public void Visible_methods_are_the_ones_on_the_list()
        {
            Methods.Where(method => !IsHidden(method)).Select(Signature).Should().BeEquivalentTo(Visible);
        }

        [Test]
        public void Hidden_methods_are_the_ones_on_the_list()
        {
            Methods.Where(IsHidden).Select(Signature).Should().BeEquivalentTo(Hidden);
        }

        [Test]
        public void Code_compiled_against_3x_still_binds()
        {
            Methods.Select(Signature).Should().Contain(Version3);
        }

        [Test]
        public void Preview_packages_still_bind()
        {
            Methods.Select(Signature).Should().Contain(CalledByPreviewPackages);
        }
    }
}
