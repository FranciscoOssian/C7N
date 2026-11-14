using System.Text.Json;
using System.Text.Json.Serialization;
using C7N.Engine.Resolvers;
using C7N.Engine.Workflows;

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
    public override async Task<object?> Execute(Dictionary<string, object?>? input)
    {
        Console.WriteLine("Node-HttpReq started");
        
        var response = await Client.GetAsync(TemplateResolver.Resolve(Parameters.Url, input)?.ToString());
        var rawBody = await response.Content.ReadAsStringAsync();
        object? parsedBody = rawBody;
        
        try
        {
            using var doc = JsonDocument.Parse(rawBody);
            parsedBody = doc.RootElement.Clone();
        }
        catch (JsonException) {}

        var output = new Dictionary<string, object?>
        {
            ["statusCode"] = (int)response.StatusCode,
            ["headers"] = response.Headers.ToDictionary(h => h.Key, h => (object?)h.Value.ToArray()),
            ["body"] = parsedBody
        };

        Console.WriteLine(JsonSerializer.Serialize(output));
        return output;
    }
}