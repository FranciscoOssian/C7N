using System.Reflection;

namespace C7N.Engine.Workflows;
using C7N.Engine.Nodes;
using System.Text.Json.Serialization;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(HttpRequestNode), nameof(HttpRequestNode))]
[JsonDerivedType(typeof(IfNode), nameof(IfNode))]
[JsonDerivedType(typeof(SetNode), nameof(SetNode))]
[JsonDerivedType(typeof(ManualTriggerNode), nameof(ManualTriggerNode))]
public abstract class WorkflowNode(string id, string name)
{
    public string Id { get; init; } = id;
    public string Name { get; set; } = name;
    public abstract Task<object?> Execute(Dictionary<string, object?>? input);
}

public abstract class WorkflowNode<TParameters>(string id, string name, TParameters parameters): WorkflowNode(id, name)
{
    public TParameters Parameters { get; init; } = parameters;
}