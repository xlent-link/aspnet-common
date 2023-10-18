using System.Net;
using System.Text;
using System.Text.Json;
using Moq;
using Moq.Protected;
using Shouldly;
using XlentLink.AspNet.Common.Exceptions;
using XlentLink.AspNet.Common.Rest;

namespace UnitTests.Rest;

public class BridgeRestClientTests
{
    private const string BaseUrl = "http://test.com/";

    #region Helpers

    private static BridgeRestClient CreateHttpClientWithResponse(HttpResponseMessage response, 
        Action<HttpRequestMessage, CancellationToken>? callback = null, string? baseUrl = null)
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var mockSetup = handlerMock
            .Protected()
            // Setup the PROTECTED method to mock
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            );
        if (callback != null)
        {
            mockSetup.Callback(callback);
        }
        mockSetup.ReturnsAsync(response);

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri(baseUrl ?? BaseUrl) };
        return new BridgeRestClient(httpClient);
    }

    private static Item CreateItem(int id, string name)
    {
        return new Item
        {
            Id = id,
            Name = name,
            Address = new()
            {
                Street = "Barling",
                City = "Bazling"
            }
        };
    }

    private void AssertEqual(Item item, Item expectedItem)
    {
        item.ShouldNotBeNull();
        item.Id.ShouldBe(expectedItem.Id);
        item.Name.ShouldBe(expectedItem.Name);
        item.Address.Street.ShouldBe(expectedItem.Address.Street);
        item.Address.City.ShouldBe(expectedItem.Address.City);
    }

    #endregion

    #region Base address behaviour

    [Theory]
    [InlineData("https://test.com", "items/123", "https://test.com/items/123")]
    [InlineData("https://test.com", "/items/123", "https://test.com/items/123")]
    [InlineData("https://test.com/api/", "items/123", "https://test.com/api/items/123")]
    [InlineData("https://test.com/api/v1/", "items/123", "https://test.com/api/v1/items/123")]
    
    // NOTE: Examples below have unexpected behaviour from the HttpClient, but there you go...
    // See https://stackoverflow.com/questions/23438416/why-is-httpclient-baseaddress-not-working/23438417#23438417
    [InlineData("https://test.com/WILL_BE_LOST_BECAUSE_OF_STARTING_SLASH_IN_RELATIVE_URL/", "/items/123", "https://test.com/items/123")]
    [InlineData("https://test.com/api/WILL_BE_LOST_BECAUSE_OF_MISSING_TRAILING_SLASH", "items/123", "https://test.com/api/items/123")]
    public async Task Note_How_Slashes_In_BaseAddress_And_RelativeUrl_Affects_The_Resulting_Url(string baseUrl, string relativeUrl, string expectedRequestUrl)
    {
        // Arrange
        HttpRequestMessage? request = null;
        var client = CreateHttpClientWithResponse(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        }, (r, _) =>
        {
            request = r;
        }, baseUrl);

        // Act
        await client.GetAsync<object?>(relativeUrl);

        // Assert
        request.ShouldNotBeNull();
        request.RequestUri.ShouldBe(new Uri(expectedRequestUrl));
    }

    #endregion

    #region Serialization

    [Fact]
    public async Task Can_Serialize_Object_For_Put()
    {
        // Arrange
        var item = CreateItem(1234, "Foo-Ling");
        const string relativeUrl = "items/1234";
        HttpRequestMessage? request = null;
        var client = CreateHttpClientWithResponse(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        }, (r, _) =>
        {
            request = r;
        });

        // Act
        await client.PutAsync<Item, Item>(relativeUrl, item);

        // Assert
        request.ShouldNotBeNull();
        var requestContent = await request.Content!.ReadAsStringAsync();
        var requestItem = JsonSerializer.Deserialize<Item>(requestContent);
        requestItem.ShouldNotBeNull();
        AssertEqual(requestItem, item);
    }

    [Fact]
    public async Task Can_Serialize_Object_For_Post()
    {
        // Arrange
        var item = CreateItem(1234, "Foo-Ling");
        const string relativeUrl = "items";
        HttpRequestMessage? request = null;
        var client = CreateHttpClientWithResponse(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        }, (r, _) =>
        {
            request = r;
        });

        // Act
        await client.PostAsync<Item, Item>(relativeUrl, item);

        // Assert
        request.ShouldNotBeNull();
        var requestContent = await request.Content!.ReadAsStringAsync();
        var requestItem = JsonSerializer.Deserialize<Item>(requestContent);
        requestItem.ShouldNotBeNull();
        AssertEqual(requestItem, item);
    }

    #endregion

    #region Deserialization

    [Fact]
    public async Task Can_Deserialize_Object_For_Get()
    {
        // Arrange
        var item = CreateItem(1234, "Foo-Ling");
        const string relativeUrl = "items/1234";
        var client = CreateHttpClientWithResponse(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonSerializer.Serialize(item), Encoding.UTF8, "application/json")
        });

        // Act
        var responseItem = await client.GetAsync<Item>(relativeUrl);

        // Assert
        AssertEqual(responseItem, item);
    }

    [Fact]
    public async Task Can_Deserialize_Object_For_Put()
    {
        // Arrange
        var item = CreateItem(1234, "Foo-Ling");
        const string relativeUrl = "items/1234";
        var client = CreateHttpClientWithResponse(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonSerializer.Serialize(item), Encoding.UTF8, "application/json")
        });

        // Act
        var responseItem = await client.PutAsync<Item, Item>(relativeUrl, item);

        // Assert
        AssertEqual(responseItem, item);
    }

    [Fact]
    public async Task Can_Deserialize_Object_For_Post()
    {
        // Arrange
        var item = CreateItem(1234, "Foo-Ling");
        const string relativeUrl = "items/1234";
        var client = CreateHttpClientWithResponse(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonSerializer.Serialize(item), Encoding.UTF8, "application/json")
        });

        // Act
        var responseItem = await client.PostAsync<Item, Item>(relativeUrl, item);

        // Assert
        AssertEqual(responseItem, item);
    }

    #endregion

    #region Error handling

    [Fact]
    public async Task Throws_For_Unsuccessful_Post()
    {
        // Arrange
        const HttpStatusCode statusCode = HttpStatusCode.InternalServerError;
        const string relativeUrl = "items";
        var client = CreateHttpClientWithResponse(new HttpResponseMessage(statusCode));

        // Act & Assert
        var exception = await Should.ThrowAsync<UnsuccessfulHttpResponseException>(async () => await client.PostAsync<string, string>(relativeUrl, "item"));
        exception.ShouldNotBeNull();
        exception.ResponseStatusCode.ShouldBe(statusCode);
        exception.Url.ShouldContain(BaseUrl);
        exception.Url.ShouldContain(relativeUrl);
    }

    [Fact]
    public async Task Throws_For_Unsuccessful_Put()
    {
        // Arrange
        const HttpStatusCode statusCode = HttpStatusCode.BadRequest;
        const string relativeUrl = "items";
        var client = CreateHttpClientWithResponse(new HttpResponseMessage(statusCode));

        // Act & Assert
        var exception = await Should.ThrowAsync<UnsuccessfulHttpResponseException>(async () => await client.PutAsync<string, string>(relativeUrl, "item"));
        exception.ShouldNotBeNull();
        exception.ResponseStatusCode.ShouldBe(statusCode);
        exception.Url.ShouldContain(BaseUrl);
        exception.Url.ShouldContain(relativeUrl);
    }

    [Fact]
    public async Task Throws_For_Unsuccessful_Get()
    {
        // Arrange
        const HttpStatusCode statusCode = HttpStatusCode.NotFound;
        const string relativeUrl = "items/123";
        var client = CreateHttpClientWithResponse(new HttpResponseMessage(statusCode));

        // Act & Assert
        var exception = await Should.ThrowAsync<UnsuccessfulHttpResponseException>(async () => await client.GetAsync<string>(relativeUrl));
        exception.ShouldNotBeNull();
        exception.ResponseStatusCode.ShouldBe(statusCode);
        exception.Url.ShouldContain(BaseUrl);
        exception.Url.ShouldContain(relativeUrl);
    }

    [Fact]
    public async Task Throws_For_Unsuccessful_Delete()
    {
        // Arrange
        const HttpStatusCode statusCode = HttpStatusCode.Conflict;
        const string relativeUrl = "items/123";
        var client = CreateHttpClientWithResponse(new HttpResponseMessage(statusCode));

        // Act & Assert
        var exception = await Should.ThrowAsync<UnsuccessfulHttpResponseException>(async () => await client.DeleteAsync(relativeUrl));
        exception.ShouldNotBeNull();
        exception.ResponseStatusCode.ShouldBe(statusCode);
        exception.Url.ShouldContain(BaseUrl);
        exception.Url.ShouldContain(relativeUrl);
    }

    #endregion
}

internal class Item
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public Address Address { get; set; } = null!;
}

internal class Address
{
    public string Street { get; set; } = null!;
    public string City { get; set; } = null!;
}