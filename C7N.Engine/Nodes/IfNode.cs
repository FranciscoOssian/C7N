using System.Globalization;
using System.Text.Json.Serialization;
using C7N.Engine.Resolvers;

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
    public override Task<NodeOutput> Execute(Dictionary<string, object?>? input)
    {
        Console.WriteLine("Node-if started");

        var left = TemplateResolver.Resolve(Parameters.LeftValue, input);
        var right = TemplateResolver.Resolve(Parameters.RightValue, input);

        bool result = Parameters.Operator switch
        {
            IfComparisonOperator.Equals => AreEqual(left, right),
            IfComparisonOperator.NotEquals => !AreEqual(left, right),
            IfComparisonOperator.GreaterThan => TryToDouble(left, out var l) && TryToDouble(right, out var r) && l > r,
            IfComparisonOperator.LessThan => TryToDouble(left, out var l) && TryToDouble(right, out var r) && l < r,
            IfComparisonOperator.Contains => left is string l && right is string r && l.Contains(r),
            _ => false
        };

        return Task.FromResult(new NodeOutput
        {
            OutputChannel = result ? "true" : "false",
            Data = input
        });
    }

    private static bool AreEqual(object? left, object? right)
    {
        if (left is string && right is string)
            return Equals(left, right);

        if (TryToDouble(left, out var l) && TryToDouble(right, out var r))
            return l == r;

        return Equals(left, right);
    }

    private static bool TryToDouble(object? value, out double number)
    {
        number = 0;

        if (value is null or bool)
            return false;

        if (value is int or long or float or double or decimal)
        {
            number = Convert.ToDouble(value);
            return true;
        }

        if (value is string str)
            return double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out number);

        return false;
    }
}