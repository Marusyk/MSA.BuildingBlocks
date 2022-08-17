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

    public static HttpRequestMessage WithHeader(this Url url, string name, string value)
    {
        ArgumentNullException.ThrowIfNull(url, nameof(url));
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Shouldn't be empty.", nameof(name));
        }

        var requestMessage = new HttpRequestMessage { RequestUri = new Uri(url) };

        requestMessage.Headers.TryAddWithoutValidation(name, value);
        return requestMessage;
    }

    public static HttpRequestMessage WithHeader(this HttpRequestMessage requestMessage, string name, string value)
    {
        ArgumentNullException.ThrowIfNull(requestMessage, nameof(requestMessage));
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Shouldn't be empty.", nameof(name));
        }

        requestMessage.Headers.TryAddWithoutValidation(name, value);
        return requestMessage;
    }

    public static HttpRequestMessage WithHttpMethod(this Url url, HttpMethod httpMethod)
    {
        ArgumentNullException.ThrowIfNull(url, nameof(url));

        var requestMessage = new HttpRequestMessage { RequestUri = new Uri(url), Method = httpMethod };
        return requestMessage;
    }

    public static HttpRequestMessage WithHttpMethod(this HttpRequestMessage requestMessage, HttpMethod httpMethod)
    {
        ArgumentNullException.ThrowIfNull(requestMessage, nameof(requestMessage));

        requestMessage.Method = httpMethod;
        return requestMessage;
    }

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