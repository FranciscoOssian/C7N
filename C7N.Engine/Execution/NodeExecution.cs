using C7N.Engine.Nodes;
using System.Collections.Concurrent;

namespace C7N.Engine.Execution;

public class NodeExecution(Node node)
{
    public Node Node { get; } = node;
    private readonly ConcurrentQueue<object?>? _queue = 
        node.ExecutionPolicy == NodeExecutionPolicy.EveryToken ? new() : null;
    private readonly ConcurrentDictionary<string, List<object>>? _dictionary = 
        node.ExecutionPolicy != NodeExecutionPolicy.EveryToken ? new() : null;

    public void SendTokens(string senderId, List<object> tokens)
    {
        if (this.Node.ExecutionPolicy is NodeExecutionPolicy.EveryToken)
        {
            foreach (var token in tokens)
            {
                _queue?.Enqueue(token);
            }
        }
        else if (this.Node.ExecutionPolicy is NodeExecutionPolicy.OnDemand
                 || this.Node.ExecutionPolicy is NodeExecutionPolicy.WaitAll)
        {
            if (_dictionary != null)
            {
                var parentTokens = _dictionary.GetOrAdd(senderId, _ => new List<object>());
                lock (parentTokens)
                {
                    parentTokens.AddRange(tokens);
                }
            }
        }
    }

    private bool CanExecuteEveryToken() => _queue is { IsEmpty: false };

    private bool CanExecuteOnDemand()
    {
        //
        return false;
    }

    private bool CanExecuteWaitAll()
    {
        return false;
    }

    public bool CanExecute()
    {
        return this.Node.ExecutionPolicy switch
        {
            NodeExecutionPolicy.EveryToken => CanExecuteEveryToken(),
            NodeExecutionPolicy.OnDemand => CanExecuteOnDemand(),
            NodeExecutionPolicy.WaitAll => CanExecuteWaitAll(),
            _ => false
        };
    }
}