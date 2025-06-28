using System.Numerics;

namespace Engine.Geometry;

public static class Sphere
{
    public static MeshPrimitive Create(
        Vector3 size,
        ushort slices      = 16,
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

        int vertexSize = 3;
        if (normal)     vertexSize += 3;
        if (uv)         vertexSize += 2;
        if (normalMap)  vertexSize += 6;

        float[] vertices = new float[(stacks + 1) * (slices + 1) * vertexSize];
        uint[] indices = new uint[stacks * slices * 6];

        int vertexOffset = 0;
        int indexOffset = 0;

        for (uint i = 0; i <= slices; i++)
        {
            float v = (float)i / slices;
            float theta = MathF.PI / 2f - v * MathF.PI;
            float sinTheta = MathF.Sin(theta);
            float cosTheta = MathF.Cos(theta);

            float y = size.Y * sinTheta;
            float r = cosTheta;

            for (uint j = 0; j <= stacks; j++)
            {
                float u = (float)j / stacks;
                float phi = u * 2f * MathF.PI;
                float cosPhi = MathF.Cos(phi);
                float sinPhi = MathF.Sin(phi);
                float x = r * cosPhi;
                float z = r * sinPhi;
                var n = Vector3.Normalize(new Vector3(x / size.X, sinTheta / size.Y, z / size.Z));
                vertices[vertexOffset++] = x * size.X;
                vertices[vertexOffset++] = y;
                vertices[vertexOffset++] = z * size.Z;


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

        for (int slice = 0; slice < slices; slice++)
        {
            for (int stack = 0; stack < stacks; stack++)
            {
                uint i0 = (uint)(slice * (stacks + 1) + stack);
                uint i1 = (uint)((slice + 1) * (stacks + 1) + stack);

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