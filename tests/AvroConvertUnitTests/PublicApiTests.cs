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
                return TypeName(type.GetElementType());
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

        private static string Signature(MethodInfo method) =>
            $"{method.Name}{(method.IsGenericMethod ? "<T>" : "")}({string.Join(", ", method.GetParameters().Select(p => TypeName(p.ParameterType)))})";

        private static readonly string[] Visible =
        {
            "Avro2Json(Byte[])",
            "Avro2Json(Byte[], AvroSchema)",
            "Avro2JsonEncoding(Byte[])",
            "Avro2JsonEncoding(Byte[], AvroSchema)",
            "Deserialize(Byte[], Type)",
            "Deserialize(Byte[], Type, AvroConvertOptions)",
            "Deserialize<T>(Byte[])",
            "Deserialize<T>(Byte[], AvroConvertOptions)",
            "Deserialize<T>(ReadOnlySpan<Byte>)",
            "Deserialize<T>(Stream, AvroConvertOptions, CancellationToken)",
            "DeserializeHeadless(Byte[], AvroSchema, Type, AvroSchema, AvroConvertOptions, Nullable<Int32>)",
            "DeserializeHeadless(Byte[], Type)",
            "DeserializeHeadless<T>(Byte[])",
            "DeserializeHeadless<T>(Byte[], AvroSchema, AvroSchema, AvroConvertOptions, Nullable<Int32>)",
            "DeserializeHeadless<T>(ReadOnlySpan<Byte>, AvroSchema, AvroSchema, AvroConvertOptions)",
            "GenerateModel(AvroSchema)",
            "GenerateModel(Byte[])",
            "GenerateSchema(Type)",
            "GenerateSchema(Type, AvroConvertOptions)",
            "GetMetadata(Byte[])",
            "GetMetadata(Stream)",
            "GetSchema(Byte[])",
            "GetSchema(Stream)",
            "Json2Avro(String)",
            "Json2Avro(String, AvroConvertOptions)",
            "Json2Avro<T>(String)",
            "Json2Avro<T>(String, AvroConvertOptions)",
            "JsonEncoding2Avro(String, AvroSchema, AvroConvertOptions)",
            "Merge<T>(IEnumerable<Byte[]>)",
            "OpenDeserializer<T>(Stream)",
            "Serialize(Object)",
            "Serialize(Object, AvroConvertOptions)",
            "Serialize(Object, AvroSchema, AvroConvertOptions, IReadOnlyDictionary<String, AvroMetadataValue>)",
            "Serialize(Object, IBufferWriter<Byte>, AvroConvertOptions)",
            "Serialize(Object, IBufferWriter<Byte>, AvroSchema, AvroConvertOptions, IReadOnlyDictionary<String, AvroMetadataValue>)",
            "Serialize<T>(IEnumerable<T>, Stream, AvroConvertOptions)",
            "Serialize<T>(IEnumerable<T>, Stream, AvroSchema, AvroConvertOptions, IReadOnlyDictionary<String, AvroMetadataValue>)",
            "Serialize<T>(IAsyncEnumerable<T>, Stream, AvroConvertOptions, CancellationToken)",
            "Serialize<T>(IAsyncEnumerable<T>, Stream, AvroSchema, AvroConvertOptions, IReadOnlyDictionary<String, AvroMetadataValue>, CancellationToken)",
            "SerializeHeadless(Object, AvroSchema, AvroConvertOptions)",
            "SerializeHeadless(Object, IBufferWriter<Byte>, AvroSchema, AvroConvertOptions)",
            "SerializeHeadless(Object, Type, AvroConvertOptions)"
        };

        /// <summary>Hidden from IntelliSense, not removed: code compiled against older versions keeps working.</summary>
        private static readonly string[] Hidden =
        {
            "Avro2Json(Byte[], String)",
            "DeserializeHeadless(Byte[], String, String, Type)",
            "DeserializeHeadless(Byte[], String, Type)",
            "DeserializeHeadless<T>(Byte[], String)",
            "DeserializeHeadless<T>(Byte[], String, Int32)",
            "DeserializeHeadless<T>(Byte[], String, String, Int32)",
            "DeserializeHeadless<T>(ReadOnlySpan<Byte>, String)",
            "GenerateModel(String)",
            "GenerateSchema(Type, Boolean)",
            "Json2Avro(String, CodecType)",
            "Json2Avro<T>(String, CodecType)",
            "Serialize(Object, CodecType)",
            "SerializeHeadless(Object, String, AvroConvertOptions)"
        };

        /// <summary>Methods of 3.x removed in 4.0. AvroConvert does not take file paths: the caller opens the file.</summary>
        private static readonly string[] RemovedFromVersion3 =
        {
            "GetSchema(String)"
        };

        /// <summary>The public methods of 3.x.</summary>
        private static readonly string[] Version3 =
        {
            "Avro2Json(Byte[])",
            "Avro2Json(Byte[], String)",
            "Deserialize(Byte[], Type)",
            "Deserialize<T>(Byte[])",
            "Deserialize<T>(Byte[], AvroConvertOptions)",
            "Deserialize<T>(ReadOnlySpan<Byte>)",
            "DeserializeHeadless(Byte[], String, String, Type)",
            "DeserializeHeadless(Byte[], String, Type)",
            "DeserializeHeadless(Byte[], Type)",
            "DeserializeHeadless<T>(Byte[])",
            "DeserializeHeadless<T>(Byte[], String)",
            "DeserializeHeadless<T>(Byte[], String, Int32)",
            "DeserializeHeadless<T>(Byte[], String, String, Int32)",
            "DeserializeHeadless<T>(ReadOnlySpan<Byte>, String)",
            "GenerateModel(Byte[])",
            "GenerateModel(String)",
            "GenerateSchema(Type)",
            "GenerateSchema(Type, AvroConvertOptions)",
            "GenerateSchema(Type, Boolean)",
            "GetSchema(Byte[])",
            "GetSchema(Stream)",
            "GetSchema(String)",
            "Json2Avro(String)",
            "Json2Avro(String, CodecType)",
            "Json2Avro<T>(String)",
            "Json2Avro<T>(String, CodecType)",
            "Merge<T>(IEnumerable<Byte[]>)",
            "OpenDeserializer<T>(Stream)",
            "Serialize(Object)",
            "Serialize(Object, AvroConvertOptions)",
            "Serialize(Object, CodecType)",
            "SerializeHeadless(Object, String, AvroConvertOptions)",
            "SerializeHeadless(Object, Type, AvroConvertOptions)"
        };

        /// <summary>
        /// What the Kafka and Http packages of 4.0.0-preview.3 call. They depend on AvroConvert with a lower bound only, so they
        /// can meet a newer AvroConvert at runtime.
        /// </summary>
        private static readonly string[] CalledByPreviewPackages =
        {
            "Deserialize(Byte[], Type, AvroConvertOptions)",
            "Deserialize<T>(Byte[])",
            "Deserialize<T>(Byte[], AvroConvertOptions)",
            "Deserialize<T>(Stream, AvroConvertOptions, CancellationToken)",
            "DeserializeHeadless<T>(ReadOnlySpan<Byte>, AvroSchema, AvroSchema, AvroConvertOptions)",
            "Serialize(Object)",
            "Serialize(Object, AvroConvertOptions)",
            "Serialize(Object, IBufferWriter<Byte>, AvroConvertOptions)",
            "Serialize<T>(IAsyncEnumerable<T>, Stream, AvroConvertOptions, CancellationToken)",
            "SerializeHeadless(Object, IBufferWriter<Byte>, AvroSchema, AvroConvertOptions)"
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
            Methods.Select(Signature).Should().Contain(Version3.Except(RemovedFromVersion3));
        }

        [Test]
        public void Methods_removed_from_3x_are_gone()
        {
            Methods.Select(Signature).Should().NotContain(RemovedFromVersion3);
        }

        [Test]
        public void Preview_packages_still_bind()
        {
            Methods.Select(Signature).Should().Contain(CalledByPreviewPackages);
        }
    }
}
