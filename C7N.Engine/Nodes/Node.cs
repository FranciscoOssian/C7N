using System.Reflection;

namespace C7N.Engine.Nodes;
using C7N.Engine.Nodes;
using System.Text.Json.Serialization;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NodeExecutionPolicy
{
    WaitAll,
    EveryToken,
    OnDemand,
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(HttpRequestNode), nameof(HttpRequestNode))]
[JsonDerivedType(typeof(IfNode), nameof(IfNode))]
[JsonDerivedType(typeof(SetNode), nameof(SetNode))]
[JsonDerivedType(typeof(ManualTriggerNode), nameof(ManualTriggerNode))]
public abstract class Node(string id, string name)
{
    public string Id { get; init; } = id;
    public string Name { get; set; } = name;
    public NodeExecutionPolicy ExecutionPolicy { get; set; } = NodeExecutionPolicy.OnDemand;
    public abstract Task<object?> Execute(Dictionary<string, object?>? input);
}

public abstract class Node<TParameters>(string id, string name, TParameters parameters): Node(id, name)
{
    public TParameters Parameters { get; init; } = parameters;
}