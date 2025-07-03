using System.Numerics;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace Engine.Graphics;

public class ShaderProgram : IDisposable
{
    private readonly uint _handle;
    private readonly GL _gl;

    public ShaderProgram(GL gl, string vertexShaderFilename, string fragmentShaderFilename)
    {
        _gl = gl;

        uint vertexShader = _gl.CreateShader(ShaderType.VertexShader);
        _gl.ShaderSource(vertexShader, LoadShaderSource(vertexShaderFilename));
        _gl.CompileShader(vertexShader);
        CheckCompileError(vertexShader, "VERTEX");

        uint fragmentShader = _gl.CreateShader(ShaderType.FragmentShader);
        _gl.ShaderSource(fragmentShader, LoadShaderSource(fragmentShaderFilename));
        _gl.CompileShader(fragmentShader);
        CheckCompileError(fragmentShader, "FRAGMENT");

        _handle = _gl.CreateProgram();

        _gl.AttachShader(_handle, vertexShader);
        _gl.AttachShader(_handle, fragmentShader);

        _gl.LinkProgram(_handle);
        CheckCompileError(_handle, "PROGRAM");

        _gl.DetachShader(_handle, vertexShader);
        _gl.DetachShader(_handle, fragmentShader);

        _gl.DeleteShader(vertexShader);
        _gl.DeleteShader(fragmentShader);
    }

    public void Use() => _gl.UseProgram(_handle);

    public void Dispose() => _gl.DeleteProgram(_handle);

    public uint GetAttribLocation(string name) => (uint)_gl.GetAttribLocation(_handle, name);

    public void SetBool(string name, bool value) =>
        _gl.Uniform1(_gl.GetUniformLocation(_handle, name), value ? 1 : 0);

    public void SetInt(string name, int value) =>
        _gl.Uniform1(_gl.GetUniformLocation(_handle, name), value);

    public void SetFloat(string name, float value) =>
        _gl.Uniform1(_gl.GetUniformLocation(_handle, name), value);

    public void SetVector2(string name, Vector2 value) =>
        _gl.Uniform2(_gl.GetUniformLocation(_handle, name), value);

    public void SetVector2(string name, float x, float y) =>
        _gl.Uniform2(_gl.GetUniformLocation(_handle, name), x, y);

    public void SetVector2(string name, float value) =>
        _gl.Uniform2(_gl.GetUniformLocation(_handle, name), value, value);

    public void SetVector3(string name, Vector3 value) =>
        _gl.Uniform3(_gl.GetUniformLocation(_handle, name), value);

    public void SetVector3(string name, float x, float y, float z) =>
        _gl.Uniform3(_gl.GetUniformLocation(_handle, name), x, y, z);

    public void SetVector3(string name, float value) =>
        _gl.Uniform3(_gl.GetUniformLocation(_handle, name), value, value, value);

    public unsafe void SetVector4(string name, Vector4 value) =>
        _gl.Uniform4(_gl.GetUniformLocation(_handle, name), value);

    public void SetVector4(string name, float x, float y, float z, float w) =>
        _gl.Uniform4(_gl.GetUniformLocation(_handle, name), x, y, z, w);

    public void SetVector4(string name, float value) =>
        _gl.Uniform4(_gl.GetUniformLocation(_handle, name), value, value, value, value);

    public unsafe void SetMatrix2(string name, Matrix2X2<float> matrix) =>
        _gl.UniformMatrix2(_gl.GetUniformLocation(_handle, name), 1, false, (float*)&matrix);

    public unsafe void SetMatrix3(string name, Matrix3X3<float> matrix) =>
        _gl.UniformMatrix3(_gl.GetUniformLocation(_handle, name), 1, false, (float*)&matrix);

    public unsafe void SetMatrix4(string name, Matrix4X4<float> matrix) =>
        _gl.UniformMatrix4(_gl.GetUniformLocation(_handle, name), 1, false, (float*)&matrix);

    public unsafe void SetMatrix4(string name, Matrix4x4 matrix) =>
        _gl.UniformMatrix4(_gl.GetUniformLocation(_handle, name), 1, false, (float*)&matrix);

    private static string LoadShaderSource(string filePath)
    {
        try
        {
            string fullPath = Path.Combine(AppContext.BaseDirectory, "Resources", "Shaders", filePath);
            using var sr = new StreamReader(fullPath);
            var shaderSource = sr.ReadToEnd();
            return shaderSource;
        }
        catch (FileNotFoundException ex)
        {
            Console.WriteLine($"Failed to load shader source file '{ex.FileName}'");
            return string.Empty;
        }
    }

    private void CheckCompileError(uint shader, string type)
    {
        string infoLog = _gl.GetShaderInfoLog(shader);
        if (!string.IsNullOrWhiteSpace(infoLog))
        {
            throw new Exception(
                $"Error compiling shader of type {type}, failed with error {infoLog}"
            );
        }
    }
}
