using System.Collections.Concurrent;
using System.Text.Json;
using C7N.Engine.DataManipulation;
using C7N.Engine.Nodes;
using C7N.Engine.Resolvers;

namespace C7N.Engine.Execution;

public class NodeExecution
{
    public Node Node { get; }
    public NodeExecutionStatus Status { get; set; }
    public string? Error { get; set; }

    private readonly ConcurrentQueue<Dictionary<string, object?>?>? _queueForEveryToken;
    private readonly ConcurrentDictionary<string, object?>? _dictionaryForWaitAll;
    private readonly ConcurrentDictionary<string, object?>? _dictionaryForOnDemand;

    public NodeExecution(Node node)
    {
        Node = node;
        Status = NodeExecutionStatus.Pending;

        if (node.ExecutionPolicy is NodeExecutionPolicy.EveryToken)
            _queueForEveryToken = new ConcurrentQueue<Dictionary<string, object?>?>();
        else if (node.ExecutionPolicy is NodeExecutionPolicy.OnDemand)
            _dictionaryForOnDemand = new ConcurrentDictionary<string, object?>();
        else
            _dictionaryForWaitAll = new ConcurrentDictionary<string, object?>();
    }

    public async Task<NodeOutput> Execute()
    {
        if (Node.ExecutionPolicy is NodeExecutionPolicy.EveryToken)
        {
            if (_queueForEveryToken?.TryDequeue(out var token) ?? false)
                return await Node.Execute(token);

            return new NodeOutput();
        }

        if (Node.ExecutionPolicy is NodeExecutionPolicy.OnDemand)
        {
            var input = new Dictionary<string, object?>(_dictionaryForOnDemand ?? []);
            _dictionaryForOnDemand?.Clear();
            return await Node.Execute(input);
        }

        if (Node.ExecutionPolicy is NodeExecutionPolicy.WaitAll)
        {
            var input = new Dictionary<string, object?>();

            if (_dictionaryForWaitAll is not null)
            {
                foreach (var parentData in _dictionaryForWaitAll.Values)
                {
                    if (parentData is Dictionary<string, object?> dict)
                    {
                        input = Merge.DeepMerge(input, dict);
                    }
                }

                _dictionaryForWaitAll.Clear();
            }

            return await Node.Execute(input);
        }

        return new NodeOutput();
    }

    public void StoreTokens(string senderId, List<Dictionary<string, object?>> tokens)
    {
        if (Node.ExecutionPolicy is NodeExecutionPolicy.EveryToken)
        {
            foreach (var token in tokens)
            {
                _queueForEveryToken?.Enqueue(token);
            }
        }
        else if (Node.ExecutionPolicy is NodeExecutionPolicy.WaitAll)
        {
            if (_dictionaryForWaitAll is not null)
                _dictionaryForWaitAll[senderId] = tokens.FirstOrDefault();
        }
        else if (Node.ExecutionPolicy is NodeExecutionPolicy.OnDemand)
        {
            if (_dictionaryForOnDemand == null || tokens.FirstOrDefault() is not { } dict) return;

            var merged = Merge.DeepMerge(_dictionaryForOnDemand, dict);
            foreach (var (k, v) in merged)
            {
                _dictionaryForOnDemand[k] = v;
            }
        }
        else
        {
            throw new ArgumentException("Fail to process node execution policy");
        }
    }

    public bool HasAnyToken()
    {
        if (Node.ExecutionPolicy is NodeExecutionPolicy.EveryToken)
            return _queueForEveryToken is { IsEmpty: false };

        if (Node.ExecutionPolicy is NodeExecutionPolicy.OnDemand || Node.ExecutionPolicy is NodeExecutionPolicy.WaitAll)
            throw new ArgumentException("In the `OnDemand` and `WaitAll` policy, it's not necessary to calculate if has tokens; there must have been an error.");

        return false;
    }

    public int SubmittedParentsCount()
    {
        if (Node.ExecutionPolicy is NodeExecutionPolicy.EveryToken || Node.ExecutionPolicy is NodeExecutionPolicy.OnDemand)
            throw new ArgumentException("In the `EveryToken` and `OnDemand` policy, it's not necessary to calculate how many parents submitted tokens; there must have been an error.");

        return _dictionaryForWaitAll?.Keys.Count ?? 0;
    }

    public bool CanResolveAllParameters()
    {
        var parametersObj = Node.GetType().GetProperty("Parameters")?.GetValue(Node);
        var json = JsonSerializer.Serialize(parametersObj);
        var values = JsonSerializer.Deserialize<Dictionary<string, object?>>(json) ?? [];
        var root = new Dictionary<string, object?>(_dictionaryForOnDemand ?? []);
        return TemplateResolver.CanResolve(values, root);
    }
}