using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Flurl;

namespace MSA.BuildingBlocks.ServiceClient;

public static class Extensions
{
    public static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Extension method to add a header with the given name and value to an <see cref="Url"/> and returns an <see cref="HttpRequestMessage"/> instance with the modified headers.
    /// </summary>
    /// <param name="url">The <see cref="Url"/> instance to modify.</param>
    /// <param name="name">The name of the header to add.</param>
    /// <param name="value">The value of the header to add.</param>
    /// <returns>An <see cref="HttpRequestMessage"/> instance with the modified headers.</returns>
    /// <exception cref="ArgumentException">Thrown when the name parameter is null or empty.</exception>
    public static HttpRequestMessage WithHeader(this Url url, string name, string value)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException("Shouldn't be empty.", nameof(name));
        }

        var requestMessage = new HttpRequestMessage { RequestUri = new Uri(url) };

        requestMessage.Headers.TryAddWithoutValidation(name, value);
        return requestMessage;
    }

    /// <summary>
    /// Extension method to add a header with the given name and value to an <see cref="HttpRequestMessage"/> instance and returns the modified <see cref="HttpRequestMessage"/>.
    /// </summary>
    /// <param name="requestMessage">The <see cref="HttpRequestMessage"/> instance to modify.</param>
    /// <param name="name">The name of the header to add.</param>
    /// <param name="value">The value of the header to add.</param>
    /// <returns>The modified <see cref="HttpRequestMessage"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the requestMessage parameter is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the name parameter is null or empty.</exception>
    public static HttpRequestMessage WithHeader(this HttpRequestMessage requestMessage, string name, string value)
    {
        ArgumentNullException.ThrowIfNull(requestMessage, nameof(requestMessage));

        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException("Shouldn't be empty.", nameof(name));
        }

        requestMessage.Headers.TryAddWithoutValidation(name, value);
        return requestMessage;
    }

    /// <summary>
    /// Extension method to set the http method of an <see cref="Url"/> and returns an <see cref="HttpRequestMessage"/> instance with the modified http method.
    /// </summary>
    /// <param name="url">The <see cref="Url"/> instance to modify.</param>
    /// <param name="httpMethod">The http method to set.</param>
    /// <returns>An <see cref="HttpRequestMessage"/> instance with the modified http method.</returns>
    public static HttpRequestMessage WithHttpMethod(this Url url, HttpMethod httpMethod) =>
        new() { RequestUri = new Uri(url), Method = httpMethod };

    /// <summary>
    /// Extension method to set the http method of an <see cref="HttpRequestMessage"/> instance and returns the modified <see cref="HttpRequestMessage"/>.
    /// </summary>
    /// <param name="requestMessage">The <see cref="HttpRequestMessage"/> instance to modify.</param>
    /// <param name="httpMethod">The http method to set.</param>
    /// <returns>The modified <see cref="HttpRequestMessage"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the requestMessage parameter is null.</exception>
    public static HttpRequestMessage WithHttpMethod(this HttpRequestMessage requestMessage, HttpMethod httpMethod)
    {
        ArgumentNullException.ThrowIfNull(requestMessage, nameof(requestMessage));

        requestMessage.Method = httpMethod;
        return requestMessage;
    }

    /// <summary>
    /// Extension method to add JSON content to an <see cref="HttpRequestMessage"/> instance and returns the modified <see cref="HttpRequestMessage"/>.
    /// </summary>
    /// <param name="requestMessage">The <see cref="HttpRequestMessage"/> instance to modify.</param>
    /// <param name="content">The object to serialize to JSON and add to the request body.</param>
    /// <returns>The modified <see cref="HttpRequestMessage"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the requestMessage parameter is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the http method of the request message is not POST, PUT or PATCH.</exception>
    public static HttpRequestMessage WithJsonContent(this HttpRequestMessage requestMessage, object content)
    {
        ArgumentNullException.ThrowIfNull(requestMessage, nameof(requestMessage));

        if (requestMessage.Method != HttpMethod.Post
            && requestMessage.Method != HttpMethod.Put
            && requestMessage.Method != HttpMethod.Patch)
        {
            throw new ArgumentException(
                "Use request body only for POST, PUT and PATCH http methods. To specify http verb use WithHttpMethod() extension.",
                nameof(requestMessage));
        }

        string jsonContent = JsonSerializer.Serialize(content, JsonSerializerOptions);
        requestMessage.Content = new StringContent(jsonContent, Encoding.UTF8, MediaTypeNames.Application.Json);
        return requestMessage;
    }
}
