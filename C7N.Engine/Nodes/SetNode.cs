namespace C7N.Engine.Nodes;

public record SetNodeParams(
    Dictionary<string, object> input,
    Dictionary<string, object> values
    );

public class SetNode(
    string id,
    string name,
    SetNodeParams parameters
) : WorkflowNode<SetNodeParams>(id, name, parameters)
{
    public override Task<object?> Execute()
    {
        Console.WriteLine("Node-Set started");

        var output = DeepMerge(Parameters.input, Parameters.values);

        return Task.FromResult<object?>(output);
    }

    private static Dictionary<string, object> DeepMerge(
        Dictionary<string, object> input,
        Dictionary<string, object> values)
    {
        var output = new Dictionary<string, object>(input);

        foreach (var (key, value) in values)
        {
            if (output.TryGetValue(key, out var existing) &&
                existing is Dictionary<string, object> existingDict &&
                value is Dictionary<string, object> incomingDict)
            {
                output[key] = DeepMerge(existingDict, incomingDict);
            }
            else
            {
                output[key] = value;
            }
        }

        return output;
    }
}