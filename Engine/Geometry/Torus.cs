using System.Numerics;

namespace Engine.Geometry;

public static class Torus
{
    public static MeshPrimitive Create(
        Vector3 size,
        ushort slices       = 16,
        ushort stacks       = 8,
        bool normal         = true,
        bool uv             = true,
        bool normalMap      = true,
        bool stretchTexture = true
    )
    {
        if (size.X <= 0 || size.Y <= 0 || size.Z <= 0)
        {
            throw new ArgumentException($"Size must be positive and non-zero in all dimensions. Received: {size}");
        }

        float radiusX = 0.5f * size.X;
        float radiusZ = 0.5f * size.Z;
        float radiusY = 0.5f * size.Y;

        int vertexSize = 3;
        if (normal)     vertexSize += 3;
        if (uv)         vertexSize += 2;
        if (normalMap)  vertexSize += 6;

        float[] vertices = new float[(slices + 1) * (stacks + 1) * vertexSize];
        uint[] indices = new uint[slices * stacks * 6];

        int vertexOffset = 0;
        int indexOffset = 0;

        for (int i = 0; i <= slices; i++)
        {
            float u = (float)i / slices;
            float theta = u * 2f * MathF.PI;
            float cosTheta = MathF.Cos(theta);
            float sinTheta = MathF.Sin(theta);
            var center = new Vector3(radiusX * cosTheta, 0f, radiusZ * sinTheta);

            for (int j = 0; j <= stacks; j++)
            {
                float v = (float)j / stacks;
                float phi = v * MathF.PI * 2f;
                float cosPhi = MathF.Cos(phi);
                float sinPhi = MathF.Sin(phi);
                var n = new Vector3(cosTheta * cosPhi, sinPhi, sinTheta * cosPhi);
                Vector3 pos = center + n * radiusY;
                vertices[vertexOffset++] = pos.X;
                vertices[vertexOffset++] = pos.Y;
                vertices[vertexOffset++] = pos.Z;

                if (normal)
                {
                    vertices[vertexOffset++] = n.X;
                    vertices[vertexOffset++] = n.Y;
                    vertices[vertexOffset++] = n.Z;
                }

                if (uv)
                {
                    vertices[vertexOffset++] = stretchTexture ? u : u * size.X;
                    vertices[vertexOffset++] = stretchTexture ? v : v * size.Y;
                }

                if (normalMap)
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

        for (int slice = 0; slice < slices; slice++)
        {
            for (int stack = 0; stack < stacks; stack++)
            {
                uint i0 = (uint)((slice * (stacks + 1)) + stack);
                uint i1 = (uint)(((slice + 1) * (stacks + 1)) + stack);

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
