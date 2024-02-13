using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using AutoFixture;
using SolTechnology.Avro;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using SolTechnology.Avro.Infrastructure.Attributes;
using Xunit;

namespace AvroConvertComponentTests.DefaultOnly
{
    public class AvroAttributeClassTests
    {
        private readonly Fixture _fixture = new();

        [Theory]
        [MemberData(nameof(TestEngine.DefaultOnly), MemberType = typeof(TestEngine))]
        public void Types_mismatches_are_handled(Func<object, Type, dynamic> engine)
        {
            //Arrange
            UserWithNonNullableProperties toSerialize = _fixture.Create<UserWithNonNullableProperties>();


            //Act
            var deserialized = engine.Invoke(toSerialize, typeof(User));


            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize.favorite_number, deserialized.favorite_number);
            Assert.Equal(toSerialize.name, deserialized.name);
            Assert.Equal(toSerialize.favorite_color, deserialized.favorite_color);
        }

        [Theory]
        [MemberData(nameof(TestEngine.DefaultOnly), MemberType = typeof(TestEngine))]
        public void Attribute_class_used_for_serialization_overrides_property_names(Func<object, Type, dynamic> engine)
        {
            //Arrange
            AttributeClass toSerialize = _fixture.Create<AttributeClass>();


            //Act
            var deserialized = (User)engine.Invoke(toSerialize, typeof(User));


            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize.NullableIntProperty, deserialized.favorite_number);
            Assert.Equal(toSerialize.NullableStringProperty, deserialized.name);
            Assert.Equal(toSerialize.AndAnotherString, deserialized.favorite_color);
        }

        [Theory]
        [MemberData(nameof(TestEngine.DefaultOnly), MemberType = typeof(TestEngine))]
        public void Attribute_class_used_for_serialization_overrides_fields_names(Func<object, Type, dynamic> engine)
        {
            //Arrange
            AttributeClassWithoutGetters toSerialize = _fixture.Create<AttributeClassWithoutGetters>();


            //Act
            var deserialized = engine.Invoke(toSerialize, typeof(User));

            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize.NullableIntProperty, deserialized.favorite_number);
            Assert.Equal(toSerialize.StringProperty, deserialized.name);
            Assert.Equal(toSerialize.AndAnotherString, deserialized.favorite_color);
        }

        [Theory]
        [MemberData(nameof(TestEngine.DefaultOnly), MemberType = typeof(TestEngine))]
        public void Attribute_class_used_for_serialization_overrides_fields_namses(Func<object, Type, dynamic> engine)
        {
            var x = AvroConvert.GenerateSchema(typeof(Chapter));

            var d = x;
        }
    }

    /// <summary>  </summary>
    [GeneratedCode("Avro.MSBuild", "1.11.3.1")]
    public class Chapter
    {
        // private static readonly Schema _schema = Schema.Parse(AvroConvert.GenerateSchema(typeof(Chapter)));

        /// <inheritdoc />
        [IgnoreDataMember] public Schema Schema;

        /// <summary>  </summary>
        [NullableSchema]
        public IEnumerable<Paragraph> Paragraphs { get; private set; }

        /// <summary>  </summary>
        [NullableSchema]
        public string Title { get; private set; }

        /// <summary> for serializer </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Chapter()
        {

        }

        public Chapter(IEnumerable<Paragraph> paragraphs, string title)
        {
            Paragraphs = paragraphs;
            Title = title;
        }


        /// <inheritdoc />
        public object Get(int fieldPos)
        {
            return fieldPos switch
            {
                0 => Paragraphs,
                1 => Title,
                _ => throw new ArgumentOutOfRangeException(nameof(fieldPos), fieldPos, null)
            };
        }

        /// <inheritdoc />
        public void Put(int fieldPos, object fieldValue)
        {
            switch (fieldPos)
            {
                case 0: Paragraphs = (IEnumerable<Paragraph>)fieldValue; break;
                case 1: Title = (string)fieldValue; break;
                default: throw new ArgumentOutOfRangeException(nameof(fieldPos), fieldPos, null);
            };
        }
    }

    /// <summary>  </summary>
    [GeneratedCode("Avro.MSBuild", "1.11.3.1")]
    public class Paragraph
    {
        // private static readonly Schema _schema = Schema.Parse(AvroConvert.GenerateSchema(typeof(Paragraph)));

        /// <inheritdoc />
        [IgnoreDataMember]
        public Schema Schema;

        /// <summary>  </summary>
        [NullableSchema]
        public string SentenceSeparator { get; private set; }

        /// <summary>  </summary>
        [NullableSchema]
        public string Style { get; private set; }

        /// <summary> for serializer </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected Paragraph()
        {

        }

        public Paragraph(string sentenceSeparator, string style)
        {
            SentenceSeparator = sentenceSeparator;
            Style = style;
        }


        /// <inheritdoc />
        public object Get(int fieldPos)
        {
            return fieldPos switch
            {
                0 => SentenceSeparator,
                1 => Style,
                _ => throw new ArgumentOutOfRangeException(nameof(fieldPos), fieldPos, null)
            };
        }

        /// <inheritdoc />
        public void Put(int fieldPos, object fieldValue)
        {
            switch (fieldPos)
            {
                case 0: SentenceSeparator = (string)fieldValue; break;
                case 1: Style = (string)fieldValue; break;
                default: throw new ArgumentOutOfRangeException(nameof(fieldPos), fieldPos, null);
            };
        }
    }
}
