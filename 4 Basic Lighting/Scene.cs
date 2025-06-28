﻿using System.Numerics;
using Engine.Geometry;
using Engine.Graphics;
using Silk.NET.OpenGL;

namespace BasicLighting;

public class Scene : IDisposable
{
    // Material properties
    public static float Shininess { get; set; } = 32f;
    public static float[] Ambient { get; set; } = [0.05f, 0.25f, 0.5f, 0.75f];
    public static float[] Diffuse { get; set; } = [0.2f, 0.4f, 0.6f, 0.8f];
    public static float[] Specular { get; set; } = [0.1f, 0.3f, 0.5f, 0.7f];

    // Buffers and VAO for rendering light source
    private readonly BufferObject<float> _lightvbo;
    private readonly BufferObject<uint> _lightebo;
    private readonly VertexArrayObject<float, uint> _lightvao;

    // Buffers and VAO for rendering the platform
    private readonly BufferObject<float> _vbo;
    private readonly BufferObject<uint> _ebo;
    private readonly VertexArrayObject<float, uint> _vao;

    // Shader program
    private readonly ShaderProgram _shader;

    // Textures used for the platform
    private readonly Engine.Graphics.Texture _diffuseTexture;
    private readonly Engine.Graphics.Texture _specularTexture;

    // Positions of the four point light sources
    private readonly Vector3[] _lightPosition =
    [
        new(0f, 1f, -4.125f),
        new(0f, 1f, -1.375f),
        new(0f, 1f,  1.375f),
        new(0f, 1f,  4.125f),
    ];

    // Sphere mesh
    private readonly MeshPrimitive _sphere =
        Sphere.Create(new Vector3(0.04f, 0.04f, 0.04f), 16, 16, false, false, false);

    // Platform mesh
    private readonly MeshPrimitive _plane =
        Engine.Geometry.Plane.Create(new Vector2(11.0f, 11.0f), normalMap: false, stretchTexture: false);

    public Scene(GL gl)
    {
        // OpenGL rendering state setup
        gl.Enable(EnableCap.CullFace);
        gl.CullFace(TriangleFace.Back);
        gl.FrontFace(FrontFaceDirection.Ccw);
        gl.Enable(EnableCap.DepthTest);
        gl.Enable(EnableCap.Blend);
        // gl.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
        gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        // --- Light sphere setup ---
        _lightebo = new BufferObject<uint>(
            gl,
            _sphere.Indices,
            BufferTargetARB.ElementArrayBuffer,
            BufferUsageARB.StaticDraw
        );
        _lightvbo = new BufferObject<float>(
            gl,
            _sphere.Vertices,
            BufferTargetARB.ArrayBuffer,
            BufferUsageARB.StaticDraw
        );
        _lightvao = new VertexArrayObject<float, uint>(gl, _lightvbo, _lightebo);
        _lightvao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 3, 0); // position
        _lightvao.Unbind();

        // --- Platform setup ---
        _ebo = new BufferObject<uint>(
            gl,
            _plane.Indices,
            BufferTargetARB.ElementArrayBuffer,
            BufferUsageARB.StaticDraw
        );
        _vbo = new BufferObject<float>(
            gl,
            _plane.Vertices,
            BufferTargetARB.ArrayBuffer,
            BufferUsageARB.StaticDraw
        );
        _vao = new VertexArrayObject<float, uint>(gl, _vbo, _ebo);
        _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 8, 0); // position
        _vao.VertexAttributePointer(1, 3, VertexAttribPointerType.Float, 8, 3); // normal
        _vao.VertexAttributePointer(2, 2, VertexAttribPointerType.Float, 8, 6); // texCoord

        // --- Shader and textures ---
        _shader = new ShaderProgram(gl, "main_shader.vert", "main_shader.frag");
        _diffuseTexture = new Engine.Graphics.Texture(gl, "brickwall.jpg", isPixelFiltering: false);
        _specularTexture = new Engine.Graphics.Texture(gl, "brickwall_specular.jpg", isPixelFiltering: false);
    }

    public unsafe void Draw(GL gl, Engine.Camera camera)
    {
        // Clear color and depth buffer
        gl.ClearColor(System.Drawing.Color.Black);
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _shader.Use();

        // View and projection matrices
        Matrix4x4 view = camera.GetViewMatrix();
        Matrix4x4 projection = camera.GetProjectionMatrix();
        _shader.SetMatrix4("uView", view);
        _shader.SetMatrix4("uProjection", projection);
        _shader.SetVector3("uViewPosition", camera.Position);

        // Material properties
        _shader.SetInt("uMaterial.diffuse", 0);
        _shader.SetInt("uMaterial.specular", 1);
        _shader.SetFloat("uMaterial.shininess", Shininess);

        // Upload each light source to shader
        for (int i = 0; i < _lightPosition.Length; i++)
        {
            _shader.SetVector3($"uLight[{i}].position", _lightPosition[i]);
            _shader.SetVector3($"uLight[{i}].ambient", Ambient[i]);
            _shader.SetVector3($"uLight[{i}].diffuse", Diffuse[i]);
            _shader.SetVector3($"uLight[{i}].specular", Specular[i]);
        }

        // --- Draw the platform ---
        _vao.Bind();
        _diffuseTexture.Bind(0);
        _specularTexture.Bind(1);
        _shader.SetMatrix4("uModel", Matrix4x4.Identity);
        _shader.SetBool("uUnlit", false);

        gl.DrawElements(
            PrimitiveType.Triangles,
            (uint)_plane.Indices.Length,
            DrawElementsType.UnsignedInt,
            null
        );
        _vao.Unbind();

        // --- Draw spheres at each light position ---
        _lightvao.Bind();

        for (int i = 0; i < _lightPosition.Length; i++)
        {
            var model = Matrix4x4.CreateTranslation(_lightPosition[i]);

            _shader.SetMatrix4("uModel", model);
            _shader.SetBool("uUnlit", true);
            _shader.SetVector3("uColor", Specular[i]);

            gl.DrawElements(
                PrimitiveType.Triangles,
                (uint)_sphere.Indices.Length,
                DrawElementsType.UnsignedInt,
                null
            );
        }

        _lightvao.Unbind();
    }

    public void Dispose()
    {
        _diffuseTexture?.Dispose();
        _specularTexture?.Dispose();

        _lightvbo?.Dispose();
        _lightebo?.Dispose();
        _lightvao?.Dispose();

        _vbo?.Dispose();
        _ebo?.Dispose();
        _vao?.Dispose();

        _shader?.Dispose();
    }
}
