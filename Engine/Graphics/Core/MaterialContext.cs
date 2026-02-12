namespace Engine.Graphics;

/// <summary>
/// Frame context with arbitrary named parameters
/// that can be mapped to material uniforms.
/// </summary>
public sealed class MaterialContext
{
    private readonly Dictionary<string, MaterialValue> _values = [];

    public void Set(string key, MaterialValue value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Context key cannot be null or empty.", nameof(key));
        }

        _values[key] = value;
    }

    public bool TryGet(string key, out MaterialValue value) => _values.TryGetValue(key, out value);
    public bool Remove(string key) => _values.Remove(key);
    public void Clear() => _values.Clear();
}
