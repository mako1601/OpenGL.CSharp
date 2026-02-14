using System.Numerics;

namespace Engine.Geometry;

public static class Cone
{
    public static MeshPrimitive Create(Vector3 size, MeshPrimitiveConfig? config = null)
    {
        if (size.X <= 0 || size.Y <= 0 || size.Z <= 0)
        {
            throw new ArgumentException($"Size must be positive and non-zero in all dimensions. Received: {size}");
        }

        config ??= new MeshPrimitiveConfig();

        float radiusX = 0.5f * size.X;
        float radiusZ = 0.5f * size.Z;
        float height = size.Y;

        int vertexSize = 3;
        if (config.HasNormals)   vertexSize += 3;
        if (config.HasUV)        vertexSize += 2;
        if (config.HasNormalMap) vertexSize += 6;

        float[] vertices = new float[((config.Slices + 1) * (config.Stacks + 1) + config.Slices + 2) * vertexSize];
        uint[] indices = new uint[config.Slices * config.Stacks * 6 + config.Slices * 3];

        int vertexOffset = 0;
        int indexOffset = 0;

        // side
        for (int y = 0; y <= config.Stacks; y++)
        {
            float v = (float)y / config.Stacks;

            for (int x = 0; x <= config.Slices; x++)
            {
                float u = (float)x / config.Slices;
                float angle = u * 2f * MathF.PI;
                float cos = MathF.Cos(angle);
                float sin = MathF.Sin(angle);
                var pos = new Vector3(radiusX * (1f - v) * cos, v * height - height / 2f, radiusZ * (1f - v) * sin);
                vertices[vertexOffset++] = pos.X;
                vertices[vertexOffset++] = pos.Y;
                vertices[vertexOffset++] = pos.Z;

                if (config.HasNormals)
                {
                    var n = Vector3.Normalize(new Vector3(cos * height / radiusX, radiusX / height, sin * height / radiusZ));
                    vertices[vertexOffset++] = n.X;
                    vertices[vertexOffset++] = n.Y;
                    vertices[vertexOffset++] = n.Z;
                }

                if (config.HasUV)
                {
                    vertices[vertexOffset++] = config.StretchTexture ? u : u * size.X;
                    vertices[vertexOffset++] = config.StretchTexture ? v : v * size.Y;
                }

                if (config.HasNormalMap)
                {
                    var tangent = new Vector3(-sin, 0f, cos);
                    var bitangent = Vector3.Cross(Vector3.Normalize(new Vector3(cos * height / radiusX, radiusX / height, sin * height / radiusZ)), tangent);
                    vertices[vertexOffset++] = tangent.X;
                    vertices[vertexOffset++] = tangent.Y;
                    vertices[vertexOffset++] = tangent.Z;
                    vertices[vertexOffset++] = bitangent.X;
                    vertices[vertexOffset++] = bitangent.Y;
                    vertices[vertexOffset++] = bitangent.Z;
                }
            }
        }

        for (int y = 0; y < config.Stacks; y++)
        {
            for (int x = 0; x < config.Slices; x++)
            {
                uint i0 = (uint)(y * (config.Slices + 1) + x);
                uint i1 = i0 + 1;
                uint i2 = i0 + config.Slices + 1;
                uint i3 = i2 + 1;

                indices[indexOffset++] = i0;
                indices[indexOffset++] = i2;
                indices[indexOffset++] = i1;

                indices[indexOffset++] = i1;
                indices[indexOffset++] = i2;
                indices[indexOffset++] = i3;
            }
        }

        // base center
        vertices[vertexOffset++] = 0f;
        vertices[vertexOffset++] = -height / 2f;
        vertices[vertexOffset++] = 0f;

        if (config.HasNormals)
        {
            vertices[vertexOffset++] = 0f;
            vertices[vertexOffset++] = -1f;
            vertices[vertexOffset++] = 0f;
        }

        if (config.HasUV)
        {
            vertices[vertexOffset++] = config.StretchTexture ? 0.5f : 0.5f * size.X;
            vertices[vertexOffset++] = config.StretchTexture ? 0.5f : 0.5f * size.Z;
        }

        if (config.HasNormalMap)
        {
            vertices[vertexOffset++] = 1f;
            vertices[vertexOffset++] = 0f;
            vertices[vertexOffset++] = 0f;
            vertices[vertexOffset++] = 0f;
            vertices[vertexOffset++] = 0f;
            vertices[vertexOffset++] = -1f;
        }

        // base circles
        for (int i = 0; i <= config.Slices; i++)
        {
            float u = (float)i / config.Slices;
            float angle = u * 2f * MathF.PI;
            float cos = MathF.Cos(angle);
            float sin = MathF.Sin(angle);
            var pos = new Vector3(radiusX * cos, -height / 2f, radiusZ * sin);
            vertices[vertexOffset++] = pos.X;
            vertices[vertexOffset++] = pos.Y;
            vertices[vertexOffset++] = pos.Z;

            if (config.HasNormals)
            {
                vertices[vertexOffset++] = 0f;
                vertices[vertexOffset++] = -1f;
                vertices[vertexOffset++] = 0f;
            }

            if (config.HasUV)
            {
                vertices[vertexOffset++] = config.StretchTexture ? cos * 0.5f + 0.5f : (cos * 0.5f + 0.5f) * size.X;
                vertices[vertexOffset++] = config.StretchTexture ? sin * 0.5f + 0.5f : (sin * 0.5f + 0.5f) * size.Z;
            }

            if (config.HasNormalMap)
            {
                vertices[vertexOffset++] = 1f;
                vertices[vertexOffset++] = 0f;
                vertices[vertexOffset++] = 0f;
                vertices[vertexOffset++] = 0f;
                vertices[vertexOffset++] = 0f;
                vertices[vertexOffset++] = -1f;
            }
        }

        for (int i = 0; i < config.Slices; i++)
        {
            int i0 = (config.Slices + 1) * (config.Stacks + 1);
            int i1 = i0 + 1 + i;
            int i2 = i0 + 1 + ((i + 1) % (config.Slices + 1));

            indices[indexOffset++] = (uint)i0;
            indices[indexOffset++] = (uint)i1;
            indices[indexOffset++] = (uint)i2;
        }

        return new MeshPrimitive(vertices, indices);
    }
}
