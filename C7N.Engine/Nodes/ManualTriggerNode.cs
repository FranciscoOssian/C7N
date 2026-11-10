namespace C7N.Engine.Nodes;

public record ManualTriggerNodeParams();

public class ManualTriggerNode(string id, string name, ManualTriggerNodeParams parameters) : WorkflowNode<ManualTriggerNodeParams>(id, name, parameters)
{
    public override Task<object?> Execute()
    {
        Console.WriteLine("Workflow started");
        return Task.FromResult<object?>(null);
    }
    
}