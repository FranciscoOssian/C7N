namespace C7N.Engine.Connections;

public class Connection(
    string sourceNodeId,
    string sourceOutputId,
    string targetNodeId,
    string targetInputId,
    BufferMode bufferMode = BufferMode.Latest)
{
    public string SourceNodeId { get; init; } = sourceNodeId;
    public string SourceOutputId { get; init; } = sourceOutputId;

    public string TargetNodeId { get; init; } = targetNodeId;
    public string TargetInputId { get; init; } = targetInputId;

    public BufferMode BufferMode { get; init; } = bufferMode;
}