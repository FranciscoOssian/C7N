namespace C7N.Engine.Nodes;

public class HttpRequestNode(string id, string name) : WorkflowNode(id, name, "http")
{
    public override Task Execute()
    {
        Console.WriteLine("Node-HttpReq started");
        return Task.CompletedTask;
    }
    
}