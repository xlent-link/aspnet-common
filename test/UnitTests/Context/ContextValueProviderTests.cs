using Shouldly;
using XlentLink.AspNet.Common.Context;

namespace UnitTests.Context;

public class ContextValueProviderTests
{
    private readonly IContextValueProvider _contextValueProvider;

    public ContextValueProviderTests()
    {
        _contextValueProvider = new AsyncLocalContextValueProvider();
    }

    private void AssertKeyHasValue(string key, string expectedValue)
    {
        var value = _contextValueProvider.GetValue<string>(key);
        value.ShouldBe(expectedValue, "Value should have been propagated to this level");
    }

    private async Task AssertKeyHasValueAsync(string key, string expectedValue)
    {
        await Task.Yield();
        AssertKeyHasValue(key, expectedValue);
    }

    [Fact]
    public void A_Value_Can_Be_Propagated_To_Lower_Levels()
    {
        // Arrange
        const string key = "key";
        const string value = "value";

        _contextValueProvider.SetValue(key, value);

        // Act && Assert
        AssertKeyHasValue(key, value);

        var contextValue = _contextValueProvider.GetValue<string>(key);
        contextValue.ShouldBe(value, "Value should stlll be available");
    }

    [Fact]
    public async Task A_Value_Can_Be_Propagated_To_Lower_Levels_With_Async()
    {
        // Arrange
        const string key = "key";
        const string value = "value";

        _contextValueProvider.SetValue(key, value);

        // Act && Assert
        await AssertKeyHasValueAsync(key, value);

        var contextValue = _contextValueProvider.GetValue<string>(key);
        contextValue.ShouldBe(value, "Value should stlll be available");
    }

    [Fact]
    public void Returns_Default_For_Unknown_Keys()
    {
        // Act
        var stringValue = _contextValueProvider.GetValue<string>("unknown-key");
        var dateTimeOffsetValue = _contextValueProvider.GetValue<DateTimeOffset>("unknown-key");
        var objectValue = _contextValueProvider.GetValue<SomeClass>("unknown-key");
        var nullableIntValue = _contextValueProvider.GetValue<int?>("unknown-key");
        var intValue = _contextValueProvider.GetValue<int>("unknown-key");
        var enumValue = _contextValueProvider.GetValue<SomeEnum>("unknown-key");
        
        // Assert
        stringValue.ShouldBe(default);
        dateTimeOffsetValue.ShouldBe(default);
        objectValue.ShouldBe(default);
        nullableIntValue.ShouldBe(default);
        intValue.ShouldBe(0);
        enumValue.ShouldBe(default);
        enumValue.ShouldBe(SomeEnum.First);
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    private class SomeClass
    {
    }

    private enum SomeEnum
    {
        First, Second, Third
    }
}
