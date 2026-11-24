using C7N.Engine.Nodes;
using C7N.Engine.Workflows;

namespace C7N.Engine.Execution;

public class WorkflowExecution
{
    public string Id;
    private readonly ILookup<string, string> _childrenOf;
    private readonly ILookup<string, string> _parentsOf;
    private readonly Dictionary<string, NodeExecution> _nodesExecutionById;
    private readonly List<(NodeExecution Node, Task<object?> Task)> _nodeTasks;

    public WorkflowExecution(Workflow workflow)
    {
        Id = Guid.NewGuid().ToString();
        _childrenOf = workflow.Connections.ToLookup(c => c.SourceNodeId, c => c.TargetNodeId);
        _parentsOf = workflow.Connections.ToLookup(c => c.TargetNodeId, c => c.SourceNodeId);
        _nodesExecutionById = workflow.Nodes.Select(n => new NodeExecution(n)).ToDictionary(n => n.Node.Id);
        _nodeTasks = new();
    }

    private bool IsNodeReadyToExecute(string nodeId)
    {
        var nodeExecution = _nodesExecutionById[nodeId];

        return nodeExecution.Node.ExecutionPolicy switch
        {
            NodeExecutionPolicy.EveryToken => nodeExecution.HasAnyToken(),
            NodeExecutionPolicy.WaitAll => nodeExecution.SubmittedParentsCount() == _parentsOf[nodeId].Count(),
            NodeExecutionPolicy.OnDemand => nodeExecution.CanResolveAllParameters(),
            _ => false
        };
    }

    public async Task<string?> Execute()
    {
        if (_nodesExecutionById.Values
                .FirstOrDefault(e => e.Node is ManualTriggerNode)
            is not { } starter) return null;

        _nodeTasks.Add((starter, starter.Node.Execute(null)));

        while (_nodeTasks.Count > 0)
        {
            var executedTask = await Task.WhenAny(_nodeTasks.Select(n => n.Task));
            var entry = _nodeTasks.FirstOrDefault(n => n.Task == executedTask);

            if (entry.Node is null) continue;

            var executedNode = entry.Node;
            _nodeTasks.Remove(entry);

            var response = await executedTask;
            var resp = response as Dictionary<string, object?> ?? [];

            if (executedNode.Node.ExecutionPolicy == NodeExecutionPolicy.EveryToken
                && IsNodeReadyToExecute(executedNode.Node.Id))
            {
                _nodeTasks.Add((executedNode, executedNode.Execute()));
            }

            foreach (var childId in _childrenOf[executedNode.Node.Id])
            {
                var child = _nodesExecutionById[childId];
                child.StoreTokens(executedNode.Node.Id, [resp]);

                if (IsNodeReadyToExecute(childId))
                {
                    _nodeTasks.Add((child, child.Execute()));
                }
            }
        }

        return Id;
    }
}