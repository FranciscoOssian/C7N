using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using C7N.Engine.Resolvers;

namespace C7N.Engine.Nodes;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum HttpsRequestMethods
{
    GET,
    POST,
    PUT,
    DELETE
}

public record HttpsRequestNodeParams(
    string Url,
    HttpsRequestMethods Method = HttpsRequestMethods.GET,
    string? Path = null,
    Dictionary<string, string>? Headers = null,
    Dictionary<string, string>? QueryParameters = null,
    string? Body = null,
    int Timeout = 30,
    int Retries = 0
);

public class HttpRequestNode(string id, string name, HttpsRequestNodeParams parameters) : Node<HttpsRequestNodeParams>(id, name, parameters)
{
    private static readonly HttpClient Client = new();

    public override async Task<NodeOutput> Execute(Dictionary<string, object?>? input)
    {
        Console.WriteLine("Node-HttpReq started");

        var baseUrl = TemplateResolver.Resolve(Parameters.Url, input)?.ToString() ?? string.Empty;
        var resolvedPath = Parameters.Path is not null
            ? TemplateResolver.Resolve(Parameters.Path, input)?.ToString()
            : null;

        var finalUrl = string.IsNullOrWhiteSpace(resolvedPath)
            ? baseUrl
            : $"{baseUrl.TrimEnd('/')}/{resolvedPath.TrimStart('/')}";

        if (Parameters.QueryParameters is { Count: > 0 })
        {
            var queryParts = Parameters.QueryParameters.Select(kv =>
            {
                var key = Uri.EscapeDataString(kv.Key);
                var val = Uri.EscapeDataString(TemplateResolver.Resolve(kv.Value, input)?.ToString() ?? string.Empty);
                return $"{key}={val}";
            });

            var separator = finalUrl.Contains('?') ? "&" : "?";
            finalUrl = $"{finalUrl}{separator}{string.Join("&", queryParts)}";
        }

        var method = HttpMethod.Parse(Parameters.Method.ToString());
        using var request = new HttpRequestMessage(method, finalUrl);

        var contentType = "application/json";

        if (Parameters.Headers is not null)
        {
            foreach (var (key, rawValue) in Parameters.Headers)
            {
                var resolvedValue = TemplateResolver.Resolve(rawValue, input)?.ToString() ?? string.Empty;

                if (key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                {
                    contentType = resolvedValue;
                    continue;
                }

                request.Headers.TryAddWithoutValidation(key, resolvedValue);
            }
        }

        if (!string.IsNullOrEmpty(Parameters.Body))
        {
            var resolvedBody = TemplateResolver.Resolve(Parameters.Body, input)?.ToString() ?? string.Empty;
            request.Content = new StringContent(resolvedBody, Encoding.UTF8, contentType);
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Parameters.Timeout));
        using var response = await Client.SendAsync(request, cts.Token);

        var rawBody = await response.Content.ReadAsStringAsync(cts.Token);
        object? parsedBody = rawBody;

        if (!string.IsNullOrWhiteSpace(rawBody))
        {
            try
            {
                using var doc = JsonDocument.Parse(rawBody);
                parsedBody = doc.RootElement.Clone();
            }
            catch (JsonException) { }
        }

        var output = new Dictionary<string, object?>
        {
            ["statusCode"] = (int)response.StatusCode,
            ["headers"] = response.Headers.ToDictionary(h => h.Key, h => (object?)h.Value.ToArray()),
            ["body"] = parsedBody
        };

        Console.WriteLine(JsonSerializer.Serialize(output));
        return new NodeOutput { Data = output, OutputChannel = "main" };
    }
}