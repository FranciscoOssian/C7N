using System.Text.Json.Serialization;
using C7N.Engine.Workflows;

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

public class IfNode(string id, string name, IfNodeParams parameters) : Node<IfNodeParams>(id, name, parameters)
{
    public override Task<object?> Execute(Dictionary<string, object?>? input)
    {
        Console.WriteLine("Node-if started");
        return Task.FromResult<object?>(null);
    }
}