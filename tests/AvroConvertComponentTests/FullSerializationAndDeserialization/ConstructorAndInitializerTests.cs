using System;
using System.Collections.Generic;
using AwesomeAssertions;
using NUnit.Framework;
using SolTechnology.Avro;
using SolTechnology.Avro.Policies;

namespace AvroConvertComponentTests.FullSerializationAndDeserialization
{
    /// <summary>Read objects are created with their parameterless constructor, as in System.Text.Json.</summary>
    public class ConstructorAndInitializerTests
    {
        public class V1
        {
            public int Id { get; set; }
        }

        public class WithInitializers
        {
            public int Id { get; set; }
            public List<string> Tags { get; set; } = new();
            public string Status { get; set; } = "New";
            public int Priority { get; set; } = 3;
        }

        public class CountsConstructorCalls
        {
            public static int Calls;

            public CountsConstructorCalls()
            {
                Calls++;
            }

            public int Id { get; set; }
        }

        public class PrivateConstructor
        {
            private PrivateConstructor()
            {
                Status = "set by constructor";
            }

            public PrivateConstructor(int id) : this()
            {
                Id = id;
            }

            public int Id { get; set; }
            public string Status { get; set; }
        }

        public record PrimaryConstructorOnly(int Id, string Name)
        {
            public string Extra { get; init; } = "initializer";
        }

        public class ThrowingConstructor
        {
            public ThrowingConstructor() => throw new InvalidOperationException("Use the other constructor");

            public ThrowingConstructor(int id) => Id = id;

            public int Id { get; set; }
        }

        [Test]
        public void Exception_from_public_constructor_is_reported()
        {
            var avro = AvroConvert.Serialize(new ThrowingConstructor(1));

            var act = () => AvroConvert.Deserialize<ThrowingConstructor>(avro);

            act.Should().Throw<Exception>().Where(e => e.ToString().Contains("Use the other constructor"));
        }

        [Test]
        public void Field_missing_in_data_keeps_initializer()
        {
            var options = new AvroConvertOptions { MissingFieldHandling = AvroMissingFieldHandling.UseDefault };

            var result = AvroConvert.Deserialize<WithInitializers>(AvroConvert.Serialize(new V1 { Id = 1 }), options);

            result.Id.Should().Be(1);
            result.Tags.Should().BeEmpty();
            result.Status.Should().Be("New");
            result.Priority.Should().Be(3);
        }

        [Test]
        public void Field_missing_in_headless_data_keeps_initializer()
        {
            var writer = AvroSchema.Create<V1>();
            var options = new AvroConvertOptions { MissingFieldHandling = AvroMissingFieldHandling.UseDefault };

            var result = AvroConvert.DeserializeHeadless<WithInitializers>(AvroConvert.SerializeHeadless(new V1 { Id = 1 }, writer), writer, AvroSchema.Create<WithInitializers>(), options);

            result.Tags.Should().BeEmpty();
            result.Status.Should().Be("New");
        }

        [Test]
        public void Field_present_in_data_overrides_initializer()
        {
            var value = new WithInitializers { Id = 2, Tags = new List<string> { "a" }, Status = null, Priority = 0 };
            var schema = @"{""type"":""record"",""name"":""WithInitializers"",""fields"":[
                {""name"":""Id"",""type"":""int""},
                {""name"":""Tags"",""type"":{""type"":""array"",""items"":""string""}},
                {""name"":""Status"",""type"":[""null"",""string""]},
                {""name"":""Priority"",""type"":""int""}]}";

            var result = AvroConvert.DeserializeHeadless<WithInitializers>(AvroConvert.SerializeHeadless(value, schema), schema);

            result.Should().BeEquivalentTo(value);
        }

        [Test]
        public void Explicit_null_default_overrides_initializer()
        {
            var writer = @"{""type"":""record"",""name"":""WithInitializers"",""fields"":[{""name"":""Id"",""type"":""int""}]}";
            var reader = @"{""type"":""record"",""name"":""WithInitializers"",""fields"":[
                {""name"":""Id"",""type"":""int""},
                {""name"":""Status"",""type"":[""null"",""string""],""default"":null}]}";

            var result = AvroConvert.DeserializeHeadless<WithInitializers>(AvroConvert.SerializeHeadless(new V1 { Id = 4 }, writer), AvroSchema.Parse(writer), AvroSchema.Parse(reader));

            result.Status.Should().BeNull();
            result.Tags.Should().BeEmpty();
        }

        [Test]
        public void Constructor_runs_once_per_object()
        {
            var items = new List<CountsConstructorCalls> { new() { Id = 1 }, new() { Id = 2 }, new() { Id = 3 } };
            var avro = AvroConvert.Serialize(items);
            CountsConstructorCalls.Calls = 0;

            AvroConvert.Deserialize<List<CountsConstructorCalls>>(avro);

            CountsConstructorCalls.Calls.Should().Be(3);
        }

        [Test]
        public void Non_public_parameterless_constructor_is_not_called()
        {
            var result = RoundTrip.Run<PrivateConstructor>(new V1 { Id = 5 }, Format.Container, new AvroConvertOptions { MissingFieldHandling = AvroMissingFieldHandling.UseDefault });

            result.Id.Should().Be(5);
            result.Status.Should().BeNull();
        }

        [Test]
        public void Type_without_parameterless_constructor_is_created_uninitialized([ValueSource(typeof(Formats), nameof(Formats.Core))] Format format)
        {
            var value = new PrimaryConstructorOnly(6, "six") { Extra = "set" };

            var result = RoundTrip.Run<PrimaryConstructorOnly>(value, format);

            result.Should().BeEquivalentTo(value);
        }
    }
}
