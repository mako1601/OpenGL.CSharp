using System.Numerics;

namespace Engine.Geometry;

public static class Cylinder
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
        float height = size.Y;

        int vertexSize = 3;
        if (normal)     vertexSize += 3;
        if (uv)         vertexSize += 2;
        if (normalMap)  vertexSize += 6;

        float[] vertices = new float[((slices + 1) * (stacks + 1) + (slices + 1) * 2 + 2) * vertexSize];
        uint[] indices = new uint[slices * stacks * 6 + slices * 6];

        int vertOffset = 0;
        int indexOffset = 0;

        // side
        for (int y = 0; y <= stacks; y++)
        {
            float v = (float)y / stacks;

            for (int x = 0; x <= slices; x++)
            {
                float u = (float)x / slices;
                float angle = u * 2f * MathF.PI;
                float cos = MathF.Cos(angle);
                float sin = MathF.Sin(angle);
                var pos = new Vector3(radiusX * cos, v * height - height * 0.5f, radiusZ * sin);
                vertices[vertOffset++] = pos.X;
                vertices[vertOffset++] = pos.Y;
                vertices[vertOffset++] = pos.Z;

                if (normal)
                {
                    Vector3 n = Vector3.Normalize(new Vector3(cos * radiusZ, 0f, sin * radiusX));
                    vertices[vertOffset++] = n.X;
                    vertices[vertOffset++] = n.Y;
                    vertices[vertOffset++] = n.Z;
                }

                if (uv)
                {
                    vertices[vertOffset++] = stretchTexture ? u : u * size.X;
                    vertices[vertOffset++] = stretchTexture ? v : v * size.Y;
                }

                if (normalMap)
                {
                    var tangent = Vector3.Normalize(new Vector3(-sin, 0f, cos));
                    var bitangent = Vector3.UnitY;
                    vertices[vertOffset++] = tangent.X;
                    vertices[vertOffset++] = tangent.Y;
                    vertices[vertOffset++] = tangent.Z;
                    vertices[vertOffset++] = bitangent.X;
                    vertices[vertOffset++] = bitangent.Y;
                    vertices[vertOffset++] = bitangent.Z;
                }
            }
        }

        for (int y = 0; y < stacks; y++)
        {
            for (int x = 0; x < slices; x++)
            {
                uint i0 = (uint)(y * (slices + 1) + x);
                uint i1 = i0 + 1;
                uint i2 = i0 + slices + 1;
                uint i3 = i2 + 1;

                indices[indexOffset++] = i0;
                indices[indexOffset++] = i2;
                indices[indexOffset++] = i1;

                indices[indexOffset++] = i1;
                indices[indexOffset++] = i2;
                indices[indexOffset++] = i3;
            }
        }

        // center of the bottom base
        vertices[vertOffset++] = 0f;
        vertices[vertOffset++] = -height / 2f;
        vertices[vertOffset++] = 0f;

        if (normal)
        {
            vertices[vertOffset++] = 0f;
            vertices[vertOffset++] = -1f;
            vertices[vertOffset++] = 0f;
        }

        if (uv)
        {
            vertices[vertOffset++] = stretchTexture ? 0.5f : 0.5f * size.X;
            vertices[vertOffset++] = stretchTexture ? 0.5f : 0.5f * size.Z;
        }

        if (normalMap)
        {
            vertices[vertOffset++] = 1f;
            vertices[vertOffset++] = 0f;
            vertices[vertOffset++] = 0f;
            vertices[vertOffset++] = 0f;
            vertices[vertOffset++] = 0f;
            vertices[vertOffset++] = -1f;
        }

        // bottom base circumference
        for (int i = 0; i <= slices; i++)
        {
            float u = (float)i / slices;
            float angle = u * 2f * MathF.PI;
            float cos = MathF.Cos(angle);
            float sin = MathF.Sin(angle);
            var pos = new Vector3(radiusX * cos, -height / 2f, radiusZ * sin);
            vertices[vertOffset++] = pos.X;
            vertices[vertOffset++] = pos.Y;
            vertices[vertOffset++] = pos.Z;

            if (normal)
            {
                vertices[vertOffset++] = 0f;
                vertices[vertOffset++] = -1f;
                vertices[vertOffset++] = 0f;
            }

            if (uv)
            {
                vertices[vertOffset++] = stretchTexture ? cos * 0.5f + 0.5f : (cos * 0.5f + 0.5f) * size.X;
                vertices[vertOffset++] = stretchTexture ? sin * 0.5f + 0.5f : (sin * 0.5f + 0.5f) * size.Z;
            }

            if (normalMap)
            {
                vertices[vertOffset++] = 1f;
                vertices[vertOffset++] = 0f;
                vertices[vertOffset++] = 0f;
                vertices[vertOffset++] = 0f;
                vertices[vertOffset++] = 0f;
                vertices[vertOffset++] = -1f;
            }
        }

        uint bottomCenterIndex = (uint)((slices + 1) * (stacks + 1));
        for (int i = 0; i < slices; i++)
        {
            indices[indexOffset++] = bottomCenterIndex;
            indices[indexOffset++] = bottomCenterIndex + (uint)i + 1;
            indices[indexOffset++] = bottomCenterIndex + (uint)i + 2;
        }

        // center of the upper base
        vertices[vertOffset++] = 0f;
        vertices[vertOffset++] = height / 2f;
        vertices[vertOffset++] = 0f;

        if (normal)
        {
            vertices[vertOffset++] = 0f;
            vertices[vertOffset++] = 1f;
            vertices[vertOffset++] = 0f;
        }

        if (uv)
        {
            vertices[vertOffset++] = stretchTexture ? 0.5f : 0.5f * size.X;
            vertices[vertOffset++] = stretchTexture ? 0.5f : 0.5f * size.Z;
        }

        if (normalMap)
        {
            vertices[vertOffset++] = 1f;
            vertices[vertOffset++] = 0f;
            vertices[vertOffset++] = 0f;
            vertices[vertOffset++] = 0f;
            vertices[vertOffset++] = 0f;
            vertices[vertOffset++] = 1f;
        }

        // circumference of the upper base
        for (int i = 0; i <= slices; i++)
        {
            float angle = 2f * MathF.PI * i / slices;
            float cos = MathF.Cos(angle);
            float sin = MathF.Sin(angle);
            var pos = new Vector3(radiusX * cos, height / 2f, radiusZ * sin);
            vertices[vertOffset++] = pos.X;
            vertices[vertOffset++] = pos.Y;
            vertices[vertOffset++] = pos.Z;

            if (normal)
            {
                vertices[vertOffset++] = 0f;
                vertices[vertOffset++] = 1f;
                vertices[vertOffset++] = 0f;
            }

            if (uv)
            {
                vertices[vertOffset++] = stretchTexture ? cos * 0.5f + 0.5f : (cos * 0.5f + 0.5f) * size.X;
                vertices[vertOffset++] = stretchTexture ? sin * 0.5f + 0.5f : (sin * 0.5f + 0.5f) * size.Z;
            }

            if (normalMap)
            {
                vertices[vertOffset++] = 1f;
                vertices[vertOffset++] = 0f;
                vertices[vertOffset++] = 0f;
                vertices[vertOffset++] = 0f;
                vertices[vertOffset++] = 0f;
                vertices[vertOffset++] = 1f;
            }
        }

        uint topCenterIndex = bottomCenterIndex + (uint)(slices + 1) + 1;
        for (int i = 0; i < slices; i++)
        {
            indices[indexOffset++] = topCenterIndex;
            indices[indexOffset++] = topCenterIndex + (uint)i + 2;
            indices[indexOffset++] = topCenterIndex + (uint)i + 1;
        }

        return new MeshPrimitive(vertices, indices);
    }
}
