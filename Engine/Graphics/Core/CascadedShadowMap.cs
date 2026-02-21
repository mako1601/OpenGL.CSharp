using System.Numerics;
using Silk.NET.OpenGL;

namespace Engine.Graphics;

public sealed class CascadedShadowMap : IDisposable
{
    private readonly GL _gl;
    private readonly FrameBuffer _frameBuffer;
    private readonly DepthTextureArray _depthTextureArray;
    private readonly Matrix4x4[] _lightSpaceMatrices;
    private readonly float[] _cascadeSplits;

    public int CascadeCount { get; }
    public uint ShadowSize { get; }

    public IReadOnlyList<Matrix4x4> LightSpaceMatrices => _lightSpaceMatrices;
    public IReadOnlyList<float> CascadeSplits => _cascadeSplits;
    public DepthTextureArray DepthTextureArray => _depthTextureArray;

    public CascadedShadowMap(GL gl, uint shadowSize, int cascadeCount)
    {
        if (cascadeCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cascadeCount), "Cascade count must be > 0.");
        }

        _gl = gl;
        CascadeCount = cascadeCount;
        ShadowSize = shadowSize;

        _frameBuffer = new FrameBuffer(gl);
        _depthTextureArray = new DepthTextureArray(gl, shadowSize, shadowSize, cascadeCount);
        _lightSpaceMatrices = new Matrix4x4[cascadeCount];
        _cascadeSplits = new float[cascadeCount];
    }

    public void BindCascadeForWriting(int cascadeIndex)
    {
        ValidateCascadeIndex(cascadeIndex);

        _gl.Viewport(0, 0, ShadowSize, ShadowSize);
        _frameBuffer.Bind();
        _frameBuffer.AttachTextureLayer(
            FramebufferAttachment.DepthAttachment,
            _depthTextureArray.Handle,
            cascadeIndex
        );
        _frameBuffer.SetDrawReadBuffers(DrawBufferMode.None, ReadBufferMode.None);
        _frameBuffer.Validate();
    }

    public void SetCascadeData(int cascadeIndex, Matrix4x4 lightSpaceMatrix, float splitDistance)
    {
        ValidateCascadeIndex(cascadeIndex);
        _lightSpaceMatrices[cascadeIndex] = lightSpaceMatrix;
        _cascadeSplits[cascadeIndex] = splitDistance;
    }

    public void BindForReading(uint textureSlot) => _depthTextureArray.Bind(textureSlot);

    public void ClearDepth() => _gl.Clear(ClearBufferMask.DepthBufferBit);

    public void Unbind() => FrameBuffer.Unbind(_gl);

    public void Dispose()
    {
        _depthTextureArray.Dispose();
        _frameBuffer.Dispose();
        GC.SuppressFinalize(this);
    }

    private void ValidateCascadeIndex(int cascadeIndex)
    {
        if (cascadeIndex < 0 || cascadeIndex >= CascadeCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cascadeIndex),
                $"Cascade index must be in range [0..{CascadeCount - 1}]."
            );
        }
    }
}
