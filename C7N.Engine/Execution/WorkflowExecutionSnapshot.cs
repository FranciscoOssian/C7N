namespace C7N.Engine.Execution;

public record NodeExecutionSnapshot(
    NodeExecutionStatus Status,
    Dictionary<string, object?>? Data = null,
    string? Error = null
);

public record WorkflowExecutionSnapshot(
    string Id,
    NodeExecutionStatus Status,
    Dictionary<string, NodeExecutionSnapshot> Nodes
);