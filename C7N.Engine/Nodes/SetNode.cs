namespace C7N.Engine.Nodes;

public class SetNode(
    string id,
    string name,
    Dictionary<string, object> parameters
) : WorkflowNode(id, name, "set", parameters)
{
    public override Task<object> Execute()
    {
        Console.WriteLine("Node-Set started");

        if (Parameters["input"] is not Dictionary<string, object> input ||
            Parameters["values"] is not Dictionary<string, object> values)
        {
            throw new InvalidOperationException("Missing or invalid input/values parameters.");
        }

        var output = DeepMerge(input, values);

        return Task.FromResult<object>(output);
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