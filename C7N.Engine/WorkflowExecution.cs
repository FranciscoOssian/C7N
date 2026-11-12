using C7N.Engine.Nodes;

namespace C7N.Engine;

public class WorkflowExecution(Workflow workflow)
{
    public async Task<Dictionary<string, object?>> Execute()
    {
        var search =
            from node in workflow.Nodes
            where node is ManualTriggerNode
            select node;
        var starter = search.FirstOrDefault();
        
        Console.WriteLine(starter);
        
        return new Dictionary<string, object?>();
    }
}