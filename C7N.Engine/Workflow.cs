namespace C7N.Engine;

public class Workflow
{
    public required string Id { get; init; }
    public required string Name { get; init; }

    public List<WorkflowNode> Nodes { get; init; } = [];
    public List<WorkflowConnection> Connections { get; init; } = [];
}