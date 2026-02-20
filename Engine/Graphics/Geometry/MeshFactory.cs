using Engine.Graphics;
using Silk.NET.OpenGL;

namespace Engine.Geometry;

public static class MeshFactory
{
    public static Mesh CreateFullscreenQuad(GL gl)
    {
        return new Mesh(
            gl,
            new MeshPrimitive(
                [
                    -1f,  1f, 0f, 0f, 1f,
                    -1f, -1f, 0f, 0f, 0f,
                     1f, -1f, 0f, 1f, 0f,
                     1f,  1f, 0f, 1f, 1f
                ],
                [0u, 1u, 2u, 0u, 2u, 3u]
            ),
            new VertexAttributeDescription(0, 3, VertexAttribPointerType.Float, 5, 0),
            new VertexAttributeDescription(1, 2, VertexAttribPointerType.Float, 5, 3)
        );
    }
}
