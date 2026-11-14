using C7N.Engine.Workflows;
using C7N.Engine.Nodes;

namespace C7N.Engine.Execution;

public class WorkflowExecution
{
    private ILookup<string, string> _childrenOf;
    private ILookup<string, string> _parentsOf;
    private Dictionary<string, NodeExecution> _nodesExecutionById;
    public WorkflowExecution(Workflow workflow)
    {
        this._childrenOf = workflow.Connections.ToLookup(c => c.SourceNodeId, c => c.TargetNodeId);
        this._parentsOf = workflow.Connections.ToLookup(c => c.TargetNodeId, c => c.SourceNodeId);
        this._nodesExecutionById = workflow.Nodes.Select(n => new NodeExecution(n)).ToDictionary(n => n.Node.Id);
    }
    
    public async Task<object?> Execute()
    {
        var starter =
            _nodesExecutionById.Values
            .FirstOrDefault(e => e.Node is ManualTriggerNode);
        
        //foreach (var child in this.ChildrenOf[starter?.Id ?? ""])
        //{
        //    await this.NodesById[child].Execute(null);
        //}
        
        return "";
    }
}