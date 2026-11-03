namespace C7N.Engine.Nodes;

public class ManualTriggerNode(string id, string name) : WorkflowNode(id, name, "manual-trigger")
{
    public override Task Execute()
    {
        Console.WriteLine("Workflow started");
        return Task.CompletedTask;
    }
    
}