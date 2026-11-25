using System.Threading.Channels;
using System.Collections.Concurrent;
using C7N.Engine.Connections;
using C7N.Engine.Nodes;
using C7N.Engine.Workflows;

namespace C7N.Engine.Execution;

public class WorkflowExecution
{
    public string Id { get; }
    public NodeExecutionStatus Status { get; private set; }
    public ConcurrentDictionary<string, Dictionary<string, object?>?> Results { get; }
    private readonly ILookup<string, Connection> _childrenOf;
    private readonly ILookup<string, string> _parentsOf;
    private readonly Dictionary<string, NodeExecution> _nodesExecutionById;
    private readonly List<(NodeExecution Node, Task<NodeOutput> Task)> _nodeTasks;
    private readonly ConcurrentDictionary<Guid, ChannelWriter<ExecutionEvent>> _subscribers;

    public WorkflowExecution(Workflow workflow)
    {
        Id = Guid.NewGuid().ToString();
        Status = NodeExecutionStatus.Pending;
        _childrenOf = workflow.Connections.ToLookup(c => c.SourceNodeId);
        _parentsOf = workflow.Connections.ToLookup(c => c.TargetNodeId, c => c.SourceNodeId);
        _nodesExecutionById = workflow.Nodes.Select(n => new NodeExecution(n)).ToDictionary(n => n.Node.Id);
        _nodeTasks = new();
        Results = new();
        _subscribers = new();
    }

    public WorkflowExecutionSnapshot GetWorkflowSnapshot()
    {
        var nodes = _nodesExecutionById.ToDictionary(
            kvp => kvp.Key,
            kvp => new NodeExecutionSnapshot(
                Status: kvp.Value.Status,
                Data: Results.TryGetValue(kvp.Key, out var data) ? data : null,
                Error: kvp.Value.Error
            )
        );

        return new WorkflowExecutionSnapshot(Id, Status, nodes);
    }
    
    public (Guid SubscriptionId, ChannelReader<ExecutionEvent> Reader) Subscribe()
    {
        var channel = Channel.CreateUnbounded<ExecutionEvent>();
        var id = Guid.NewGuid();
        _subscribers[id] = channel.Writer;

        if (Status is NodeExecutionStatus.Completed or NodeExecutionStatus.Failed)
        {
            _subscribers.TryRemove(id, out _);
            channel.Writer.TryComplete();
        }

        return (id, channel.Reader);
    }

    public void Unsubscribe(Guid subscriptionId)
    {
        if (_subscribers.TryRemove(subscriptionId, out var writer))
        {
            writer.TryComplete();
        }
    }

    private void Publish(ExecutionEvent evt)
    {
        foreach (var writer in _subscribers.Values)
        {
            writer.TryWrite(evt);
        }
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
            Publish(new ExecutionEvent(ExecutionEventType.WorkflowRunning, new WorkflowEventPayload(Status)));

            await ExecuteInternal();

            Status = NodeExecutionStatus.Completed;
            Publish(new ExecutionEvent(ExecutionEventType.WorkflowCompleted, new WorkflowEventPayload(Status)));
        }
        catch (Exception e)
        {
            Status = NodeExecutionStatus.Failed;
            Publish(new ExecutionEvent(ExecutionEventType.WorkflowFailed, new WorkflowEventPayload(Status, e.Message)));
            Console.WriteLine(e);
            return null;
        }
        finally
        {
            foreach (var writer in _subscribers.Values)
            {
                writer.TryComplete();
            }
            _subscribers.Clear();
        }

        return Id;
    }

    private void ScheduleNode(NodeExecution nodeExecution, Task<NodeOutput> task)
    {
        nodeExecution.Status = NodeExecutionStatus.Running;
        nodeExecution.Error = null;
        Publish(new ExecutionEvent(
            ExecutionEventType.NodeRunning,
            new NodeEventPayload(
                nodeExecution.Node.Id,
                new NodeExecutionSnapshot(NodeExecutionStatus.Running)
            )
        ));
        _nodeTasks.Add((nodeExecution, task));
    }

    private async Task<string?> ExecuteInternal()
    {
        if (_nodesExecutionById.Values
                .FirstOrDefault(e => e.Node is ManualTriggerNode)
            is not { } starter) throw new InvalidOperationException("Workflow has no StarterNode.");

        ScheduleNode(starter, starter.Node.Execute(null));

        while (_nodeTasks.Count > 0)
        {
            var executedTask = await Task.WhenAny(_nodeTasks.Select(n => n.Task));
            var entry = _nodeTasks.FirstOrDefault(n => n.Task == executedTask);

            if (entry.Node is null) continue;

            var executedNode = entry.Node;
            _nodeTasks.Remove(entry);

            NodeOutput response;
            try
            {
                response = await executedTask;
            }
            catch (Exception ex)
            {
                executedNode.Status = NodeExecutionStatus.Failed;
                executedNode.Error = ex.Message;
                Publish(new ExecutionEvent(
                    ExecutionEventType.NodeFailed,
                    new NodeEventPayload(
                        executedNode.Node.Id,
                        new NodeExecutionSnapshot(NodeExecutionStatus.Failed, Error: ex.Message)
                    )
                ));
                throw;
            }

            executedNode.Status = NodeExecutionStatus.Completed;
            Results[executedNode.Node.Id] = response.Data;
            Publish(new ExecutionEvent(
                ExecutionEventType.NodeCompleted,
                new NodeEventPayload(
                    executedNode.Node.Id,
                    new NodeExecutionSnapshot(NodeExecutionStatus.Completed, Data: response.Data)
                )
            ));
            var resp = response.Data ?? [];

            if (executedNode.Node.ExecutionPolicy == NodeExecutionPolicy.EveryToken
                && IsNodeReadyToExecute(executedNode.Node.Id))
            {
                ScheduleNode(executedNode, executedNode.Execute());
            }

            foreach (var conn in _childrenOf[executedNode.Node.Id])
            {
                if (conn.SourceOutputId != response.OutputChannel) continue;
                var childId = conn.TargetNodeId;
                var child = _nodesExecutionById[childId];
                child.StoreTokens(executedNode.Node.Id, [resp]);

                if (IsNodeReadyToExecute(childId))
                {
                    ScheduleNode(child, child.Execute());
                }
            }
        }

        return Id;
    }
}