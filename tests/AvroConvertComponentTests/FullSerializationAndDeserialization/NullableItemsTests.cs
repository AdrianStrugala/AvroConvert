#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using AwesomeAssertions;
using NUnit.Framework;
using SolTechnology.Avro;

namespace AvroConvertComponentTests.FullSerializationAndDeserialization
{
    /// <summary>Items of arrays and values of maps annotated as nullable (string?, City?) are a union with "null".</summary>
    public class NullableItemsTests
    {
        public class City
        {
            public string Name { get; set; } = "";
        }

        public class WithNullableItems
        {
            public List<string?> Names { get; set; } = new();
            public string?[] Codes { get; set; } = [];
            public IReadOnlyList<City?> Cities { get; set; } = [];
            public Dictionary<string, string?> Labels { get; set; } = new();
            public List<List<string?>> Rows { get; set; } = new();
            public List<string?>? Optional { get; set; }
            public int?[] Numbers { get; set; } = [];
        }

        public class WithNonNullableItems
        {
            public List<string> Names { get; set; } = new();
            public Dictionary<string, string> Labels { get; set; } = new();
            public List<string>? Optional { get; set; }
        }

        [Test]
        public void Nullable_items_and_values_are_a_union_with_null()
        {
            var schema = AvroSchema.Create<WithNullableItems>().Json;

            schema.Should().Contain("{\"name\":\"Names\",\"type\":{\"type\":\"array\",\"items\":[\"null\",\"string\"]}}");
            schema.Should().Contain("{\"name\":\"Codes\",\"type\":{\"type\":\"array\",\"items\":[\"null\",\"string\"]}}");
            schema.Should().Contain("{\"name\":\"Cities\",\"type\":{\"type\":\"array\",\"items\":[\"null\",{");
            schema.Should().Contain("{\"name\":\"Labels\",\"type\":{\"type\":\"map\",\"values\":[\"null\",\"string\"]}}");
            schema.Should().Contain("{\"name\":\"Rows\",\"type\":{\"type\":\"array\",\"items\":{\"type\":\"array\",\"items\":[\"null\",\"string\"]}}}");
            schema.Should().Contain("{\"name\":\"Optional\",\"type\":[\"null\",{\"type\":\"array\",\"items\":[\"null\",\"string\"]}]}");
            schema.Should().Contain("{\"name\":\"Numbers\",\"type\":{\"type\":\"array\",\"items\":[\"null\",\"int\"]}}");
        }

        [Test]
        public void Items_and_values_not_annotated_as_nullable_stay_as_they_are()
        {
            var schema = AvroSchema.Create<WithNonNullableItems>().Json;

            schema.Should().Contain("{\"name\":\"Names\",\"type\":{\"type\":\"array\",\"items\":\"string\"}}");
            schema.Should().Contain("{\"name\":\"Labels\",\"type\":{\"type\":\"map\",\"values\":\"string\"}}");
            schema.Should().Contain("{\"name\":\"Optional\",\"type\":[\"null\",{\"type\":\"array\",\"items\":\"string\"}]}");
        }

        [Test]
        public void Null_items_and_values_round_trip([ValueSource(typeof(Formats), nameof(Formats.Core))] Format format)
        {
            var value = new WithNullableItems
            {
                Names = new() { "a", null },
                Codes = [null, "c"],
                Cities = [new City { Name = "Wroclaw" }, null],
                Labels = new() { ["x"] = null, ["y"] = "z" },
                Rows = new() { new() { null, "r" } },
                Optional = new() { null },
                Numbers = [1, null]
            };

            var result = RoundTrip.Run<WithNullableItems>(value, format);

            result.Should().BeEquivalentTo(value);
        }

        [Test]
        public void Null_item_where_items_are_not_nullable_names_the_items_in_the_error()
        {
            var value = new WithNonNullableItems { Names = new() { "a", null! } };

            var exception = Recorded.Exception(() => AvroConvert.Serialize(value));

            exception.Message.Should().Contain("Field 'WithNonNullableItems.Names[]' is null")
                .And.Contain("Declare the items as nullable in a member of a class (List<string?>, string?[], Dictionary<string, string?>")
                .And.NotContain("Declare the member as nullable");
        }

        [Test]
        public void Null_item_of_a_collection_given_directly_says_it_has_no_annotation()
        {
            var exception = Recorded.Exception(() => AvroConvert.Serialize(new List<string?> { "a", null }));

            // Each item is an entry of the file, a value of its own: no member, so no annotation to declare.
            exception.Message.Should().Contain("A value given directly to Serialize, or an item of a collection given to it, carries no nullable annotation")
                .And.NotContain("Declare the");
        }

        /// <summary>A collection whose items are not its first type argument.</summary>
        public class Bag<TMeta, TItem> : IEnumerable<TItem>
        {
            private readonly List<TItem> _items = new();
            public IEnumerator<TItem> GetEnumerator() => _items.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public class WithOtherCollections
        {
            public Bag<string?, string> NullableMeta { get; set; } = new();
            public Bag<string, string?> NullableItems { get; set; } = new();
            public ImmutableArray<string?>? OptionalTags { get; set; }
        }

