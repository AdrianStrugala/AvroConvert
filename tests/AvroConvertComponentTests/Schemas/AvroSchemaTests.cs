using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using SolTechnology.Avro;
using NUnit.Framework;

namespace AvroConvertComponentTests.Schemas
{
    public class AvroSchemaTests
    {
        public class Order
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        [Test]
        public void Create_MatchesGenerateSchema()
        {
            //Act
            var schema = AvroSchema.Create<Order>();


            //Assert
            schema.Json.Should().Be(AvroConvert.GenerateSchema(typeof(Order)));
            schema.ToString().Should().Be(schema.Json);
            AvroSchema.Create(typeof(Order)).Should().Be(schema);
        }

        [Test]
        public void FromAvro_ReadsHeaderSchema()
        {
            //Arrange
            var avro = AvroConvert.Serialize(new Order { Id = 1, Name = "a" });


            //Act
            var schema = AvroSchema.FromAvro(avro);


            //Assert
            schema.Should().Be(AvroSchema.Create<Order>());
            schema.Json.Should().Be(AvroConvert.GetSchema(avro));
        }

        [Test]
        public void Equality_IgnoresAttributesThatDoNotAffectData()
        {
            //Arrange
            var plain = AvroSchema.Parse(@"{""type"":""record"",""name"":""R"",""namespace"":""n"",""fields"":[{""name"":""a"",""type"":""int""}]}");
            var documented = AvroSchema.Parse(@"{""name"":""n.R"",""type"":""record"",""doc"":""d"",""fields"":[{""name"":""a"",""type"":{""type"":""int""},""default"":0}]}");
            var renamed = AvroSchema.Parse(@"{""type"":""record"",""name"":""R"",""namespace"":""n"",""fields"":[{""name"":""b"",""type"":""int""}]}");


            //Assert
            documented.Should().Be(plain);
            documented.GetHashCode().Should().Be(plain.GetHashCode());
            documented.Fingerprint.Should().Be(plain.Fingerprint);
            renamed.Should().NotBe(plain);
            plain.CanonicalForm.Should().Be(@"{""name"":""n.R"",""type"":""record"",""fields"":[{""name"":""a"",""type"":""int""}]}");
        }

        [Test]
        public void AvroConvertOverloads_AcceptAvroSchema()
        {
            //Arrange
            var schema = AvroSchema.Create<Order>();
            var order = new Order { Id = 7, Name = "seven" };


            //Act
            var headless = AvroConvert.SerializeHeadless(order, schema);


            //Assert
            AvroConvert.DeserializeHeadless<Order>(headless, schema).Should().BeEquivalentTo(order);
            AvroConvert.DeserializeHeadless<Order>(headless, schema, schema).Should().BeEquivalentTo(order);
            AvroConvert.Avro2Json(headless, schema).Should().Be(@"{""Id"":7,""Name"":""seven""}");
            AvroConvert.Avro2JsonEncoding(headless, schema).Should().Be(@"{""Id"":7,""Name"":""seven""}");
            AvroConvert.Deserialize<Order>(AvroConvert.JsonEncoding2Avro(@"{""Id"":7,""Name"":""seven""}", schema)).Should().BeEquivalentTo(order);
            AvroConvert.GenerateModel(schema).Should().Contain("public class Order");
        }

        [Test]
        public void Parse_Empty_Throws()
        {
            var act = () => AvroSchema.Parse(" ");

            act.Should().Throw<ArgumentException>();
        }

        private const string Money = """{"type":"record","name":"Money","namespace":"shared","fields":[{"name":"Amount","type":"double"},{"name":"Currency","type":"shared.Currency"}]}""";
        private const string Currency = """{"type":"enum","name":"Currency","namespace":"shared","symbols":["EUR","PLN"]}""";
        private const string Invoice = """{"type":"record","name":"Invoice","namespace":"billing","fields":[{"name":"Number","type":"string"},{"name":"Total","type":"shared.Money"}]}""";

        public class InvoiceModel
        {
            public string Number { get; set; }
            public MoneyModel Total { get; set; }
        }

