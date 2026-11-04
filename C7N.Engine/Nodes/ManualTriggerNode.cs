namespace C7N.Engine.Nodes;

public class ManualTriggerNode(string id, string name, Dictionary<string, object> parameters) : WorkflowNode(id, name, "manual-trigger", parameters)
{
    public override Task Execute()
    {
        Console.WriteLine("Workflow started");
        return Task.CompletedTask;
    }
    
}