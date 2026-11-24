namespace C7N.Engine.DataManipulation;

public static class Merge
{
    public static Dictionary<string, object?> DeepMerge(
        IDictionary<string, object?> target,
        IDictionary<string, object?> source)
    {
        var output = new Dictionary<string, object?>(target);

        foreach (var (key, value) in source)
        {
            if (output.TryGetValue(key, out var existing) &&
                existing is IDictionary<string, object?> existingDict &&
                value is IDictionary<string, object?> incomingDict)
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