        [Test]
        public void Items_take_the_annotation_of_their_own_type_argument()
        {
            var schema = AvroSchema.Create<WithOtherCollections>().Json;

            schema.Should().Contain("{\"name\":\"NullableMeta\",\"type\":{\"type\":\"array\",\"items\":\"string\"}}");
            schema.Should().Contain("{\"name\":\"NullableItems\",\"type\":{\"type\":\"array\",\"items\":[\"null\",\"string\"]}}");
            schema.Should().Contain("{\"name\":\"OptionalTags\",\"type\":[\"null\",{\"type\":\"array\",\"items\":[\"null\",\"string\"]}]}");
        }

        // The compiler puts the nullable context of ContextInner on ContextOuter (most of ContextOuter's members are string?), not on ContextInner.
        public class ContextOuter
        {
            public string? A { get; set; }
            public string? B { get; set; }
            public string? C { get; set; }

            public class ContextInner
            {
                public string? Name { get; set; }
                public List<string?>? Tags { get; set; }
                public string? Other { get; set; }
            }
        }

        [Test]
        public void Members_of_a_class_nested_in_another_take_the_context_of_the_outer_one()
        {
            var schema = AvroSchema.Create<ContextOuter.ContextInner>().Json;

            schema.Should().Contain("{\"name\":\"Name\",\"type\":[\"null\",\"string\"]}");
            schema.Should().Contain("{\"name\":\"Tags\",\"type\":[\"null\",{\"type\":\"array\",\"items\":[\"null\",\"string\"]}]}");
        }

        public class WithIntKeys
        {
            public Dictionary<int, string?> Labels { get; set; } = new();
        }

        [Test]
        public void Null_value_of_a_dictionary_with_other_keys_says_why_the_annotation_does_not_help()
        {
            var exception = Recorded.Exception(() => AvroConvert.Serialize(new WithIntKeys { Labels = new() { [1] = null } }));

            exception.Message.Should().Contain("A dictionary with keys other than string is an array of {Key, Value} records")
                .And.NotContain("Declare the member as nullable");
        }

        public class Page<T>
        {
            public List<T> Items { get; set; } = new();
            public T[] Array { get; set; } = [];
            public Dictionary<string, T> ById { get; set; } = new();
            public List<string?> Notes { get; set; } = new();
            public List<string> Names { get; set; } = new();
        }

        [Test]
        public void Items_of_a_type_parameter_stay_as_they_are()
        {
            // .NET reports T of Page<string> as nullable whether or not it is written T?, so only annotations on concrete types count.
            var schema = AvroSchema.Create<Page<string>>().Json;

            schema.Should().Contain("{\"name\":\"Items\",\"type\":{\"type\":\"array\",\"items\":\"string\"}}");
            schema.Should().Contain("{\"name\":\"Array\",\"type\":{\"type\":\"array\",\"items\":\"string\"}}");
            schema.Should().Contain("{\"name\":\"ById\",\"type\":{\"type\":\"map\",\"values\":\"string\"}}");
            schema.Should().Contain("{\"name\":\"Notes\",\"type\":{\"type\":\"array\",\"items\":[\"null\",\"string\"]}}");
            schema.Should().Contain("{\"name\":\"Names\",\"type\":{\"type\":\"array\",\"items\":\"string\"}}");
        }

        [Test]
        public void Annotations_without_nullable_warnings_are_enough()
        {
            // What a generated model starts with: #nullable enable annotations.
            AvroSchema.Create<AnnotationsOnly>().Json.Should().Contain("\"items\":[\"null\",\"string\"]");
        }

        [Test]
        public void Model_generated_for_nullable_items_gives_back_the_same_schema()
        {
            const string source = @"{""type"":""record"",""name"":""Tagged"",""namespace"":""shop"",""fields"":[
                {""name"":""tags"",""type"":{""type"":""array"",""items"":[""null"",""string""]}},
                {""name"":""labels"",""type"":{""type"":""map"",""values"":[""null"",""string""]}}]}";

            var model = AvroConvert.GenerateModel(source);

            model.Should().StartWith("#nullable enable annotations\r\n\r\n", "string? keeps its annotation in a project without <Nullable>");
            model.Should().Contain("\tpublic string?[] tags { get; set; }\r\n").And.Contain("\tpublic Dictionary<string,string?> labels { get; set; }\r\n");
            AvroSchema.Create<GeneratedTagged>().CanonicalForm.Should().Be(AvroSchema.Parse(source).CanonicalForm);
        }

        // What GenerateModel gives for the schema above.
        [System.Runtime.Serialization.DataContract(Name = "Tagged", Namespace = "shop")]
        public class GeneratedTagged
        {
            [System.Runtime.Serialization.DataMember]
            public string?[] tags { get; set; } = [];

            [System.Runtime.Serialization.DataMember]
            public Dictionary<string, string?> labels { get; set; } = new();
        }
    }

#nullable disable
#nullable enable annotations
    public class AnnotationsOnly
    {
        public List<string?> Names { get; set; }
    }
#nullable restore
}
