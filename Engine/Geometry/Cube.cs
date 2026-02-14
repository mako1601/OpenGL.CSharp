using System.Numerics;
using Silk.NET.OpenGL;

namespace Engine.Geometry;

public static class Cube
{
    public static MeshPrimitive Create(Vector3 size, MeshPrimitiveConfig? config = null)
    {
        if (size.X <= 0 || size.Y <= 0 || size.Z <= 0)
        {
            throw new ArgumentException($"Size must be positive and non-zero in all dimensions. Received: {size}");
        }

        config ??= new MeshPrimitiveConfig();
        Vector3 halfSize = size * 0.5f;

        ReadOnlySpan<Vector3> positions =
        [
            new(-halfSize.X, -halfSize.Y,  halfSize.Z), // 0
            new( halfSize.X, -halfSize.Y,  halfSize.Z), // 1
            new( halfSize.X,  halfSize.Y,  halfSize.Z), // 2
            new(-halfSize.X,  halfSize.Y,  halfSize.Z), // 3

            new(-halfSize.X, -halfSize.Y, -halfSize.Z), // 4
            new( halfSize.X, -halfSize.Y, -halfSize.Z), // 5
            new( halfSize.X,  halfSize.Y, -halfSize.Z), // 6
            new(-halfSize.X,  halfSize.Y, -halfSize.Z), // 7
        ];

        if (config.PrimitiveType == PrimitiveType.Lines)
        {
            float[] lineVertices = new float[positions.Length * 3];
            for (int i = 0; i < positions.Length; i++)
            {
                lineVertices[i * 3 + 0] = positions[i].X;
                lineVertices[i * 3 + 1] = positions[i].Y;
                lineVertices[i * 3 + 2] = positions[i].Z;
            }

            uint[] lineIndices =
            [
                0, 1, 1, 2, 2, 3, 3, 0,
                4, 5, 5, 6, 6, 7, 7, 4,
                0, 4, 1, 5, 2, 6, 3, 7,
            ];

            return new MeshPrimitive(lineVertices, lineIndices);
        }

        ReadOnlySpan<Vector3> normals =
        [
            new( 0,  0,  1), // Front
            new( 1,  0,  0), // Right
            new( 0,  0, -1), // Back
            new(-1,  0,  0), // Left
            new( 0,  1,  0), // Top
            new( 0, -1,  0), // Bottom
        ];

        ReadOnlySpan<Vector2> baseTexCoords =
        [
            new(0, 1), // Top Left
            new(1, 1), // Top Right
            new(1, 0), // Bottom Right
            new(0, 0), // Bottom Left
        ];

        ReadOnlySpan<int> faceVertexIndices =
        [
            0, 1, 2, 3, // Front
            1, 5, 6, 2, // Right
            5, 4, 7, 6, // Back
            4, 0, 3, 7, // Left
            3, 2, 6, 7, // Top
            4, 5, 1, 0, // Bottom
        ];

        ReadOnlySpan<uint> faceIndices = [0, 1, 2, 2, 3, 0];

        const int VERTICES_PER_FACE = 4;
        const int TRIANGLES_PER_FACE = 6;
        const int FACE_COUNT = 6;

        int vertexSize = 3;
        if (config.HasNormals)   vertexSize += 3;
        if (config.HasUV)        vertexSize += 2;
        if (config.HasNormalMap) vertexSize += 6;

        float[] vertices = new float[FACE_COUNT * VERTICES_PER_FACE * vertexSize];
        uint[] indices = new uint[FACE_COUNT * TRIANGLES_PER_FACE];

        Span<Vector2> texCoords = stackalloc Vector2[4];
        int vertexOffset = 0;
        int indexOffset = 0;

        for (int face = 0; face < FACE_COUNT; face++)
        {
            Vector3 faceNormal = normals[face];

            ref readonly var pos0 = ref positions[faceVertexIndices[face * 4 + 0]];
            ref readonly var pos1 = ref positions[faceVertexIndices[face * 4 + 1]];
            ref readonly var pos2 = ref positions[faceVertexIndices[face * 4 + 2]];
            ref readonly var pos3 = ref positions[faceVertexIndices[face * 4 + 3]];

            if (config.StretchTexture)
            {
                texCoords[0] = baseTexCoords[0];
                texCoords[1] = baseTexCoords[1];
                texCoords[2] = baseTexCoords[2];
                texCoords[3] = baseTexCoords[3];
            }
            else
            {
                Vector3 d1 = pos1 - pos0;
                Vector3 d2 = pos3 - pos0;
                float width = d1.Length();
                float height = d2.Length();
                texCoords[0] = new Vector2(0, height);
                texCoords[1] = new Vector2(width, height);
                texCoords[2] = new Vector2(width, 0);
                texCoords[3] = new Vector2(0, 0);
            }

            Vector3 edge1 = pos1 - pos0;
            Vector3 edge2 = pos2 - pos0;
            Vector2 deltaUV1 = texCoords[1] - texCoords[0];
            Vector2 deltaUV2 = texCoords[2] - texCoords[0];

            float f = 1.0f / (deltaUV1.X * deltaUV2.Y - deltaUV2.X * deltaUV1.Y);
            Vector3 tangent = f * (deltaUV2.Y * edge1 - deltaUV1.Y * edge2);
            Vector3 bitangent = f * (-deltaUV2.X * edge1 + deltaUV1.X * edge2);

            for (int i = 0; i < 4; i++)
            {
                var pos = positions[faceVertexIndices[face * 4 + i]];
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

            uint baseIndex = (uint)face * 4;
            for (int i = 0; i < 6; i++)
            {
                indices[indexOffset++] = baseIndex + faceIndices[i];
            }
        }

        return new MeshPrimitive(vertices, indices);
    }
}
