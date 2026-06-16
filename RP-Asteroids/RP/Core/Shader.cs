using OpenTK.Graphics.OpenGL;

namespace RP.Core
    {
    internal abstract class Shader
    {
        private int _id = 0;
        public int Id => _id;

        protected abstract ShaderType Type { get; }


        protected Shader(string source)
        {
            _id = GL.CreateShader(Type);

            GL.ShaderSource(_id, source);
            GL.CompileShader(_id);
            string log = GL.GetShaderInfoLog(_id);
            if(log != null && log != "")
            {
                Console.WriteLine($"Erro de compilação de shader({Type.ToString()}): {log}");
            }
        }

        public void Delete()
        {
            if (_id != 0)
            {
                GL.DeleteShader(_id);
            }
        }
    }

    internal class VertexShader : Shader
    {
        protected override ShaderType Type => ShaderType.VertexShader;

        public VertexShader(string source) : base(source) {}

        public static VertexShader LoadFromFile(string filePath)
        {
            return new VertexShader(File.ReadAllText(filePath));
        }
    }

    internal class FragmentShader : Shader
    {
        protected override ShaderType Type => ShaderType.FragmentShader;

        public FragmentShader(string source) : base(source) {}

        public static FragmentShader LoadFromFile(string filePath)
        {
            return new FragmentShader(File.ReadAllText(filePath));
        }
    }
}
