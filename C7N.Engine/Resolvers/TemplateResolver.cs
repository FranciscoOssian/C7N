namespace C7N.Engine.Resolvers;

using System.Text.Json;
using System.Text.RegularExpressions;

/// <summary>
/// Provides utility methods to resolve placeholder expressions within templates using runtime data.
/// </summary>
public static class TemplateResolver
{
    private static readonly Regex PlaceholderRegex = new(@"\<\<\s*([a-zA-Z0-9_.]+)\s*\>\>", RegexOptions.Compiled);

    public static bool CanResolve(Dictionary<string, object?> values, Dictionary<string, object?> root)
    {
        Queue<Dictionary<string, object?>> toSearch = new();
        toSearch.Enqueue(values);
        while (toSearch.TryDequeue(out var current))
        {
            foreach (var (key, value) in current)
            {
                var unpacked = value is JsonElement el ? UnpackJsonElement(el) : value;

                if (unpacked is string v)
                {
                    if (PlaceholderRegex.IsMatch(v))
                    {
                        foreach (Match match in PlaceholderRegex.Matches(v))
                        {
                            string path = match.Groups[1].Value;
                            if (GetValueByPath(root, path) is null)
                                return false;
                        }
                    }
                }
                else if (unpacked is Dictionary<string, object?> dict)
                {
                    toSearch.Enqueue(dict);
                }
                else if (unpacked is JsonElement obj && obj.ValueKind == JsonValueKind.Object)
                {
                    toSearch.Enqueue(obj.EnumerateObject().ToDictionary(p => p.Name, p => (object?)p.Value));
                }
            }
        }
        return true;
    }

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
        if (value is JsonElement jsonElement)
        {
            value = UnpackJsonElement(jsonElement);
        }

        if (value is not string template)
        {
            return value;
        }

        if (string.IsNullOrEmpty(template))
        {
            return string.Empty;
        }

        if (root is null || root.Count == 0 || !template.Contains("<<"))
        {
            return template;
        }

        var trimmed = template.Trim();

        if (trimmed.StartsWith("<<") && trimmed.EndsWith(">>") && Regex.Count(trimmed, Regex.Escape("<<")) == 1)
        {
            var path = trimmed.Replace("<<", "").Replace(">>", "").Trim();
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
            return UnpackJsonElement(finalElement);
        }

        return current;
    }

    private static object? UnpackJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number => element.TryGetInt32(out var n) ? n : element.GetDouble(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            _ => element
        };
    }
}