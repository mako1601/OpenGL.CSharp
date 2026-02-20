using System.Numerics;

namespace Engine.Geometry;

public static class Capsule
{
    public static MeshPrimitive Create(Vector3 size, MeshPrimitiveConfig? config = null)
    {
        if (size.X <= 0 || size.Y <= 0 || size.Z <= 0)
        {
            throw new ArgumentException($"Size must be positive and non-zero in all dimensions. Received: {size}");
        }

        config ??= new MeshPrimitiveConfig();

        float radiusX = size.X * 0.5f;
        float radiusZ = size.Z * 0.5f;
        float minRadius = MathF.Min(radiusX, radiusZ);
        float cylinderHeight = MathF.Max(0, size.Y - 2 * minRadius);
        float halfCylinderHeight = cylinderHeight * 0.5f;
        float averageRadius = 0.5f * (radiusX + radiusZ);
        float uWorldScale = 2f * MathF.PI * averageRadius;
        float uWrapScale = MathF.Max(1f, MathF.Round(uWorldScale));
        float hemisphereArcLength = 0.5f * MathF.PI * minRadius;
        float vWorldScale = cylinderHeight + 2f * hemisphereArcLength;

        int vertexSize = 3;
        if (config.HasNormals)   vertexSize += 3;
        if (config.HasUV)        vertexSize += 2;
        if (config.HasNormalMap) vertexSize += 6;

        int cylinderVertexCount = (config.Slices + 1) * (config.Stacks / 2 + 1);
        int hemisphereVertexCount = 2 * (config.Slices + 1) * (config.Stacks / 2 + 1);
        int totalVertexCount = cylinderVertexCount + hemisphereVertexCount;

        float[] vertices = new float[totalVertexCount * vertexSize];
        uint[] indices = new uint[config.Slices * (config.Stacks / 2) * 6 * 3];

        int vertexOffset = 0;
        int indexOffset = 0;

        GenerateCylinder(
            vertices,
            ref vertexOffset,
            indices,
            ref indexOffset,
            radiusX,
            radiusZ,
            halfCylinderHeight,
            config,
            hemisphereArcLength,
            cylinderHeight,
            vWorldScale,
            uWrapScale
        );

        uint vertexIndex = (uint)cylinderVertexCount;
        GenerateHemisphere(
            vertices,
            ref vertexOffset,
            indices,
            ref indexOffset,
            radiusX,
            radiusZ,
            minRadius,
            halfCylinderHeight,
            config,
            hemisphereArcLength,
            cylinderHeight,
            vWorldScale,
            uWrapScale,
            1f,
            ref vertexIndex
        );

        GenerateHemisphere(
            vertices,
            ref vertexOffset,
            indices,
            ref indexOffset,
            radiusX,
            radiusZ,
            minRadius,
            halfCylinderHeight,
            config,
            hemisphereArcLength,
            cylinderHeight,
            vWorldScale,
            uWrapScale,
            -1f,
            ref vertexIndex
        );

        return new MeshPrimitive(vertices, indices);
    }

    private static void GenerateCylinder(
        float[] vertices,
        ref int vertexOffset,
        uint[] indices,
        ref int indexOffset,
        float radiusX,
        float radiusZ,
        float halfCylinderHeight,
        MeshPrimitiveConfig confing,
        float hemisphereArcLength,
        float cylinderHeight,
        float vWorldScale,
        float uWorldScale
    )
    {
        for (int stack = 0; stack <= confing.Stacks / 2; stack++)
        {
            float stackProgress = (float)stack / (confing.Stacks / 2);
            float y = halfCylinderHeight - stackProgress * 2f * halfCylinderHeight;

            for (int slice = 0; slice <= confing.Slices; slice++)
            {
                float u = slice * (1f / confing.Slices);
                float angle = u * 2f * MathF.PI;
                float cos = MathF.Cos(angle);
                float sin = MathF.Sin(angle);
                vertices[vertexOffset++] = cos * radiusX;
                vertices[vertexOffset++] = y;
                vertices[vertexOffset++] = sin * radiusZ;

                if (confing.HasNormals)
                {
                    vertices[vertexOffset++] = cos;
                    vertices[vertexOffset++] = 0;
                    vertices[vertexOffset++] = sin;
                }

                if (confing.HasUV)
                {
                    float uvStackProgress = (float)stack / (confing.Stacks / 2);
                    float vNorm = (hemisphereArcLength + uvStackProgress * cylinderHeight) / vWorldScale;
                    vertices[vertexOffset++] = confing.StretchTexture ? u : u * uWorldScale;
                    vertices[vertexOffset++] = confing.StretchTexture ? vNorm : vNorm * vWorldScale;
                }

                if (confing.HasNormalMap)
                {
                    vertices[vertexOffset++] = -sin * radiusX;
                    vertices[vertexOffset++] = 0;
                    vertices[vertexOffset++] = cos * radiusZ;
                    vertices[vertexOffset++] = 0;
                    vertices[vertexOffset++] = 1;
                    vertices[vertexOffset++] = 0;
                }
            }
        }

        for (int stack = 0; stack < confing.Stacks / 2; stack++)
        {
            for (int slice = 0; slice < confing.Slices; slice++)
            {
                uint topLeft = (uint)(stack * (confing.Slices + 1) + slice);
                uint topRight = topLeft + 1;
                uint bottomLeft = (uint)((stack + 1) * (confing.Slices + 1) + slice);
                uint bottomRight = bottomLeft + 1;

                indices[indexOffset++] = topLeft;
                indices[indexOffset++] = topRight;
                indices[indexOffset++] = bottomLeft;

                indices[indexOffset++] = topRight;
                indices[indexOffset++] = bottomRight;
                indices[indexOffset++] = bottomLeft;
            }
        }
    }

