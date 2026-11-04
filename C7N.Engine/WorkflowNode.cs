namespace C7N.Engine;
using C7N.Engine.Nodes;
using System.Text.Json.Serialization;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(HttpRequestNode), "http")]
[JsonDerivedType(typeof(IfNode), "if")]
[JsonDerivedType(typeof(SetNode), "set")]
[JsonDerivedType(typeof(ManualTriggerNode), "manualTriggerNode")]
public abstract class WorkflowNode(string id, string name, string type, Dictionary<string, object> parameters)
{
    public string Id { get; init; } = id;
    public string Name { get; set; } = name;

    public abstract Task Execute();

    public Dictionary<string, object> Parameters { get; init; } = parameters;
}