namespace C7N.Engine.Nodes;

public class SetNode(string id, string name) : WorkflowNode(id, name, "set")
{
    public override Task Execute()
    {
        Console.WriteLine("Node-Set started");
        return Task.CompletedTask;
    }
    
}