using System.Text.Json;
using C7N.Engine.Nodes;
using C7N.Engine.Registry.Definitions;

namespace C7N.Engine.Registry;

public class NodeRegistry
{
    public List<NodeDefinition> GetAvailableNodes()
    {
        var nodeTypes = new List<Type>
        {
            typeof(HttpRequestNode),
            typeof(SetNode),
            typeof(IfNode),
            typeof(ManualTriggerNode)
        };
        
        var definitions = new List<NodeDefinition>();

        foreach (var nodeType in nodeTypes)
        {
            var parametersProp = nodeType.GetProperty("Parameters");
            if (parametersProp is null) continue;

            Type paramType = parametersProp.PropertyType;
            var properties = paramType.GetProperties();

            var parameterDefinitions = new List<ParameterDefinition>();

            foreach (var prop in properties)
            {
                string fieldName = JsonNamingPolicy.CamelCase.ConvertName(prop.Name);
                ParameterFieldType fieldType = ParameterFieldType.String;
                List<string>? options = null;

                Type actualType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                if (actualType == typeof(int) || actualType == typeof(double) || actualType == typeof(float))
                {
                    fieldType = ParameterFieldType.Number;
                }
                else if (actualType == typeof(bool))
                {
                    fieldType = ParameterFieldType.Boolean;
                }
                else if (actualType.IsEnum)
                {
                    fieldType = ParameterFieldType.Options;
                    options = Enum.GetNames(actualType).ToList();
                }
                else if (typeof(System.Collections.IDictionary).IsAssignableFrom(actualType))
                {
                    fieldType = ParameterFieldType.Json;
                }
                else
                {
                    fieldType = ParameterFieldType.String;
                }

                parameterDefinitions.Add(new ParameterDefinition(
                    Name: fieldName,
                    Type: fieldType,
                    Options: options
                ));
            }
            
            string nodeTypeIdentifier = nodeType.Name;
            List<string> outPuts = nodeType == typeof(IfNode) ? ["true", "false"] : ["main"];

            definitions.Add(new NodeDefinition(
                Type: nodeTypeIdentifier,
                Parameters: parameterDefinitions,
                Outputs:  outPuts
            ));
        }

        return definitions;
    }
}