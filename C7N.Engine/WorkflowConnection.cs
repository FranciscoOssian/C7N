namespace C7N.Engine;

public class WorkflowConnection(string sourceNodeId, string targetNodeId)
{
    public string SourceNodeId { get; init; } = sourceNodeId;
    public string TargetNodeId { get; init; } = targetNodeId;
}