namespace Engine.Geometry;

public sealed class MeshPrimitive
{
    private readonly float[] _vertices;
    private readonly uint[] _indices;

    public Span<float> Vertices => _vertices;
    public Span<uint> Indices => _indices;

    public MeshPrimitive(MeshPrimitive meshPrimitive)
    {
        _vertices = [.. meshPrimitive._vertices];
        _indices = [.. meshPrimitive._indices];
    }

    public MeshPrimitive(float[] vertices, uint[] indices)
    {
        _vertices = vertices ?? throw new ArgumentNullException(nameof(vertices));
        _indices = indices ?? throw new ArgumentNullException(nameof(indices));
    }
}
