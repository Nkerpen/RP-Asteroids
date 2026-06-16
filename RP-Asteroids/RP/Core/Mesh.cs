using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace RP.Core
{
    internal class Mesh
    {
        private int _vbo = 0;// vertex buffer object
        private int _ebo = 0;// element buffer object
        private int _vao = 0;// vertex array object
        private int _count = 0;// contagem de vértices
        private Dictionary<int, int> _ibos = new();// instanced buffer objects

        public Mesh(float[] vertices, uint[] indices)
        {
            _vbo = GL.GenBuffer();// Vertex Buffer Object
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * vertices.Length, vertices, BufferUsageHint.StaticDraw);

            //Os dados foram enviados de forma bruta, sem especificação alguma. Por isso, utilizamos um
            //Vertex Array Object, para especificar como os dados devem se comportar. No nosso caso, os
            //dados são vetores com 2 elementos de float.
            _vao = GL.GenVertexArray();// Vertex Array Object
            GL.BindVertexArray(_vao);
            // 0 - Atributo de posição: 3 floats
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, sizeof(float) * 8, 0);
            // 1 - Atributo de cor: 3 floats(rgb)
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, sizeof(float) * 8, sizeof(float) * 3);
            // 2 - Atributo de UV de textura: 2 floats
            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, sizeof(float) * 8, sizeof(float) * 6);

            // Para manipular as vértices de forma mais direta, podemos especificar os triângulos que vão
            //ser desenhados usando um Element Buffer Object, que vai conter os índices dos vértices que
            //formam cada triângulo. O processo de envio de um Element Buffer para a placa é muito similar
            //ao do Vertex Buffer Object, que enviamos anteriormente.
            _ebo = GL.GenBuffer();// Element Buffer Object
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, sizeof(uint) * indices.Length, indices, BufferUsageHint.StaticDraw);
            _count = indices.Length;
        }

        public void Draw(int instances = 1)
        {
            GL.BindVertexArray(_vao);
            GL.DrawElementsInstanced(PrimitiveType.Triangles, _count, DrawElementsType.UnsignedInt, 0, instances);
        }

        public void SetInstancePointer(int index, int size, int stride, int offset = 0, VertexAttribPointerType type = VertexAttribPointerType.Float)
        {
            if (!_ibos.ContainsKey(index))
            {
                return;
            }
            int ibo = _ibos[index];

            int typeSize = 1;
            switch (type)
            {
                case VertexAttribPointerType.Float:
                    typeSize = sizeof(float);
                    break;
                case VertexAttribPointerType.Int:
                    typeSize = sizeof(int);
                    break;
            }

            int prev = GL.GetInteger(GetPName.VertexArrayBinding);
            GL.BindVertexArray(_vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, ibo);
            switch (size)
            {
                case 1:
                case 2:
                case 3:
                case 4:
                    GL.EnableVertexAttribArray(index);
                    GL.VertexAttribPointer(index, size, type, false, stride, offset);
                    GL.VertexAttribDivisor(index, 1);
                    break;
                case 9://matriz 3x3
                    for (int i = 0; i < 3; i++)
                    {
                        GL.EnableVertexAttribArray(index + i);
                        GL.VertexAttribPointer(index + i, 3, type, false, stride, offset + typeSize * 3 * i);
                        GL.VertexAttribDivisor(index + i, 1);
                    }
                    break;
                case 16:// matriz 4x4
                    for (int i = 0; i < 4; i++)
                    {
                        GL.EnableVertexAttribArray(index + i);
                        GL.VertexAttribPointer(index + i, 4, type, false, stride, offset + typeSize * 4 * i);
                        GL.VertexAttribDivisor(index + i, 1);
                    }
                    break;
            }
            GL.BindVertexArray(prev);
        }

        public void SetInstanceData(int index, int[] data)
        {
            if (!_ibos.ContainsKey(index))
            {
                _ibos.Add(index, GL.GenBuffer());
            }
            int ibo = _ibos[index];

            GL.BindBuffer(BufferTarget.ArrayBuffer, ibo);
            GL.BufferData(BufferTarget.ArrayBuffer, data.Length * sizeof(int), data, BufferUsageHint.StaticDraw);

            SetInstancePointer(index, 1, sizeof(int), 0);
        }

        public void SetInstanceData(int index, float[] data)
        {
            if (!_ibos.ContainsKey(index))
            {
                _ibos.Add(index, GL.GenBuffer());
            }
            int ibo = _ibos[index];

            GL.BindBuffer(BufferTarget.ArrayBuffer, ibo);
            GL.BufferData(BufferTarget.ArrayBuffer, data.Length * sizeof(float), data, BufferUsageHint.StaticDraw);

            SetInstancePointer(index, 1, sizeof(float), 0);
        }

        public void SetInstanceData(int index, Vector2[] data)
        {
            float[] data2 = new float[2 * data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                data2[i * 2 + 0] = data[i].X;
                data2[i * 2 + 1] = data[i].Y;
            }
            SetInstanceData(index, data2);
            SetInstancePointer(index, 2, sizeof(float) * 2, 0);
        }

        public void SetInstanceData(int index, Vector3[] data)
        {
            float[] data2 = new float[3 * data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                data2[i * 3 + 0] = data[i].X;
                data2[i * 3 + 1] = data[i].Y;
                data2[i * 3 + 2] = data[i].Z;
            }
            SetInstanceData(index, data2);
            SetInstancePointer(index, 3, sizeof(float) * 3, 0);
        }

        public void SetInstanceData(int index, Vector4[] data)
        {
            float[] data2 = new float[4 * data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                data2[i * 4 + 0] = data[i].X;
                data2[i * 4 + 1] = data[i].Y;
                data2[i * 4 + 2] = data[i].Z;
                data2[i * 4 + 3] = data[i].W;
            }
            SetInstanceData(index, data2);
            SetInstancePointer(index, 4, sizeof(float) * 4, 0);
        }

        public void SetInstanceData(int index, Matrix3[] data)
        {
            Vector3[] data2 = new Vector3[3 * data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                data2[i * 3 + 0] = data[i].Column0;
                data2[i * 3 + 1] = data[i].Column1;
                data2[i * 3 + 2] = data[i].Column2;
            }
            SetInstanceData(index, data2);
            SetInstancePointer(index, 9, sizeof(float) * 9, 0);
        }

        public void SetInstanceData(int index, Matrix4[] data)
        {
            Vector4[] data2 = new Vector4[4 * data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                data2[i * 4 + 0] = data[i].Column0;
                data2[i * 4 + 1] = data[i].Column1;
                data2[i * 4 + 2] = data[i].Column2;
                data2[i * 4 + 3] = data[i].Column3;
            }
            SetInstanceData(index, data2);
            SetInstancePointer(index, 16, sizeof(float) * 16, 0);
        }

        public void Delete()
        {
            GL.DeleteVertexArray(_vao);
            GL.DeleteBuffer(_ebo);
            GL.DeleteBuffer(_vbo);
            foreach (KeyValuePair<int, int> kvp in _ibos)
            {
                GL.DeleteBuffer(kvp.Value);
            }
        }

        public static Mesh[] LoadFromFile(string path, float scale = 1f)
        {
            Assimp.AssimpContext importer = new();
            importer.Scale = scale;
            try
            {
                Assimp.PostProcessSteps flags =
                    Assimp.PostProcessSteps.Triangulate |
                    Assimp.PostProcessSteps.GenerateNormals |
                    Assimp.PostProcessSteps.PreTransformVertices
                ;
                Assimp.Scene scene = importer.ImportFile(path, flags);

                Mesh ProcessMesh(Assimp.Mesh m)
                {
                    if (!m.HasNormals || !m.HasVertices || !m.HasTextureCoords(0))
                    {
                        return Primitive.CreateCube(1f);
                    }

                    List<float> vertices = new();
                    for (int i = 0; i < m.VertexCount; i++)
                    {
                        var v = m.Vertices[i];
                        var n = m.Normals[i];
                        var t = m.TextureCoordinateChannels[0][i];
                        vertices.AddRange([v.X, v.Y, v.Z, n.X, n.Y, n.Z, t.X, t.Y]);
                    }

                    uint[] indices = m.GetUnsignedIndices().ToArray();

                    return new Mesh(vertices.ToArray(), indices);
                }

                Mesh[] ProcessNode(Assimp.Scene s, Assimp.Node n)
                {
                    List<Mesh> meshes = new();
                    foreach (int i in n.MeshIndices)
                    {
                        meshes.Add(ProcessMesh(s.Meshes[i]));
                    }
                    foreach (Assimp.Node other in s.RootNode.Children)
                    {
                        meshes.AddRange(ProcessNode(s, other));
                    }
                    return meshes.ToArray();
                }

                return ProcessNode(scene, scene.RootNode);
            }
            catch
            {
                return [];
            }
        }
    }

    internal class Primitive
    {
        // Plano que cobre toda a tela, para efeitos de pós processamento
        public static Mesh CreatePost()
        {
            float[] vertices =
            [   // posição     //cor/normal //uv
                -1f, -1f, 0f,  0f, 0f, 1f,  0f, 0f,
                 1f, -1f, 0f,  0f, 0f, 1f,  1f, 0f,
                 1f,  1f, 0f,  0f, 0f, 1f,  1f, 1f,
                -1f,  1f, 0f,  0f, 0f, 1f,  0f, 1f,
            ];

            uint[] indices =
            [
                0, 1, 2,
                0, 2, 3,
            ];

            return new Mesh(vertices, indices);
        }

        // Criação de plano, com valores para largura e comprimento
        public static Mesh CreatePlane(float width, float length)
        {
            float hw = width / 2f;
            float hl = length / 2f;

            float[] vertices =
            [   // posição     //cor/normal //uv
                -hw, 0f,  hl,  0f, 1f, 0f,  0f, 0f,
                 hw, 0f,  hl,  0f, 1f, 0f,  1f, 0f,
                 hw, 0f, -hl,  0f, 1f, 0f,  1f, 1f,
                -hw, 0f, -hl,  0f, 1f, 0f,  0f, 1f,
            ];

            uint[] indices =
            [
                0, 1, 2,
                0, 2, 3,
            ];

            return new Mesh(vertices, indices);
        }

        // Criação de plano, mas com dimensões iguais para largura e comprimento
        public static Mesh CreatePlane(float size = 1f)
        {
            return CreatePlane(size, size);
        }

        // Criação de prisma retangular, uma espécie de "cubo", mas com valores distintos para largura, altura e comprimento
        public static Mesh CreateRectangularPrism(float width, float height, float length)
        {
            float hw = width / 2f;
            float hh = height / 2f;
            float hl = length / 2f;

            float[] vertices =
            [   // posição    //cor/normal    //uv
                -hw,  hh,  hl,   0f,  1f,  0f,   0f, 0f,//0
                 hw,  hh,  hl,   0f,  1f,  0f,   1f, 0f,
                 hw,  hh, -hl,   0f,  1f,  0f,   1f, 1f,
                -hw,  hh, -hl,   0f,  1f,  0f,   0f, 1f,

                -hw, -hh, -hl,   0f, -1f,  0f,   0f, 0f,//4
                 hw, -hh, -hl,   0f, -1f,  0f,   1f, 0f,
                 hw, -hh,  hl,   0f, -1f,  0f,   1f, 1f,
                -hw, -hh,  hl,   0f, -1f,  0f,   0f, 1f,

                 hw, -hh,  hl,   1f,  0f,  0f,   0f, 0f,//8
                 hw, -hh, -hl,   1f,  0f,  0f,   1f, 0f,
                 hw,  hh, -hl,   1f,  0f,  0f,   1f, 1f,
                 hw,  hh,  hl,   1f,  0f,  0f,   0f, 1f,

                -hw, -hh, -hl,  -1f,  0f,  0f,   0f, 0f,//12
                -hw, -hh,  hl,  -1f,  0f,  0f,   1f, 0f,
                -hw,  hh,  hl,  -1f,  0f,  0f,   1f, 1f,
                -hw,  hh, -hl,  -1f,  0f,  0f,   0f, 1f,

                -hw, -hh,  hl,   0f,  0f,  1f,   0f, 0f,//16
                 hw, -hh,  hl,   0f,  0f,  1f,   1f, 0f,
                 hw,  hh,  hl,   0f,  0f,  1f,   1f, 1f,
                -hw,  hh,  hl,   0f,  0f,  1f,   0f, 1f,

                 hw, -hh, -hl,   0f,  0f, -1f,   0f, 0f,//20
                -hw, -hh, -hl,   0f,  0f, -1f,   1f, 0f,
                -hw,  hh, -hl,   0f,  0f, -1f,   1f, 1f,
                 hw,  hh, -hl,   0f,  0f, -1f,   0f, 1f,
            ];

            uint[] indices =
            [
                0, 1, 2,
                0, 2, 3,

                4, 5, 6,
                4, 6, 7,

                8, 9, 10,
                8, 10, 11,

                12, 13, 14,
                12, 14, 15,

                16, 17, 18,
                16, 18, 19,

                20, 21, 22,
                20, 22, 23,
            ];

            return new Mesh(vertices, indices);
        }

        // Criação de cubo, com valor uniforme de tamanho.
        // Notem a utilização da função de criação de prisma retangular, aproveitando parte do código
        public static Mesh CreateCube(float size = 1f)
        {
            return CreateRectangularPrism(size, size, size);
        }

        // Cilindro, com valores para raio e altura, assim como parametrização da "qualidade" pelo número de segmentos
        public static Mesh CreateCylinder(float radius = 0.5f, float height = 1.0f, int segments = 16)
        {
            float halfHeight = height / 2.0f;

            List<float> vertices = new();
            for (int i = 0; i <= segments; i++)
            {
                float value = (float)i / segments;
                float angle = value * MathF.Tau;
                float cos = MathF.Cos(angle);
                float sin = MathF.Sin(angle);
                float x = cos * radius;
                float z = -sin * radius;

                // 0 - Face lateral (topo)
                vertices.AddRange(new float[] { x, halfHeight, z });// Posição
                vertices.AddRange(new float[] { cos, 0.0f, -sin });// Normal
                vertices.AddRange(new float[] { value, 1.0f });// UV

                // 1 - Face lateral (baixo)
                vertices.AddRange(new float[] { x, -halfHeight, z });// Posição
                vertices.AddRange(new float[] { cos, 0.0f, -sin });// Normal
                vertices.AddRange(new float[] { value, 0.0f });// UV

                float uvX = cos * 0.5f + 0.5f;
                float uvY = sin * 0.5f + 0.5f;

                // 2 - Face superior
                vertices.AddRange(new float[] { x, halfHeight, z });// Posição
                vertices.AddRange(new float[] { 0.0f, 1.0f, 0.0f });// Normal
                vertices.AddRange(new float[] { uvX, uvY });// UV

                // 3 - Face inferior
                vertices.AddRange(new float[] { x, -halfHeight, z });// Posição
                vertices.AddRange(new float[] { 0.0f, -1.0f, 0.0f });// Normal
                vertices.AddRange(new float[] { -uvX, uvY });// UV
            }

            List<uint> indices = new();
            // Triângulos laterais
            for (uint i = 0; i < segments; i++)
            {
                uint i0 = i * 4;
                uint i1 = i0 + 1;
                uint i2 = i0 + 4;
                uint i3 = i0 + 5;

                indices.AddRange(new uint[] { i0, i1, i2 });
                indices.AddRange(new uint[] { i1, i3, i2 });
            }
            // Triângulos superiores
            for (uint i = 0; i < segments; i++)
            {
                uint i0 = 2;
                uint i1 = i * 4 + 2;
                uint i2 = (i + 1) * 4 + 2;

                indices.AddRange(new uint[] { i0, i1, i2 });
            }
            // Triângulos inferiores
            for (uint i = 0; i < segments; i++)
            {
                uint i0 = 3;
                uint i1 = i * 4 + 3;
                uint i2 = (i + 1) * 4 + 3;

                indices.AddRange(new uint[] { i1, i0, i2 });
            }

            return new Mesh(vertices.ToArray(), indices.ToArray());
        }

        // Criação de esfera, com definição de raio, número de segmentos(linhas de latitude) e número de aneis(linhas de longitude)
        public static Mesh CreateSphere(float radius = 0.5f, uint segments = 32, uint rings = 16)
        {
            List<float> vertices = new();
            for (uint i = 0; i <= rings; i++)
            {
                float valueY = (float)i / rings;
                float mult = MathF.Sin(valueY * MathF.PI);
                float cosY = MathF.Cos(valueY * MathF.PI);
                float y = -cosY;
                for (uint j = 0; j <= segments; j++)
                {
                    float valueX = (float)j / segments;

                    float cosX = MathF.Cos(MathF.Tau * valueX);
                    float sinZ = MathF.Sin(MathF.Tau * valueX);

                    float x = cosX * mult;
                    float z = -sinZ * mult;

                    vertices.AddRange(new float[] { x * radius, y * radius, z * radius });// Posição
                    vertices.AddRange(new float[] { x, y, z });// Normal
                    vertices.AddRange(new float[] { valueX, valueY });// UV
                }
            }

            List<uint> indices = new();
            for (uint i = 0; i < rings; i++)
            {
                for (uint j = 0; j < segments; j++)
                {
                    uint i0 = i * (segments + 1) + j;
                    uint i1 = i * (segments + 1) + j + 1;
                    uint i2 = (i + 1) * (segments + 1) + j + 1;
                    uint i3 = (i + 1) * (segments + 1) + j;

                    indices.AddRange(new uint[] { i0, i1, i2 });
                    indices.AddRange(new uint[] { i0, i2, i3 });
                }
            }
            return new Mesh(vertices.ToArray(), indices.ToArray());
        }

        // Cone, com raio da base, altura e número de segmentos ao redor
        // Note que o vértice superior não é reaproveitado, devido à necessidade de normais diferentes para cada vértice
        public static Mesh CreateCone(float radius = 0.5f, float height = 1.0f, uint segments = 16)
        {
            List<float> vertices = new();
            List<uint> indices = new();

            float half = height / 2f;
            for (uint i = 0; i < segments + 1; i++)
            {
                float value = i / (float)segments;
                float x = MathF.Sin(value * MathF.Tau);
                float z = MathF.Cos(value * MathF.Tau);

                Vector3 normal = new Vector3(x * height, radius, z * height).Normalized();

                // Parede inferior
                vertices.AddRange(new float[] { x * radius, -half, z * radius });// Posição
                vertices.AddRange(new float[] { normal.X, normal.Y, normal.Z });// Normal
                vertices.AddRange(new float[] { x / 2f + 0.5f, -z / 2f + 0.5f });// UV
                // Parede superior
                vertices.AddRange(new float[] { 0f, half, 0f });// Posição
                vertices.AddRange(new float[] { normal.X, normal.Y, normal.Z });// Normal
                vertices.AddRange(new float[] { 0.5f, 0.5f });// UV
                // Baixo
                vertices.AddRange(new float[] { x * radius, -half, z * radius });// Posição
                vertices.AddRange(new float[] { 0f, -1f, 0f });// Normal
                vertices.AddRange(new float[] { x / 2f + 0.5f, z / 2f + 0.5f });// UV
            }

            // Paredes
            for (uint i = 0; i < segments; i++)
            {
                uint one = i * 3;
                uint two = (i + 1) * 3;
                uint three = one + 1;

                indices.AddRange(new[] { one, two, three });
            }
            // Baixo
            for (uint i = 0; i < segments - 1; i++)
            {
                uint one = 2;
                uint two = (i + 2) * 3 + 2;
                uint three = (i + 1) * 3 + 2;

                indices.AddRange(new[] { one, two, three });
            }
            return new Mesh(vertices.ToArray(), indices.ToArray());
        }
    }
}
