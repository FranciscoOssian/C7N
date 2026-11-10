using System.Text.Json;
using System.Text.Json.Serialization;
namespace C7N.Engine.Nodes;
using System.Net.Http;

public class HttpRequestOutput
{
    public int StatusCode { get; set; }
    public Dictionary<string, string[]> Headers { get; set; } = [];
    public string Body { get; set; } = string.Empty;
}

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

public class HttpRequestNode(string id, string name, HttpsRequestNodeParams parameters) : WorkflowNode<HttpsRequestNodeParams>(id, name, parameters)
{
    private static readonly HttpClient Client = new();
    public override async Task<object?> Execute()
    {
        Console.WriteLine("Node-HttpReq started");
        
        var response = await Client.GetAsync(parameters.Url);
        response.EnsureSuccessStatusCode();
        var output = new HttpRequestOutput
        {
            StatusCode = (int)response.StatusCode,
            Headers = response.Headers.ToDictionary(
                h => h.Key,
                h => h.Value.ToArray()),
            Body = await response.Content.ReadAsStringAsync()
        };
        Console.WriteLine(JsonSerializer.Serialize(output));
        return output;
    }
}