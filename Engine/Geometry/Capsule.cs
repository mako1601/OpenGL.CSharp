using System.Numerics;

namespace Engine.Geometry;

public static class Capsule
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

        float radiusX = size.X * 0.5f;
        float radiusZ = size.Z * 0.5f;
        float minRadius = MathF.Min(radiusX, radiusZ);
        float cylinderHeight = MathF.Max(0, size.Y - 2 * minRadius);
        float halfCylinderHeight = cylinderHeight * 0.5f;

        int vertexSize = 3;
        if (normal)     vertexSize += 3;
        if (uv)         vertexSize += 2;
        if (normalMap)  vertexSize += 6;

        int cylinderVertexCount = (slices + 1) * (stacks / 2 + 1);
        int hemisphereVertexCount = 2 * (slices + 1) * (stacks / 2 + 1);
        int totalVertexCount = cylinderVertexCount + hemisphereVertexCount;

        float[] vertices = new float[totalVertexCount * vertexSize];
        uint[] indices = new uint[slices * (stacks / 2) * 6 * 3];

        float hemisphereHeight = minRadius;
        float totalHeight = 2 * hemisphereHeight + cylinderHeight;
        float vHemisphere = hemisphereHeight / totalHeight;
        float vCylinder = cylinderHeight / totalHeight;

        int vertOffset = 0;
        int indexOffset = 0;

        GenerateCylinder(
            vertices,
            ref vertOffset,
            indices,
            ref indexOffset,
            radiusX,
            radiusZ,
            halfCylinderHeight,
            slices,
            stacks / 2,
            normal,
            uv,
            normalMap,
            stretchTexture,
            vHemisphere,
            vCylinder,
            size
        );

        uint vertexIndex = (uint)cylinderVertexCount;
        GenerateHemisphere(
            vertices,
            ref vertOffset,
            indices,
            ref indexOffset,
            radiusX,
            radiusZ,
            minRadius,
            halfCylinderHeight,
            slices,
            stacks / 2,
            normal,
            uv,
            normalMap,
            stretchTexture,
            vHemisphere,
            vCylinder,
            size,
            1f,
            ref vertexIndex
        );

        GenerateHemisphere(
            vertices,
            ref vertOffset,
            indices,
            ref indexOffset,
            radiusX,
            radiusZ,
            minRadius,
            halfCylinderHeight,
            slices,
            stacks / 2,
            normal,
            uv,
            normalMap,
            stretchTexture,
            vHemisphere,
            vCylinder,
            size,
            -1f,
            ref vertexIndex
        );

        return new MeshPrimitive(vertices, indices);
    }

    private static void GenerateCylinder(
        float[]     vertices,
        ref int     vertOffset,
        uint[]      indices,
        ref int     indexOffset,
        float       radiusX,
        float       radiusZ,
        float       halfCylinderHeight,
        uint        slices,
        int         halfStacks,
        bool        normal,
        bool        uv,
        bool        normalMap,
        bool        stretchTexture,
        float       vHemisphere,
        float       vCylinder,
        Vector3     size
    )
    {
        for (int stack = 0; stack <= halfStacks; stack++)
        {
            float stackProgress = (float)stack / halfStacks;
            float y = halfCylinderHeight - stackProgress * 2f * halfCylinderHeight;

            for (int slice = 0; slice <= slices; slice++)
            {
                float u = slice * (1f / slices);
                float angle = u * 2f * MathF.PI;
                float cos = MathF.Cos(angle);
                float sin = MathF.Sin(angle);
                vertices[vertOffset++] = cos * radiusX;
                vertices[vertOffset++] = y;
                vertices[vertOffset++] = sin * radiusZ;

                if (normal)
                {
                    vertices[vertOffset++] = cos;
                    vertices[vertOffset++] = 0;
                    vertices[vertOffset++] = sin;
                }

                if (uv)
                {
                    float v = vHemisphere + vCylinder + vHemisphere * stack / halfStacks;
                    vertices[vertOffset++] = stretchTexture ? u : u * size.X;
                    vertices[vertOffset++] = stretchTexture ? v : v * size.Y;
                }

                if (normalMap)
                {
                    vertices[vertOffset++] = -sin * radiusX;
                    vertices[vertOffset++] = 0;
                    vertices[vertOffset++] = cos * radiusZ;
                    vertices[vertOffset++] = 0;
                    vertices[vertOffset++] = 1;
                    vertices[vertOffset++] = 0;
                }
            }
        }

        for (int stack = 0; stack < halfStacks; stack++)
        {
            for (int slice = 0; slice < slices; slice++)
            {
                uint topLeft = (uint)(stack * (slices + 1) + slice);
                uint topRight = topLeft + 1;
                uint bottomLeft = (uint)((stack + 1) * (slices + 1) + slice);
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
        float[]     vertices,
        ref int     vertOffset,
        uint[]      indices,
        ref int     indexOffset,
        float       radiusX,
        float       radiusZ,
        float       minRadius,
        float       halfCylinderHeight,
        uint        slices,
        int         halfStacks,
        bool        normal,
        bool        uv,
        bool        normalMap,
        bool        stretchTexture,
        float       vHemisphere,
        float       vCylinder,
        Vector3     size,
        float       direction,
        ref uint    vertexIndex
    )
    {
        for (int stack = 0; stack <= halfStacks; stack++)
        {
            float phi = 0.5f * MathF.PI * stack * (1f / halfStacks);
            float sinPhi = MathF.Sin(phi);
            float cosPhi = MathF.Cos(phi);
            float ySign = direction;
            float y = ySign * sinPhi * minRadius + ySign * halfCylinderHeight;

            for (int slice = 0; slice <= slices; slice++)
            {
                float theta = slice * (1f / slices) * 2f * MathF.PI;
                float cosTheta = MathF.Cos(theta);
                float sinTheta = MathF.Sin(theta);
                float x = cosPhi * cosTheta * radiusX;
                float z = cosPhi * sinTheta * radiusZ;
                vertices[vertOffset++] = x;
                vertices[vertOffset++] = y;
                vertices[vertOffset++] = z;

                if (normal)
                {
                    vertices[vertOffset++] = x / radiusX;
                    vertices[vertOffset++] = (y - ySign * halfCylinderHeight) / minRadius;
                    vertices[vertOffset++] = z / radiusZ;
                }

                if (uv)
                {
                    float u = slice * (1f / slices);
                    float v = direction > 0
                        ? vHemisphere + vCylinder - vHemisphere * stack * (1f / halfStacks)
                        : vHemisphere + vCylinder + vHemisphere * stack * (1f / halfStacks);
                    vertices[vertOffset++] = stretchTexture ? u : u * size.X;
                    vertices[vertOffset++] = stretchTexture ? v : v * size.Y;

                }

                if (normalMap)
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
                    vertices[vertOffset++] = tx;
                    vertices[vertOffset++] = 0;
                    vertices[vertOffset++] = tz;
                    vertices[vertOffset++] = btx;
                    vertices[vertOffset++] = bty;
                    vertices[vertOffset++] = btz;
                }
            }
        }

        for (int stack = 0; stack < halfStacks; stack++)
        {
            for (int slice = 0; slice < slices; slice++)
            {
                uint first = vertexIndex + (uint)(stack * (slices + 1) + slice);
                uint second = first + slices + 1;

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

        vertexIndex += (uint)((halfStacks + 1) * (slices + 1));
    }
}