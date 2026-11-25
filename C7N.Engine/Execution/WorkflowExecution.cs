using System.Collections.Concurrent;
using C7N.Engine.Connections;
using C7N.Engine.Nodes;
using C7N.Engine.Workflows;

namespace C7N.Engine.Execution;

public class WorkflowExecution
{
    public string Id { get; }
    public NodeExecutionStatus Status { get; private set; }
    public ConcurrentDictionary<string, object?> Results { get; }
    private readonly ILookup<string, Connection> _childrenOf;
    private readonly ILookup<string, string> _parentsOf;
    private readonly Dictionary<string, NodeExecution> _nodesExecutionById;
    private readonly List<(NodeExecution Node, Task<NodeOutput> Task)> _nodeTasks;

    public WorkflowExecution(Workflow workflow)
    {
        Id = Guid.NewGuid().ToString();
        Status = NodeExecutionStatus.Pending;
        _childrenOf = workflow.Connections.ToLookup(c => c.SourceNodeId);
        _parentsOf = workflow.Connections.ToLookup(c => c.TargetNodeId, c => c.SourceNodeId);
        _nodesExecutionById = workflow.Nodes.Select(n => new NodeExecution(n)).ToDictionary(n => n.Node.Id);
        _nodeTasks = new();
        Results = new();
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
        try
        {
            Status = NodeExecutionStatus.Running;
            await ExecuteInternal();
            Status = NodeExecutionStatus.Completed;
        }
        catch (Exception e)
        {
            Status = NodeExecutionStatus.Failed;
            Console.WriteLine(e);
            return null;
        }

        return Id;
    }

    private async Task<string?> ExecuteInternal()
    {
        if (_nodesExecutionById.Values
                .FirstOrDefault(e => e.Node is ManualTriggerNode)
            is not { } starter) throw new InvalidOperationException("Workflow has no StarterNode.");

        _nodeTasks.Add((starter, starter.Node.Execute(null)));

        while (_nodeTasks.Count > 0)
        {
            var executedTask = await Task.WhenAny(_nodeTasks.Select(n => n.Task));
            var entry = _nodeTasks.FirstOrDefault(n => n.Task == executedTask);

            if (entry.Node is null) continue;

            var executedNode = entry.Node;
            _nodeTasks.Remove(entry);

            var response = await executedTask;
            Results[executedNode.Node.Id] = response.Data;
            var resp = response.Data ?? [];

            if (executedNode.Node.ExecutionPolicy == NodeExecutionPolicy.EveryToken
                && IsNodeReadyToExecute(executedNode.Node.Id))
            {
                _nodeTasks.Add((executedNode, executedNode.Execute()));
            }

            foreach (var conn in _childrenOf[executedNode.Node.Id])
            {
                if (conn.SourceOutputId != response.OutputChannel) continue;
                var childId = conn.TargetNodeId;
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