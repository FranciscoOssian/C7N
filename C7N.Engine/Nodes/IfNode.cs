namespace C7N.Engine.Nodes;

public class IfNode(string id, string name, Dictionary<string, object> parameters) : WorkflowNode(id, name, "if", parameters)
{
    public override Task Execute()
    {
        Console.WriteLine("Node-if started");
        return Task.CompletedTask;
    }
    
}