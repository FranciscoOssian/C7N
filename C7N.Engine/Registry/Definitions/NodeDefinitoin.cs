namespace C7N.Engine.Registry.Definitions;

public record NodeDefinition(
    string Type,
    List<ParameterDefinition> Parameters
);