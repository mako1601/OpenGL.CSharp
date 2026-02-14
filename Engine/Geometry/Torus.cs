using System.Numerics;

namespace Engine.Geometry;

public static class Torus
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
        float radiusY = 0.5f * size.Y;

        int vertexSize = 3;
        if (config.HasNormals)   vertexSize += 3;
        if (config.HasUV)        vertexSize += 2;
        if (config.HasNormalMap) vertexSize += 6;

        float[] vertices = new float[(config.Slices + 1) * (config.Stacks + 1) * vertexSize];
        uint[] indices = new uint[config.Slices * config.Stacks * 6];

        int vertexOffset = 0;
        int indexOffset = 0;

        for (int i = 0; i <= config.Slices; i++)
        {
            float u = (float)i / config.Slices;
            float theta = u * 2f * MathF.PI;
            float cosTheta = MathF.Cos(theta);
            float sinTheta = MathF.Sin(theta);
            var center = new Vector3(radiusX * cosTheta, 0f, radiusZ * sinTheta);

            for (int j = 0; j <= config.Stacks; j++)
            {
                float v = (float)j / config.Stacks;
                float phi = v * MathF.PI * 2f;
                float cosPhi = MathF.Cos(phi);
                float sinPhi = MathF.Sin(phi);
                var n = new Vector3(cosTheta * cosPhi, sinPhi, sinTheta * cosPhi);
                Vector3 pos = center + n * radiusY;
                vertices[vertexOffset++] = pos.X;
                vertices[vertexOffset++] = pos.Y;
                vertices[vertexOffset++] = pos.Z;

                if (config.HasNormals)
                {
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
                    var tangent = Vector3.Normalize(new Vector3(-radiusX * sinTheta, 0f, radiusZ * cosTheta));
                    var bitangent = Vector3.Normalize(Vector3.Cross(n, tangent));
                    vertices[vertexOffset++] = tangent.X;
                    vertices[vertexOffset++] = tangent.Y;
                    vertices[vertexOffset++] = tangent.Z;
                    vertices[vertexOffset++] = bitangent.X;
                    vertices[vertexOffset++] = bitangent.Y;
                    vertices[vertexOffset++] = bitangent.Z;
                }
            }
        }

        for (int slice = 0; slice < config.Slices; slice++)
        {
            for (int stack = 0; stack < config.Stacks; stack++)
            {
                uint i0 = (uint)((slice * (config.Stacks + 1)) + stack);
                uint i1 = (uint)(((slice + 1) * (config.Stacks + 1)) + stack);

                indices[indexOffset++] = i0;
                indices[indexOffset++] = i0 + 1;
                indices[indexOffset++] = i1;

                indices[indexOffset++] = i1;
                indices[indexOffset++] = i0 + 1;
                indices[indexOffset++] = i1 + 1;
            }
        }

        return new MeshPrimitive(vertices, indices);
    }
}
