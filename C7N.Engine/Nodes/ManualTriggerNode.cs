namespace C7N.Engine.Nodes;
using C7N.Engine.Workflows;

public record ManualTriggerNodeParams();

public class ManualTriggerNode(string id, string name, ManualTriggerNodeParams parameters) : Node<ManualTriggerNodeParams>(id, name, parameters)
{
    public override Task<object?> Execute(Dictionary<string, object?>? input)
    {
        Console.WriteLine("Workflow started");
        return Task.FromResult<object?>(null);
    }
    
}