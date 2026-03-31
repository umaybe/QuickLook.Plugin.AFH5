using System.Linq;
using System.Collections.Generic;

namespace QuickLook.Plugin.AFH5;

public static class DictionaryExtensions
{
    public static TValue GetValueOrDefault<TKey, TValue>(
        this IDictionary<TKey, TValue> dictionary,
        TKey key)
    {
        dictionary.TryGetValue(key, out TValue value);
        return value;
    }

    public static TValue GetValueOrDefault<TKey, TValue>(
        this IDictionary<TKey, TValue> dictionary,
        TKey key,
        TValue defaultValue)
    {
        if (dictionary.TryGetValue(key, out TValue value))
            return value;
        return defaultValue;
    }
}

public static class BoundaryExtensions
{
    public static string ToFormattedString(this IBoundary boundary)
    {
        var props = boundary.GetType().GetProperties()
            .Select(p => new { Name = p.Name, Value = p.GetValue(boundary, null) })
            .Where(x => x.Value != null && !string.IsNullOrEmpty(x.Value.ToString()))
            .Select(x => $"    {x.Name}: {x.Value}");
        var info = string.Join("\n", props);
        info += "\n";
        return info;
    }
}
