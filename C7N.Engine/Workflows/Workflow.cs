using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace C7N.Engine.Workflows;

using C7N.Engine.Nodes;

using C7N.Engine.Connections;

public class Workflow
{
    public required string Id { get; init; }
    public required string Name { get; init; }

    public List<Node> Nodes { get; init; } = [];
    public List<Connection> Connections { get; init; } = [];
}