using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace RP.Core
{
    //  Classe de material. Materiais são uma coleção de parâmetros de variáveis uniforme que deixam mais prático o processo de
    //desenho dos objetos. Com materiais, não precisamos mais atribuir os valores de cada uma das variáveis individualmente.
    internal class Material
    {
        private struct UniformCollection
        {
            private Dictionary<string, int> _intUniforms = new();
            private Dictionary<string, float> _floatUniforms = new();
            private Dictionary<string, Vector2> _vec2Uniforms = new();
            private Dictionary<string, Vector3> _vec3Uniforms = new();
            private Dictionary<string, Vector4> _vec4Uniforms = new();
            private Dictionary<string, Matrix4> _mat4Uniforms = new();
            private Dictionary<string, Texture> _textureUniforms = new();

            public UniformCollection() {}

            public void SetInt(string name, int value)
            {
                _intUniforms[name] = value;
            }

            public void SetFloat(string name, float value)
            {
                _floatUniforms[name] = value;
            }

            public void SetVec2(string name, Vector2 value)
            {
                _vec2Uniforms[name] = value;
            }

            public void SetVec3(string name, Vector3 value)
            {
                _vec3Uniforms[name] = value;
            }

            public void SetVec4(string name, Vector4 value)
            {
                _vec4Uniforms[name] = value;
            }

            public void SetMat4(string name, Matrix4 value)
            {
                _mat4Uniforms[name] = value;
            }

            public void SetTexture(string name, Texture value)
            {
                _textureUniforms[name] = value;
            }

            public void Unset(string name)
            {
                _intUniforms.Remove(name);
                _floatUniforms.Remove(name);
                _vec2Uniforms.Remove(name);
                _vec3Uniforms.Remove(name);
                _vec4Uniforms.Remove(name);
                _mat4Uniforms.Remove(name);
                _textureUniforms.Remove(name);
            }

            public void UnsetAll()
            {
                _intUniforms.Clear();
                _floatUniforms.Clear();
                _vec2Uniforms.Clear();
                _vec3Uniforms.Clear();
                _vec4Uniforms.Clear();
                _mat4Uniforms.Clear();
                _textureUniforms.Clear();
            }

            public void Apply(ShaderProgram program, ref int textureIndex)
            {
                foreach (var uniform in _intUniforms)
                {
                    program.SetUniform(uniform.Key, uniform.Value);
                }
                foreach (var uniform in _floatUniforms)
                {
                    program.SetUniform(uniform.Key, uniform.Value);
                }
                foreach (var uniform in _vec2Uniforms)
                {
                    program.SetUniform(uniform.Key, uniform.Value);
                }
                foreach (var uniform in _vec3Uniforms)
                {
                    program.SetUniform(uniform.Key, uniform.Value);
                }
                foreach (var uniform in _vec4Uniforms)
                {
                    program.SetUniform(uniform.Key, uniform.Value);
                }
                foreach (var uniform in _mat4Uniforms)
                {
                    program.SetUniform(uniform.Key, uniform.Value);
                }

                //  Texturas precisam ser tratadas de forma especial, pois não as passamos elas diretamente para a placa de vídeo
                //por questão de performance. Aqui, colocamos cada uma das texturas em uma unit, uma "caixinha", e falamos para o
                //shader de qual "caixinha" ele deve pegar a textura.
                foreach (var uniform in _textureUniforms)
                {
                    uniform.Value.Use(textureIndex);
                    program.SetUniform(uniform.Key, textureIndex);
                    textureIndex++;
                }
            }
        }

        private static UniformCollection _globalUniforms = new();

        private UniformCollection _uniforms = new();
        private ShaderProgram _program;
        public ShaderProgram Program => _program;
        public TriangleFace cullMode = TriangleFace.Back;
        public bool cull = true;
        public bool depthWrite = true;

        public Material(ShaderProgram program)
        {
            _program = program;
        }

        public void Use()
        {
            _program.Use();
            int textureIndex = 0;

            _globalUniforms.Apply(_program, ref textureIndex);
            _uniforms.Apply(_program, ref textureIndex);

            if (cull)
            {
            GL.Enable(EnableCap.CullFace);
            }
            else
            {
                GL.Disable(EnableCap.CullFace);
            }
            GL.CullFace(cullMode);
            GL.DepthMask(depthWrite);
        }

        public void SetInt(string name, int value)
        {
            _uniforms.SetInt(name, value);
        }

        public void SetFloat(string name, float value)
        {
            _uniforms.SetFloat(name, value);
        }

        public void SetVec2(string name, Vector2 value)
        {
            _uniforms.SetVec2(name, value);
        }

        public void SetVec2(string name, float x, float y)
        {
            SetVec2(name, new Vector2(x, y));
        }

        public void SetVec3(string name, Vector3 value)
        {
            _uniforms.SetVec3(name, value);
        }

        public void SetVec3(string name, float x, float y, float z)
        {
            SetVec3(name, new Vector3(x, y, z));
        }

        public void SetVec4(string name, Vector4 value)
        {
            _uniforms.SetVec4(name, value);
        }

        public void SetVec4(string name, float x, float y, float z, float w)
        {
            SetVec4(name, new Vector4(x, y, z, w));
        }

        public void SetTexture(string name, Texture value)
        {
            _uniforms.SetTexture(name, value);
        }

        public void Unset(string name)
        {
            _uniforms.Unset(name);
        }


        public static void SetGlobalInt(string name, int value)
        {
            _globalUniforms.SetInt(name, value);
        }

        public static void SetGlobalFloat(string name, float value)
        {
            _globalUniforms.SetFloat(name, value);
        }

        public static void SetGlobalVec2(string name, Vector2 value)
        {
            _globalUniforms.SetVec2(name, value);
        }

        public static void SetGlobalVec2(string name, float x, float y)
        {
            SetGlobalVec2(name, new Vector2(x, y));
        }

        public static void SetGlobalVec3(string name, Vector3 value)
        {
            _globalUniforms.SetVec3(name, value);
        }

        public static void SetGlobalVec3(string name, float x, float y, float z)
        {
            SetGlobalVec3(name, new Vector3(x, y, z));
        }

        public static void SetGlobalVec4(string name, Vector4 value)
        {
            _globalUniforms.SetVec4(name, value);
        }

        public static void SetGlobalVec4(string name, float x, float y, float z, float w)
        {
            SetGlobalVec4(name, new Vector4(x, y, z, w));
        }

        public static void SetGlobalMat4(string name, Matrix4 value)
        {
            _globalUniforms.SetMat4(name, value);
        }

        public static void SetGlobalTexture(string name, Texture value)
        {
            _globalUniforms.SetTexture(name, value);
        }

        public static void UnsetGlobal(string name)
        {
            _globalUniforms.Unset(name);
        }
    }
}
