using System.Collections.Generic;
using AwesomeAssertions;
using NUnit.Framework;
using SolTechnology.Avro;
using SolTechnology.Avro.Infrastructure.Attributes;

namespace AvroConvertComponentTests.FullSerializationAndDeserialization
{
    public class RecursiveTypesTests
    {
        public class Node
        {
            public int Value { get; set; }
            public Node Next { get; set; }
            public List<Node> Children { get; set; } = new();
        }

        public class Parent
        {
            public string Name { get; set; }
            public Child Child { get; set; }
        }

        public class Child
        {
            public int Id { get; set; }
            public Parent Parent { get; set; }
        }

        private static Node Tree() => new()
        {
            Value = 1,
            Next = new Node { Value = 2, Next = new Node { Value = 3 } },
            Children = { new Node { Value = 10, Children = { new Node { Value = 11 } } }, new Node { Value = 20 } }
        };

        [Test]
        public void Schema_of_self_referencing_type_refers_to_itself_by_name()
        {
            var schema = AvroSchema.Create<Node>().Json;

            schema.Should().Be(
                @"{""name"":""Node"",""namespace"":""AvroConvertComponentTests.FullSerializationAndDeserialization"",""type"":""record"",""fields"":[" +
                @"{""name"":""Value"",""type"":""int""}," +
                @"{""name"":""Next"",""type"":[""null"",""AvroConvertComponentTests.FullSerializationAndDeserialization.Node""]}," +
                @"{""name"":""Children"",""type"":{""type"":""array"",""items"":""AvroConvertComponentTests.FullSerializationAndDeserialization.Node""}}]}");
        }

        [Test]
        public void Schema_of_mutually_referencing_types_terminates()
        {
            var schema = AvroSchema.Create<Parent>().Json;

            schema.Should().Contain(@"{""name"":""Parent"",""type"":[""null"",""AvroConvertComponentTests.FullSerializationAndDeserialization.Parent""]}");
        }

        [Test]
        public void Self_referencing_type([ValueSource(typeof(Formats), nameof(Formats.Core))] Format format)
        {
            var tree = Tree();

            var result = RoundTrip.Run<Node>(tree, format);

            result.Should().BeEquivalentTo(tree);
        }

        [Test]
        public void Mutually_referencing_types([ValueSource(typeof(Formats), nameof(Formats.Core))] Format format)
        {
            var value = new Parent { Name = "a1", Child = new Child { Id = 7, Parent = new Parent { Name = "a2", Child = new Child { Id = 8 } } } };

            var result = RoundTrip.Run<Parent>(value, format);

            result.Should().BeEquivalentTo(value);
        }

        [Test]
        public void Collection_of_self_referencing_type([ValueSource(typeof(Formats), nameof(Formats.Core))] Format format)
        {
            var list = new List<Node> { Tree(), new Node { Value = 5 } };

            var result = RoundTrip.Run<List<Node>>(list, format);

            result.Should().BeEquivalentTo(list);
        }

        public class IntKeyed
        {
            public int Value { get; set; }
            public Dictionary<int, IntKeyed> Children { get; set; } = new();
        }

        public class OtherIntKeyed
        {
            public string Name { get; set; }
            public Dictionary<int, OtherIntKeyed> Children { get; set; } = new();
        }

        public class TwoRecursiveDictionaries
        {
            public IntKeyed First { get; set; }
            public OtherIntKeyed Second { get; set; }
        }

        public class UnionNode
        {
            public int Value { get; set; }

            [AvroUnion(typeof(UnionNode))]
            public object Next { get; set; }
        }

        [Test]
        public void Recursion_through_dictionary_with_non_string_keys([ValueSource(typeof(Formats), nameof(Formats.Core))] Format format)
        {
            var value = new Dictionary<int, IntKeyed> { [1] = new IntKeyed { Value = 1, Children = { [2] = new IntKeyed { Value = 2 } } } };

            var result = RoundTrip.Run<Dictionary<int, IntKeyed>>(value, format);

            result.Should().BeEquivalentTo(value);
        }

        [Test]
        public void Two_recursive_dictionaries_with_the_same_key_type([ValueSource(typeof(Formats), nameof(Formats.Core))] Format format)
        {
            var value = new TwoRecursiveDictionaries
            {
                First = new IntKeyed { Value = 1, Children = { [2] = new IntKeyed { Value = 2 } } },
                Second = new OtherIntKeyed { Name = "a", Children = { [3] = new OtherIntKeyed { Name = "b" } } }
            };

            var result = RoundTrip.Run<TwoRecursiveDictionaries>(value, format);

            result.Should().BeEquivalentTo(value);
        }

        [Test]
        public void Recursion_through_avro_union_attribute([ValueSource(typeof(Formats), nameof(Formats.Core))] Format format)
        {
            var value = new UnionNode { Value = 1, Next = new UnionNode { Value = 2 } };

            var result = RoundTrip.Run<UnionNode>(value, format);

            result.Value.Should().Be(1);
            result.Next.Should().BeOfType<UnionNode>().Which.Value.Should().Be(2);
            ((UnionNode)result.Next).Next.Should().BeNull();
        }
    }
}
