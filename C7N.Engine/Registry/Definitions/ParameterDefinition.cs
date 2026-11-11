namespace C7N.Engine.Registry.Definitions;

public record ParameterDefinition(
    string Name,
    string Type,
    List<string>? Options = null
);
