namespace Engine.Graphics;

/// <summary>
/// A set of temporary uniform overrides for a single draw call.
/// Values from this block have priority when applying the material.
/// </summary>
public sealed class MaterialPropertyBlock
{
    private readonly Dictionary<string, MaterialValue> _values = [];

    public IReadOnlyDictionary<string, MaterialValue> Values => _values;

    public void Set(string uniformName, MaterialValue value)
    {
        if (string.IsNullOrWhiteSpace(uniformName))
        {
            throw new ArgumentException("Uniform name cannot be null or empty.", nameof(uniformName));
        }

        _values[uniformName] = value;
    }

    public bool Remove(string uniformName) => _values.Remove(uniformName);
    public void Clear() => _values.Clear();
}
