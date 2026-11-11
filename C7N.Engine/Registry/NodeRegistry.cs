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
                string fieldName = prop.Name;
                string fieldType = "string";
                List<string>? options = null;
                
                Type actualType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                if (actualType == typeof(int) || actualType == typeof(double) || actualType == typeof(float))
                {
                    fieldType = "number";
                }
                else if (actualType == typeof(bool))
                {
                    fieldType = "boolean";
                }
                else if (actualType.IsEnum)
                {
                    fieldType = "options";
                    options = Enum.GetNames(actualType).ToList();
                }
                else if (typeof(System.Collections.IDictionary).IsAssignableFrom(actualType))
                {
                    fieldType = "json";
                }
                else
                {
                    fieldType = "string";
                }

                parameterDefinitions.Add(new ParameterDefinition(
                    Name: fieldName,
                    Type: fieldType,
                    Options: options
                ));
            }
            
            string nodeTypeIdentifier = nodeType.Name.Replace("Node", "").ToLower();

            definitions.Add(new NodeDefinition(
                Type: nodeTypeIdentifier,
                Parameters: parameterDefinitions
            ));
        }

        return definitions;
    }
}