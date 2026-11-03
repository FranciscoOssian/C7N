namespace C7N.Engine;

public abstract class WorkflowNode(string id, string name, string type)
{
    public required string Id { get; init; } = id;
    public required string Name { get; set; } = name;
    public required string Type { get; init; } = type;

    public abstract Task Execute();
    
    public Dictionary<string, object> Parameters { get; init; } = [];
}