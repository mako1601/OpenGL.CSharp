using System.Numerics;
using Silk.NET.Maths;

namespace Engine.Graphics;

/// <summary>
/// Universal material.
/// Stores textures, base uniform values, bindings to frame-context keys,
/// and named properties (property -> uniform) loaded from JSON.
/// </summary>
public sealed class Material(ShaderProgram shader) : IDisposable
{
    private readonly List<TextureBinding> _textures = [];
    private readonly Dictionary<string, MaterialValue> _uniforms = [];
    private readonly Dictionary<string, string> _contextBindings = [];
    private readonly Dictionary<string, string> _properties = [];

    public ShaderProgram Shader { get; } = shader ?? throw new ArgumentNullException(nameof(shader));

    /// <summary>
    /// Adds a texture to the material and binds it to a sampler uniform.
    /// </summary>
    public void AddTexture(string uniformName, Texture texture, int slot)
    {
        if (string.IsNullOrWhiteSpace(uniformName))
        {
            throw new ArgumentException("Uniform name cannot be null or empty.", nameof(uniformName));
        }

        if (slot is < 0 or > 31)
        {
            throw new ArgumentOutOfRangeException(nameof(slot), "Texture slot must be in range 0..31.");
        }

        _textures.Add(new TextureBinding(uniformName, texture, slot));
    }

    /// <summary>
    /// Binds a uniform to a key from <see cref="MaterialContext"/>.
    /// </summary>
    public void BindContext(string uniformName, string contextKey)
    {
        if (string.IsNullOrWhiteSpace(uniformName))
        {
            throw new ArgumentException("Uniform name cannot be null or empty.", nameof(uniformName));
        }

        if (string.IsNullOrWhiteSpace(contextKey))
        {
            throw new ArgumentException("Context key cannot be null or empty.", nameof(contextKey));
        }

        _contextBindings[uniformName] = contextKey;
    }

    /// <summary>
    /// Registers a friendly property name that maps to a specific uniform.
    /// Example: "Shininess" -> "uMaterial.shininess".
    /// </summary>
    public void BindProperty(string propertyName, string uniformName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            throw new ArgumentException("Property name cannot be null or empty.", nameof(propertyName));
        }

        if (string.IsNullOrWhiteSpace(uniformName))
        {
            throw new ArgumentException("Uniform name cannot be null or empty.", nameof(uniformName));
        }

        _properties[propertyName] = uniformName;
    }

    /// <summary>
    /// Sets a base uniform value for the material.
    /// </summary>
    public void Set(string uniformName, MaterialValue value)
    {
        if (string.IsNullOrWhiteSpace(uniformName))
        {
            throw new ArgumentException("Uniform name cannot be null or empty.", nameof(uniformName));
        }

        _uniforms[uniformName] = value;
    }

    /// <summary>
    /// Sets a value using a property name registered in JSON.
    /// </summary>
    public void SetProperty(string propertyName, MaterialValue value)
    {
        string uniformName = ResolveProperty(propertyName);
        _uniforms[uniformName] = value;
    }

    /// <summary>
    /// Tries to get the property value in the requested type.
    /// </summary>
    public bool TryGetProperty<T>(string propertyName, out T value)
    {
        value = default!;

        if (!_properties.TryGetValue(propertyName, out var uniformName))
        {
            return false;
        }

        if (!_uniforms.TryGetValue(uniformName, out var uniformValue))
        {
            return false;
        }

        if (uniformValue.Value is T typed)
        {
            value = typed;
            return true;
        }

        return false;
    }

    public bool HasProperty(string propertyName) => _properties.ContainsKey(propertyName);
    public bool Remove(string uniformName) => _uniforms.Remove(uniformName);
    public bool RemoveContextBinding(string uniformName) => _contextBindings.Remove(uniformName);
    public bool RemoveProperty(string propertyName) => _properties.Remove(propertyName);

    /// <summary>
    /// Applies the material in priority order:
    /// 1) shader and textures,
    /// 2) base material parameters,
    /// 3) values from the context by key,
    /// 4) override parameters from <see cref="MaterialPropertyBlock"/>.
    /// </summary>
    public void Apply(MaterialContext? context = null, MaterialPropertyBlock? overrides = null)
    {
        Shader.Use();

        foreach (var binding in _textures)
        {
            binding.Texture.Bind((uint)binding.Slot);
            Shader.SetInt(binding.UniformName, binding.Slot);
        }

        foreach (var (uniformName, value) in _uniforms)
        {
            ApplyValue(uniformName, value);
        }

        if (context is not null)
        {
            foreach (var (uniformName, contextKey) in _contextBindings)
            {
                if (context.TryGet(contextKey, out var value))
                {
                    ApplyValue(uniformName, value);
                }
            }
        }

        if (overrides is not null)
        {
            foreach (var (uniformName, value) in overrides.Values)
            {
                ApplyValue(uniformName, value);
            }
        }
    }

    public void Dispose()
    {
        foreach (var binding in _textures)
        {
            binding.Texture.Dispose();
        }

        Shader.Dispose();
    }

    private string ResolveProperty(string propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            throw new ArgumentException("Property name cannot be null or empty.", nameof(propertyName));
        }

        if (!_properties.TryGetValue(propertyName, out var uniformName))
        {
            throw new KeyNotFoundException($"Property '{propertyName}' is not defined for this material.");
        }

        return uniformName;
    }

    private void ApplyValue(string uniformName, MaterialValue value)
    {
        switch (value.Value)
        {
            case bool v:
                Shader.SetBool(uniformName, v);
                return;
            case int v:
                Shader.SetInt(uniformName, v);
                return;
            case float v:
                Shader.SetFloat(uniformName, v);
                return;
            case Vector2 v:
                Shader.SetVector2(uniformName, v);
                return;
            case Vector3 v:
                Shader.SetVector3(uniformName, v);
                return;
            case Vector4 v:
                Shader.SetVector4(uniformName, v);
                return;
            case Matrix4x4 v:
                Shader.SetMatrix4(uniformName, v);
                return;
            case Matrix4X4<float> v:
                Shader.SetMatrix4(uniformName, v);
                return;
            default:
                throw new NotSupportedException($"Uniform '{uniformName}' has unsupported type '{value.Value.GetType().Name}'.");
        }
    }

    private sealed record TextureBinding(string UniformName, Texture Texture, int Slot);
}
