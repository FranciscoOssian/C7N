using C7N.Engine.Nodes;

namespace C7N.Engine.Workflows;

public class WorkflowExecution(Workflow workflow)
{
    
    public async Task<string?> Execute()
    {
        var starter = workflow.Nodes.OfType<ManualTriggerNode>().FirstOrDefault();
        
        var childrenOf = workflow.Connections
            .ToLookup(c => c.SourceNodeId, c => c.TargetNodeId);
        
        var parentsOf = workflow.Connections
            .ToLookup(c => c.TargetNodeId, c => c.SourceNodeId);
        
        var nodesById = workflow.Nodes.ToDictionary(n => n.Id);

        foreach (var child in childrenOf[starter?.Id ?? ""])
        {
            await nodesById[child].Execute(null);
        }
        
        return "";
    }
}