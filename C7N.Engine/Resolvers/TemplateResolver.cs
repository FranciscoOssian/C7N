namespace C7N.Engine.Resolvers;

using System.Text.Json;
using System.Text.RegularExpressions;

/// <summary>
/// Provides utility methods to resolve placeholder expressions within templates using runtime data.
/// </summary>
public static class TemplateResolver
{
    private static readonly Regex PlaceholderRegex = new(@"\{\{\s*([a-zA-Z0-9_.]+)\s*\}\}", RegexOptions.Compiled);

    /// <summary>
    /// Resolves placeholders inside the provided value against the root data dictionary.
    /// Non-string values are returned unchanged.
    /// Single placeholder expressions preserve their underlying primitive types.
    /// Multiple placeholders or mixed text produce a concatenated string.
    /// </summary>
    /// <param name="value">The raw input value to resolve.</param>
    /// <param name="root">The dictionary containing resolution data.</param>
    /// <returns>The resolved value or the original value if no resolution was required.</returns>
    public static object? Resolve(object? value, Dictionary<string, object?>? root)
    {
        if (value is not string template)
        {
            return value;
        }

        if (string.IsNullOrEmpty(template))
        {
            return string.Empty;
        }

        if (root is null || root.Count == 0 || !template.Contains("{{"))
        {
            return template;
        }

        var trimmed = template.Trim();

        if (trimmed.StartsWith("{{") && trimmed.EndsWith("}}") && Regex.Count(trimmed, Regex.Escape("{{")) == 1)
        {
            var path = trimmed.Replace("{{", "").Replace("}}", "").Trim();
            return GetValueByPath(root, path);
        }

        return PlaceholderRegex.Replace(template, match =>
        {
            string path = match.Groups[1].Value;
            var resolvedValue = GetValueByPath(root, path);
            return resolvedValue?.ToString() ?? string.Empty;
        });
    }

    /// <summary>
    /// Traverses a nested dictionary or JSON element structure following a dot-separated path.
    /// </summary>
    /// <param name="root">The root data dictionary.</param>
    /// <param name="path">The dot-separated property path.</param>
    /// <returns>The extracted value unpacked into its native type, or null if the path is invalid.</returns>
    private static object? GetValueByPath(Dictionary<string, object?> root, string path)
    {
        string[] parts = path.Split('.');
        object? current = root;

        foreach (var part in parts)
        {
            if (current is null)
            {
                return null;
            }

            if (current is Dictionary<string, object?> dict)
            {
                if (!dict.TryGetValue(part, out current))
                {
                    return null;
                }
            }
            else if (current is JsonElement element && element.ValueKind == JsonValueKind.Object)
            {
                if (element.TryGetProperty(part, out var property))
                {
                    current = property;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        if (current is JsonElement finalElement)
        {
            return finalElement.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Number => finalElement.TryGetInt32(out var n) ? n : finalElement.GetDouble(),
                JsonValueKind.String => finalElement.GetString(),
                JsonValueKind.Null or JsonValueKind.Undefined => null,
                _ => finalElement
            };
        }

        return current;
    }
}