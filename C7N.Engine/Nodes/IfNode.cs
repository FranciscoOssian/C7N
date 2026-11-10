using System.Text.Json.Serialization;

namespace C7N.Engine.Nodes;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum IfComparisonOperator
{
    Equals,
    NotEquals,
    GreaterThan,
    LessThan,
    Contains
}

public record IfNodeParams(
    object? LeftValue = null,
    IfComparisonOperator Operator = IfComparisonOperator.Equals,
    object? RightValue = null
);

public class IfNode(string id, string name, IfNodeParams parameters) : WorkflowNode<IfNodeParams>(id, name, parameters)
{
    public override Task<object?> Execute()
    {
        Console.WriteLine("Node-if started");
        return Task.FromResult<object?>(null);
    }
    
}