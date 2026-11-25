using System.Text.Json.Serialization;

namespace C7N.Engine.Execution;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ExecutionEventType
{
    WorkflowRunning,
    NodeRunning,
    NodeCompleted,
    NodeFailed,
    WorkflowCompleted,
    WorkflowFailed
}