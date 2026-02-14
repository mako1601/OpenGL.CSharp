using System.Numerics;

namespace Engine.Geometry;

public static class Sphere
{
    public static MeshPrimitive Create(Vector3 size, MeshPrimitiveConfig? config = null)
    {
        if (size.X <= 0 || size.Y <= 0 || size.Z <= 0)
        {
            throw new ArgumentException($"Size must be positive and non-zero in all dimensions. Received: {size}");
        }

        config ??= new MeshPrimitiveConfig();

        int vertexSize = 3;
        if (config.HasNormals)   vertexSize += 3;
        if (config.HasUV)        vertexSize += 2;
        if (config.HasNormalMap) vertexSize += 6;

        float[] vertices = new float[(config.Stacks + 1) * (config.Slices + 1) * vertexSize];
        uint[] indices = new uint[config.Stacks * config.Slices * 6];

        int vertexOffset = 0;
        int indexOffset = 0;

        for (uint i = 0; i <= config.Slices; i++)
        {
            float v = (float)i / config.Slices;
            float theta = MathF.PI / 2f - v * MathF.PI;
            float sinTheta = MathF.Sin(theta);
            float cosTheta = MathF.Cos(theta);

            float y = size.Y * sinTheta;
            float r = cosTheta;

            for (uint j = 0; j <= config.Stacks; j++)
            {
                float u = (float)j / config.Stacks;
                float phi = u * 2f * MathF.PI;
                float cosPhi = MathF.Cos(phi);
                float sinPhi = MathF.Sin(phi);
                float x = r * cosPhi;
                float z = r * sinPhi;
                var n = Vector3.Normalize(new Vector3(x / size.X, sinTheta / size.Y, z / size.Z));
                vertices[vertexOffset++] = x * size.X;
                vertices[vertexOffset++] = y;
                vertices[vertexOffset++] = z * size.Z;


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
                    var tangent = Vector3.Normalize(new Vector3(-sinPhi * size.X, 0f, cosPhi * size.Z));
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
                uint i0 = (uint)(slice * (config.Stacks + 1) + stack);
                uint i1 = (uint)((slice + 1) * (config.Stacks + 1) + stack);

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
