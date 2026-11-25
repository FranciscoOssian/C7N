using System.Text.Json.Serialization;

namespace C7N.Engine.Execution;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(NodeEventPayload), nameof(NodeEventPayload))]
[JsonDerivedType(typeof(WorkflowEventPayload), nameof(WorkflowEventPayload))]
public abstract record ExecutionEventPayload;

public record NodeEventPayload(
    string NodeId,
    NodeExecutionSnapshot Node
) : ExecutionEventPayload;

public record WorkflowEventPayload(
    NodeExecutionStatus Status,
    string? Error = null
) : ExecutionEventPayload;

public record ExecutionEvent(
    ExecutionEventType EventType,
    ExecutionEventPayload Payload
);