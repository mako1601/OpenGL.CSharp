using System.Numerics;

namespace Engine.Geometry;

public static class Plane
{
    public static MeshPrimitive Create(Vector2 size, MeshPrimitiveConfig? config = null)
    {
        if (size.X <= 0 || size.Y <= 0)
        {
            throw new ArgumentException($"Size must be positive and non-zero. Received: {size}");
        }

        config ??= new MeshPrimitiveConfig();

        Vector2 halfSize = size * 0.5f;

        ReadOnlySpan<Vector3> positions =
        [
            new(-halfSize.X, 0f,  halfSize.Y), // Top Left
            new( halfSize.X, 0f,  halfSize.Y), // Top Right
            new( halfSize.X, 0f, -halfSize.Y), // Bottom Right
            new(-halfSize.X, 0f, -halfSize.Y), // Bottom Left
        ];

        Span<Vector2> texCoords = stackalloc Vector2[4];
        if (config.StretchTexture)
        {
            texCoords[0] = new Vector2(0, 0);
            texCoords[1] = new Vector2(1, 0);
            texCoords[2] = new Vector2(1, 1);
            texCoords[3] = new Vector2(0, 1);
        }
        else
        {
            texCoords[0] = new Vector2(0, 0);
            texCoords[1] = new Vector2(size.X, 0);
            texCoords[2] = new Vector2(size.X, size.Y);
            texCoords[3] = new Vector2(0, size.Y);
        }

        Vector3 tangent = Vector3.Zero;
        Vector3 bitangent = Vector3.Zero;

        if (config.HasNormalMap)
        {
            Vector3 edge1 = positions[1] - positions[0];
            Vector3 edge2 = positions[3] - positions[0];
            Vector2 deltaUV1 = texCoords[1] - texCoords[0];
            Vector2 deltaUV2 = texCoords[3] - texCoords[0];
            float f = 1.0f / (deltaUV1.X * deltaUV2.Y - deltaUV2.X * deltaUV1.Y);
            tangent = f * (deltaUV2.Y * edge1 - deltaUV1.Y * edge2);
            bitangent = f * (-deltaUV2.X * edge1 + deltaUV1.X * edge2);
        }

        int vertexSize = 3;
        if (config.HasNormals)   vertexSize += 3;
        if (config.HasUV)        vertexSize += 2;
        if (config.HasNormalMap) vertexSize += 6;

        Vector3 faceNormal = new(0f, 1f, 0f);

        float[] vertices = new float[4 * vertexSize];

        int vertexOffset = 0;

        for (int i = 0; i < 4; i++)
        {
            Vector3 pos = positions[i];
            vertices[vertexOffset++] = pos.X;
            vertices[vertexOffset++] = pos.Y;
            vertices[vertexOffset++] = pos.Z;

            if (config.HasNormals)
            {
                vertices[vertexOffset++] = faceNormal.X;
                vertices[vertexOffset++] = faceNormal.Y;
                vertices[vertexOffset++] = faceNormal.Z;
            }

            if (config.HasUV)
            {
                vertices[vertexOffset++] = texCoords[i].X;
                vertices[vertexOffset++] = texCoords[i].Y;
            }

            if (config.HasNormalMap)
            {
                vertices[vertexOffset++] = tangent.X;
                vertices[vertexOffset++] = tangent.Y;
                vertices[vertexOffset++] = tangent.Z;
                vertices[vertexOffset++] = bitangent.X;
                vertices[vertexOffset++] = bitangent.Y;
                vertices[vertexOffset++] = bitangent.Z;
            }
        }

        uint[] indices = [0, 1, 2, 2, 3, 0];

        return new MeshPrimitive(vertices, indices);
    }
}
