using Silk.NET.OpenGL;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Engine.Graphics;

/// <summary>
/// Loads a material from a JSON file and creates a <see cref="Material"/> instance.
/// Supports textures, scalar/vector/matrix values, frame-context bindings
/// (semantics), and user-defined named properties (properties).
/// </summary>
public static class MaterialLoader
{
    private static readonly JsonSerializerSettings _jsonSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        MissingMemberHandling = MissingMemberHandling.Ignore,
        NullValueHandling = NullValueHandling.Ignore,
        FloatParseHandling = FloatParseHandling.Double
    };

    public static Material Load(GL gl, string materialName)
    {
        if (string.IsNullOrWhiteSpace(materialName))
        {
            throw new ArgumentException("Material name cannot be null or empty.", nameof(materialName));
        }

        string fileName = materialName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            ? materialName
            : materialName + ".json";

        string fullPath = Path.Combine(
            AppContext.BaseDirectory,
            "Resources",
            "Materials",
            fileName
        );

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Material file '{fullPath}' not found.", fullPath);
        }

        string json = File.ReadAllText(fullPath);
        var definition = JsonConvert.DeserializeObject<MaterialDefinition>(json, _jsonSettings)
                         ?? throw new InvalidOperationException($"Failed to parse material '{materialName}'.");

        if (definition.Shader is null)
        {
            throw new InvalidOperationException($"Material '{materialName}' must define a shader.");
        }

        var shader = new ShaderProgram(gl, definition.Shader.Vertex, definition.Shader.Fragment);
        var material = new Material(shader);

        if (definition.Textures is not null)
        {
            foreach (var tex in definition.Textures)
            {
                if (string.IsNullOrWhiteSpace(tex.Uniform) || string.IsNullOrWhiteSpace(tex.File))
                {
                    continue;
                }

                var texture = new Texture(gl, tex.File);
                material.AddTexture(tex.Uniform, texture, tex.Slot);
            }
        }

        if (definition.Bools is not null)
        {
            foreach (var (name, value) in definition.Bools)
            {
                material.Set(name, value);
            }
        }

        if (definition.Ints is not null)
        {
            foreach (var (name, value) in definition.Ints)
            {
                material.Set(name, value);
            }
        }

        if (definition.Floats is not null)
        {
            foreach (var (name, value) in definition.Floats)
            {
                material.Set(name, value);
            }
        }

        if (definition.Vector2s is not null)
        {
            foreach (var (name, value) in definition.Vector2s)
            {
                material.Set(name, value);
            }
        }

        if (definition.Vector3s is not null)
        {
            foreach (var (name, value) in definition.Vector3s)
            {
                material.Set(name, value);
            }
        }

        if (definition.Vector4s is not null)
        {
            foreach (var (name, value) in definition.Vector4s)
            {
                material.Set(name, value);
            }
        }

        if (definition.Matrix4s is not null)
        {
            foreach (var (name, value) in definition.Matrix4s)
            {
                material.Set(name, value);
            }
        }

        if (definition.Semantics is not null)
        {
            foreach (var (name, contextKey) in definition.Semantics)
            {
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(contextKey))
                {
                    continue;
                }

                material.BindContext(name, contextKey);
            }
        }

        if (definition.Properties is not null)
        {
            foreach (var (propertyName, uniformName) in definition.Properties)
            {
                if (string.IsNullOrWhiteSpace(propertyName) || string.IsNullOrWhiteSpace(uniformName))
                {
                    continue;
                }

                material.BindProperty(propertyName, uniformName);
            }
        }

        return material;
    }
}
