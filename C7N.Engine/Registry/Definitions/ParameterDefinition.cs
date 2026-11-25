using System.Text.Json.Serialization;

namespace C7N.Engine.Registry.Definitions;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ParameterFieldType
{
    String,
    Number,
    Boolean,
    Options,
    Json
}

public record ParameterDefinition(
    string Name,
    ParameterFieldType Type,
    List<string>? Options = null
);