    private static void GenerateHemisphere(
        float[] vertices,
        ref int vertexOffset,
        uint[] indices,
        ref int indexOffset,
        float radiusX,
        float radiusZ,
        float minRadius,
        float halfCylinderHeight,
        MeshPrimitiveConfig confing,
        float hemisphereArcLength,
        float cylinderHeight,
        float vWorldScale,
        float uWorldScale,
        float direction,
        ref uint vertexIndex
    )
    {
        for (int stack = 0; stack <=  confing.Stacks / 2; stack++)
        {
            float phi = 0.5f * MathF.PI * stack * (1f / (confing.Stacks / 2));
            float sinPhi = MathF.Sin(phi);
            float cosPhi = MathF.Cos(phi);
            float ySign = direction;
            float y = ySign * sinPhi * minRadius + ySign * halfCylinderHeight;

            for (int slice = 0; slice <= confing.Slices; slice++)
            {
                float theta = slice * (1f / confing.Slices) * 2f * MathF.PI;
                float cosTheta = MathF.Cos(theta);
                float sinTheta = MathF.Sin(theta);
                float x = cosPhi * cosTheta * radiusX;
                float z = cosPhi * sinTheta * radiusZ;
                vertices[vertexOffset++] = x;
                vertices[vertexOffset++] = y;
                vertices[vertexOffset++] = z;

                if (confing.HasNormals)
                {
                    vertices[vertexOffset++] = x / radiusX;
                    vertices[vertexOffset++] = (y - ySign * halfCylinderHeight) / minRadius;
                    vertices[vertexOffset++] = z / radiusZ;
                }

                if (confing.HasUV)
                {
                    float u = slice * (1f / confing.Slices);
                    float stackProgress = (float)stack / (confing.Stacks / 2);
                    float vNorm = direction > 0f
                        ? hemisphereArcLength * (1f - stackProgress) / vWorldScale
                        : (hemisphereArcLength + cylinderHeight + hemisphereArcLength * stackProgress) / vWorldScale;
                    vertices[vertexOffset++] = confing.StretchTexture ? u : u * uWorldScale;
                    vertices[vertexOffset++] = confing.StretchTexture ? vNorm : vNorm * vWorldScale;

                }

                if (confing.HasNormalMap)
                {
                    float tx = -sinTheta * radiusX;
                    float tz = cosTheta * radiusZ;
                    float nx = x / radiusX;
                    float ny = (y - ySign * halfCylinderHeight) / minRadius;
                    float nz = z / radiusZ;
                    float btx = ny * tz - nz * 0;
                    float bty = nz * tx - nx * tz;
                    float btz = nx * 0 - ny * tx;
                    float invLength = 1f / MathF.Sqrt(btx * btx + bty * bty + btz * btz);
                    btx *= invLength;
                    bty *= invLength;
                    btz *= invLength;
                    vertices[vertexOffset++] = tx;
                    vertices[vertexOffset++] = 0;
                    vertices[vertexOffset++] = tz;
                    vertices[vertexOffset++] = btx;
                    vertices[vertexOffset++] = bty;
                    vertices[vertexOffset++] = btz;
                }
            }
        }

        for (int stack = 0; stack < confing.Stacks / 2; stack++)
        {
            for (int slice = 0; slice < confing.Slices; slice++)
            {
                uint first = vertexIndex + (uint)(stack * (confing.Slices + 1) + slice);
                uint second = first + confing.Slices + 1;

                if (direction > 0)
                {
                    indices[indexOffset++] = first;
                    indices[indexOffset++] = second;
                    indices[indexOffset++] = first + 1;

                    indices[indexOffset++] = second;
                    indices[indexOffset++] = second + 1;
                    indices[indexOffset++] = first + 1;
                }
                else
                {
                    indices[indexOffset++] = first;
                    indices[indexOffset++] = first + 1;
                    indices[indexOffset++] = second;

                    indices[indexOffset++] = second;
                    indices[indexOffset++] = first + 1;
                    indices[indexOffset++] = second + 1;
                }
            }
        }

        vertexIndex += (uint)((confing.Stacks / 2 + 1) * (confing.Slices + 1));
    }
}