        public class MoneyModel
        {
            public double Amount { get; set; }
            public CurrencyModel Currency { get; set; }
        }

        public enum CurrencyModel
        {
            EUR,
            PLN
        }

        [Test]
        public void Parse_WithReferences_InAnyOrder_IsSelfContained()
        {
            //Act: Money (needs Currency) comes before Currency
            var schema = AvroSchema.Parse(Invoice, new[] { Money, Currency });


            //Assert
            schema.Json.Should().Contain("\"name\":\"Money\"").And.Contain("\"symbols\":[\"EUR\",\"PLN\"]");
            AvroSchema.Parse(schema.Json).Should().Be(schema);
        }

        [Test]
        public void Parse_WithReferences_ReadsAndWritesData()
        {
            //Arrange
            var schema = AvroSchema.Parse(Invoice, new[] { Currency, Money });
            var invoice = new InvoiceModel { Number = "FV/1", Total = new MoneyModel { Amount = 199, Currency = CurrencyModel.PLN } };


            //Act
            var bytes = AvroConvert.SerializeHeadless(invoice, schema);
            var result = AvroConvert.DeserializeHeadless<InvoiceModel>(bytes, schema);


            //Assert
            result.Should().BeEquivalentTo(invoice);
        }

        [Test]
        public void Parse_WithAMissingReference_NamesTheType()
        {
            var act = () => AvroSchema.Parse(Invoice, new[] { Money });

            act.Should().Throw<Exception>().WithMessage("*Unknown type 'shared.Currency'*");
        }

        [Test]
        public void Parse_UnknownType_NamesIt()
        {
            var act = () => AvroSchema.Parse("""{"type":"record","name":"R","fields":[{"name":"F","type":"Nope"}]}""");

            act.Should().Throw<Exception>().WithMessage("*Unknown type 'Nope'*");
        }
    }

    public class SchemaEvolutionTests
    {
        private static AvroSchema Record(string fields, string name = "Order") =>
            AvroSchema.Parse($@"{{""type"":""record"",""name"":""{name}"",""namespace"":""shop"",""fields"":[{fields}]}}");

        private const string Id = @"{""name"":""id"",""type"":""int""}";

        [Test]
        public void Compare_SameSchema_IsEquivalent()
        {
            var comparison = AvroSchema.Compare(Record(Id), Record(Id));

            comparison.AreEquivalent.Should().BeTrue();
            comparison.CanRead.Should().BeTrue();
            comparison.Differences.Should().BeEmpty();
        }

        [Test]
        public void Compare_FieldAddedWithoutDefault_IsBreaking()
        {
            //Act
            var comparison = AvroSchema.Compare(
                writer: Record(Id),
                reader: Record(Id + @",{""name"":""note"",""type"":""string""}"));


            //Assert
            comparison.CanRead.Should().BeFalse();
            var difference = comparison.Differences.Should().ContainSingle().Subject;
            difference.Path.Should().Be("Order.note");
            difference.Kind.Should().Be(SchemaDifferenceKind.FieldAdded);
            difference.IsBreaking.Should().BeTrue();
        }

