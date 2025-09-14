using System;
using Xunit;

namespace AvroConvertComponentTests.FullSerializationAndDeserialization;

public class NumberTests
{
    [Theory]
    [MemberData(nameof(TestEngine.Core), MemberType = typeof(TestEngine))]
    public void Class_WithTooBigDecimal_ShouldThrow_ByDefault(Func<object, Type, dynamic> engine)
    {
        //Arrange
        var record = new LogicalTypesClass();
        record.One = 1.12345678901234567890m; // Default scale is 14

        //Act
        var exception = Record.Exception(() => engine.Invoke(record, typeof(LogicalTypesClass)));

        //Assert
        Assert.NotNull(exception);
    }

    [Theory]
    [MemberData(nameof(TestEngine.HeadlessUsingNumberHandling), MemberType = typeof(TestEngine))]
    public void Class_WithTooBigDecimal_ShouldTruncateOrRound_UsingHandling(Func<object, Type, dynamic> engine)
    {
        //Arrange
        var record = new LogicalTypesClass();
        record.One = 1.12345678901234567890m; // Default scale is 14

        //Act
        var deserialized = engine.Invoke(record, typeof(LogicalTypesClass));
        var value = (decimal)deserialized.One;

        //Assert
        Assert.NotNull(deserialized);
        Assert.Equal(14, value.Scale);
        Assert.True(value >= 1.12345678901234m && value <= 1.12345678901235m);
    }
}