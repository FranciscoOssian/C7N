namespace C7N.Engine.Workflows;

public class WorkflowConnection(
    string sourceNodeId,
    string sourceOutputId,
    string targetNodeId,
    string targetInputId)
{
    public string SourceNodeId { get; init; } = sourceNodeId;
    public string SourceOutputId { get; init; } = sourceOutputId;

    public string TargetNodeId { get; init; } = targetNodeId;
    public string TargetInputId { get; init; } = targetInputId;
}