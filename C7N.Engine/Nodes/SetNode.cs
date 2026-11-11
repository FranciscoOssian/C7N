namespace C7N.Engine.Nodes;

using C7N.Engine.Resolvers;

/// <summary>
/// Represents the parameters for configuring a SetNode operation.
/// </summary>
public record SetNodeParams(
    Dictionary<string, object?> values
);

/// <summary>
/// Workflow node that merges configured values into the incoming execution data,
/// resolving any template placeholders present in keys or values.
/// </summary>
public class SetNode(
    string id,
    string name,
    SetNodeParams parameters
) : WorkflowNode<SetNodeParams>(id, name, parameters)
{
    /// <summary>
    /// Executes the node operation by merging parameters into the provided input payload.
    /// </summary>
    /// <param name="input">The incoming dictionary from previous execution steps.</param>
    /// <returns>A task containing the merged and resolved output dictionary.</returns>
    public override Task<object?> Execute(Dictionary<string, object?>? input)
    {
        Console.WriteLine("Node-Set started");
        
        var safeInput = input ?? [];
        var output = DeepMerge(safeInput, Parameters.values, safeInput);

        return Task.FromResult<object?>(output);
    }

    /// <summary>
    /// Recursively merges source key-value pairs into the target dictionary while resolving templates.
    /// </summary>
    /// <param name="target">The base dictionary to be modified.</param>
    /// <param name="source">The dictionary containing updates and additions.</param>
    /// <param name="context">The root lookup dictionary used for template resolution.</param>
    /// <returns>A new dictionary containing the merged and resolved result.</returns>
    private static Dictionary<string, object?> DeepMerge(
        Dictionary<string, object?> target,
        Dictionary<string, object?> source,
        Dictionary<string, object?> context)
    {
        var output = new Dictionary<string, object?>(target);

        foreach (var (key, value) in source)
        {
            var resolvedKey = TemplateResolver.Resolve(key, context)?.ToString() ?? key;

            if (output.TryGetValue(resolvedKey, out var existing) &&
                existing is Dictionary<string, object?> existingDict &&
                value is Dictionary<string, object?> incomingDict)
            {
                output[resolvedKey] = DeepMerge(existingDict, incomingDict, context);
            }
            else
            {
                output[resolvedKey] = TemplateResolver.Resolve(value, context);
            }
        }

        return output;
    }
}