        [Test]
        public void Compare_FieldAddedWithDefaultAndFieldRemoved_AreNotBreaking()
        {
            //Act
            var comparison = AvroSchema.Compare(
                writer: Record(Id + @",{""name"":""old"",""type"":""string""}"),
                reader: Record(Id + @",{""name"":""note"",""type"":""string"",""default"":""""}"));


            //Assert
            comparison.CanRead.Should().BeTrue();
            comparison.AreEquivalent.Should().BeFalse();
            comparison.Differences.Select(d => (d.Path, d.Kind)).Should().BeEquivalentTo(new[]
            {
                ("Order.note", SchemaDifferenceKind.FieldAdded),
                ("Order.old", SchemaDifferenceKind.FieldRemoved)
            });
        }

        [TestCase("int", "long", SchemaDifferenceKind.TypePromoted, false)]
        [TestCase("int", "double", SchemaDifferenceKind.TypePromoted, false)]
        [TestCase("string", "bytes", SchemaDifferenceKind.TypePromoted, false)]
        [TestCase("long", "int", SchemaDifferenceKind.TypeChanged, true)]
        [TestCase("string", "int", SchemaDifferenceKind.TypeChanged, true)]
        public void Compare_PrimitiveChange_FollowsPromotionRules(string writerType, string readerType, SchemaDifferenceKind kind, bool breaking)
        {
            //Act
            var comparison = AvroSchema.Compare(
                Record($@"{{""name"":""v"",""type"":""{writerType}""}}"),
                Record($@"{{""name"":""v"",""type"":""{readerType}""}}"));


            //Assert
            var difference = comparison.Differences.Should().ContainSingle().Subject;
            difference.Kind.Should().Be(kind);
            difference.IsBreaking.Should().Be(breaking);
            difference.Path.Should().Be("Order.v");
        }

        [Test]
        public void Compare_RenamedFieldWithAlias_IsReadable()
        {
            var comparison = AvroSchema.Compare(
                Record(Id),
                Record(@"{""name"":""orderId"",""type"":""int"",""aliases"":[""id""]}"));

            comparison.CanRead.Should().BeTrue();
            comparison.Differences.Should().BeEmpty();
        }

        [Test]
        public void Compare_EnumSymbolRemoved_DependsOnReaderDefault()
        {
            //Arrange
            const string writer = @"{""name"":""s"",""type"":{""type"":""enum"",""name"":""Status"",""symbols"":[""NEW"",""PAID"",""LOST""]}}";
            const string strictReader = @"{""name"":""s"",""type"":{""type"":""enum"",""name"":""Status"",""symbols"":[""NEW"",""PAID""]}}";
            const string tolerantReader = @"{""name"":""s"",""type"":{""type"":""enum"",""name"":""Status"",""symbols"":[""NEW"",""PAID""],""default"":""NEW""}}";


            //Act & Assert
            AvroSchema.Compare(Record(writer), Record(strictReader)).CanRead.Should().BeFalse();
            AvroSchema.Compare(Record(writer), Record(tolerantReader)).CanRead.Should().BeTrue();
            AvroSchema.Compare(Record(strictReader), Record(writer)).Differences
                .Should().ContainSingle(d => d.Kind == SchemaDifferenceKind.SymbolAdded && d.Path == "Order.s");
        }

        [Test]
        public void Compare_Unions_AreMatchedBranchByBranch()
        {
            //Arrange
            var nullableInt = Record(@"{""name"":""v"",""type"":[""null"",""int""]}");
            var nullableLong = Record(@"{""name"":""v"",""type"":[""null"",""long""]}");
            var plainInt = Record(@"{""name"":""v"",""type"":""int""}");


            //Act & Assert
            AvroSchema.Compare(nullableInt, nullableLong).CanRead.Should().BeTrue();
            AvroSchema.Compare(plainInt, nullableInt).CanRead.Should().BeTrue();
            var narrowing = AvroSchema.Compare(nullableInt, plainInt);
            narrowing.CanRead.Should().BeFalse();
            narrowing.Differences.Should().ContainSingle(d => d.Kind == SchemaDifferenceKind.UnionBranchRemoved);
            AvroSchema.Compare(plainInt, nullableLong).Differences.Select(d => d.Kind)
                .Should().BeEquivalentTo(new[] { SchemaDifferenceKind.TypePromoted });
        }

        [Test]
        public void Compare_NestedChanges_ReportPaths()
        {
            //Arrange
            const string writerLines = @"{""name"":""lines"",""type"":{""type"":""array"",""items"":{""type"":""record"",""name"":""Line"",""fields"":[
                {""name"":""price"",""type"":{""type"":""bytes"",""logicalType"":""decimal"",""precision"":10,""scale"":2}},
                {""name"":""tags"",""type"":{""type"":""map"",""values"":""int""}},
                {""name"":""hash"",""type"":{""type"":""fixed"",""name"":""Md5"",""size"":16}}]}}}";
            var readerLines = writerLines
                .Replace(@"""scale"":2", @"""scale"":4")
                .Replace(@"""values"":""int""", @"""values"":""string""")
                .Replace(@"""size"":16", @"""size"":32");


            //Act
            var comparison = AvroSchema.Compare(Record(writerLines), Record(readerLines));


            //Assert
            comparison.Differences.Select(d => $"{d.Path} {d.Kind} {d.IsBreaking}").Should().BeEquivalentTo(
                "Order.lines[].price LogicalTypeChanged True",
                "Order.lines[].tags{} TypeChanged True",
                "Order.lines[].hash SizeChanged True");
        }

        [Test]
        public void Compare_RecursiveRecord_Terminates()
        {
            var node = AvroSchema.Parse(@"{""type"":""record"",""name"":""Node"",""fields"":[{""name"":""next"",""type"":[""null"",""Node""]}]}");

            AvroSchema.Compare(node, node).CanRead.Should().BeTrue();
        }

        [Test]
        public void Compare_LogicalTypeOnly_IsReportedButNotBreaking()
        {
            var comparison = AvroSchema.Compare(
                Record(@"{""name"":""t"",""type"":{""type"":""long"",""logicalType"":""timestamp-millis""}}"),
                Record(@"{""name"":""t"",""type"":{""type"":""long"",""logicalType"":""timestamp-micros""}}"));

            comparison.CanRead.Should().BeTrue();
            comparison.Differences.Should().ContainSingle(d => d.Kind == SchemaDifferenceKind.LogicalTypeChanged && !d.IsBreaking);
        }

        [Test]
        public void Compare_RecordRenamed_IsBreakingUnlessAliased()
        {
            AvroSchema.Compare(Record(Id), Record(Id, "Purchase")).Differences
                .Should().ContainSingle(d => d.Kind == SchemaDifferenceKind.NameChanged && d.IsBreaking);

            var aliased = AvroSchema.Parse(@"{""type"":""record"",""name"":""Purchase"",""namespace"":""shop"",""aliases"":[""Order""],""fields"":[" + Id + "]}");
            AvroSchema.Compare(Record(Id), aliased).CanRead.Should().BeTrue();
        }

        [TestCase(AvroCompatibilityMode.Backward, true)]
        [TestCase(AvroCompatibilityMode.Forward, false)]
        [TestCase(AvroCompatibilityMode.Full, false)]
        public void CheckCompatibility_FieldAddedWithDefault(AvroCompatibilityMode mode, bool compatible)
        {
            //Arrange: next widens "id" to long and adds "note" with a default – it reads old data, but old readers cannot read long.
            var previous = Record(Id);
            var next = Record(@"{""name"":""id"",""type"":""long""},{""name"":""note"",""type"":""string"",""default"":""""}");


            //Act
            var result = AvroSchema.CheckCompatibility(previous, next, mode);


            //Assert
            result.IsCompatible.Should().Be(compatible);
            result.Mode.Should().Be(mode);
        }

        [Test]
        public void CheckCompatibility_Transitive_ChecksWholeHistory()
        {
            //Arrange: v2 dropped "legacy" (string); v3 re-adds it as int – fine against v2, but v1 data holds strings there.
            var v1 = Record(Id + @",{""name"":""legacy"",""type"":""string""}");
            var v2 = Record(Id);
            var v3 = Record(Id + @",{""name"":""legacy"",""type"":""int"",""default"":0}");
            var history = new List<AvroSchema> { v1, v2 };


            //Act
            var latestOnly = AvroSchema.CheckCompatibility(history, v3, AvroCompatibilityMode.Backward);
            var transitive = AvroSchema.CheckCompatibility(history, v3, AvroCompatibilityMode.BackwardTransitive);


            //Assert
            latestOnly.IsCompatible.Should().BeTrue();
            transitive.IsCompatible.Should().BeFalse();
            transitive.Incompatibilities.Should().ContainSingle(d => d.Path == "Order.legacy" && d.Kind == SchemaDifferenceKind.TypeChanged);
        }
    }
}
