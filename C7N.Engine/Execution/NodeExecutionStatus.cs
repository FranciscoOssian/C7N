using System.Text.Json.Serialization;

namespace C7N.Engine.Execution;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NodeExecutionStatus
{
    Pending,
    Ready,
    Running,
    Completed,
    Failed,
}