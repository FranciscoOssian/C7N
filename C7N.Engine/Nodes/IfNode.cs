namespace C7N.Engine.Nodes;

public class IfNode(string id, string name) : WorkflowNode(id, name, "if")
{
    public override Task Execute()
    {
        Console.WriteLine("Node-if started");
        return Task.CompletedTask;
    }
    
}