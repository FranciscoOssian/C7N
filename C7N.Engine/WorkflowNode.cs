namespace C7N.Engine;
using C7N.Engine.Nodes;
using System.Text.Json.Serialization;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(HttpRequestNode), "http")]
[JsonDerivedType(typeof(IfNode), "if")]
[JsonDerivedType(typeof(SetNode), "set")]
[JsonDerivedType(typeof(ManualTriggerNode), "manual-trigger")]
public abstract class WorkflowNode(string id, string name)
{
    public string Id { get; init; } = id;
    public string Name { get; set; } = name;
    public abstract Task<object?> Execute();
}

public abstract class WorkflowNode<TParameters>(string id, string name, TParameters parameters): WorkflowNode(id, name)
{
    public TParameters Parameters { get; init; } = parameters;
}