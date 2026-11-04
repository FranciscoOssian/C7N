using System.Text.Json;

namespace C7N.Engine.Nodes;
using System.Net.Http;

public class HttpRequestOutput
{
    public int StatusCode { get; set; }
    public Dictionary<string, string[]> Headers { get; set; } = [];
    public string Body { get; set; } = string.Empty;
}

public class HttpRequestNode(string id, string name, Dictionary<string, object> parameters) : WorkflowNode(id, name, "http", parameters)
{
    private static readonly HttpClient Client = new();
    public override async Task<object> Execute()
    {
        Console.WriteLine("Node-HttpReq started");
        
        if (!Parameters.TryGetValue("url", out var urlValue) || urlValue is not string url)
            throw new InvalidOperationException("Missing or invalid URL parameter.");
        
        var response = await Client.GetAsync(url);
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