namespace C7N.Engine.Nodes;

public record NodeOutput(
    Dictionary<string, object?>? Data = null,
    string OutputChannel = "main"
);