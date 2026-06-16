using System.Drawing;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using RP.Core;

namespace RP
{
    internal class Game : GameWindow
    {
        private float _time = 0f;

        private Mesh _objectMesh;
        private Mesh _planeMesh;
        private Mesh _skyboxMesh;
        private Mesh _postMesh;
        private Mesh _instancedMesh;

        private Transform _objectTransform = new();
        private Transform _planeTransform = new();
        private Transform _skyboxTransform = new();

        private Material _skyboxMaterial;
        private Material _planeMaterial;
        private Material _objectMaterial;
        private Material _postMaterial;
        private Material _instancedMaterial;

        private readonly Camera _camera;
        private readonly DirectionalLight _directionalLight = new();

        private RenderTarget _shadowMap;
        private RenderTarget _postTarget;

        private int _instanceCount = 250000;

        public Game(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings)
        {
            _objectMesh = Primitive.CreateCube(1f);
            _planeMesh = Primitive.CreatePlane(100f);
            _skyboxMesh = Primitive.CreateSphere(10f);
            _postMesh = Primitive.CreatePost();

            {// mesh customizada para uso com instancing
                float[] vertices = [
                    -0.5f, 0f, 0f,  0f, 1f, 0f,  0f, 0f,
                     0.5f, 0f, 0f,  0f, 1f, 0f,  1f, 0f,
                     0.5f, 1f, 0f,  0f, 1f, 0f,  1f, 1f,
                    -0.5f, 1f, 0f,  0f, 1f, 0f,  0f, 1f,
                ];

                uint[] indices = [
                    0, 1, 2,
                    0, 2, 3,
                ];

                _instancedMesh = new(vertices, indices);

                Matrix4[] modelMatrices = new Matrix4[_instanceCount];
                Vector3[] colors = new Vector3[_instanceCount];
                Random r = new();
                float areaSize = 100f;
                for (int i = 0; i < _instanceCount; i++)
                {
                    Transform t = new();
                    t.position = new Vector3(
                        r.NextSingle() * areaSize - areaSize / 2f,
                        0f,
                        r.NextSingle() * areaSize - areaSize / 2f
                    );
                    t.scale.Y = 0.5f + r.NextSingle() * 0.5f;
                    t.rotation.Y = r.NextSingle() * 360f;

                    modelMatrices[i] = t.ModelMatrix;// variação de posição
                    colors[i] = new Vector3(1f) * (r.NextSingle() * 0.4f + 0.6f);// variação de cor
                }
                _instancedMesh.SetInstanceData(3, modelMatrices);
                _instancedMesh.SetInstanceData(7, colors);
            }

            _objectTransform.position.Y = 0.5f;

            _shadowMap = RenderTarget.CreateDepth(4096, 4096);
            _postTarget = RenderTarget.CreateColor(Size.X, Size.Y);

            //Para exibir os elementos na tela, é preciso que os dados sejam processados na placa de vídeo.
            //É aí que entram os shaders, programas que serão executados diretamente na GPU, escritos em
            //linguagem GLSL.

            //Primeiro, criamos um vertex shader, que vai ser executado para cada vértice da nossa malha,
            //ele é responsável por determinar a posição de cada triângulo na tela.
            Shader vertexShader = VertexShader.LoadFromFile("./assets/shaders/phong.vert");

            //Em seguida, criamos o fragment shader, responsável pela cor de cada fragmento/pixel dos triângulos.
            Shader fragmentShader = FragmentShader.LoadFromFile("./assets/shaders/phong.frag");

            //Existem outros tipos de shader, mas o vertex shader e fragment shader são os únicos obrigatórios

            //O passo final é juntar os 2 shaders compilados em um único programa, garantindo a compatibilidade
            //entre os 2.
            ShaderProgram _shaderProgram = new ShaderProgram(vertexShader, fragmentShader);
            _shaderProgram.Use();

            ShaderProgram _skyboxProgram = new ShaderProgram(
                VertexShader.LoadFromFile("./assets/shaders/skybox.vert"),
                FragmentShader.LoadFromFile("./assets/shaders/skybox.frag")
            );

            ShaderProgram _postProgram = new ShaderProgram(
                VertexShader.LoadFromFile("./assets/shaders/post.vert"),
                FragmentShader.LoadFromFile("./assets/shaders/post.frag")
            );

            ShaderProgram _instancedProgram = new ShaderProgram(
                VertexShader.LoadFromFile("./assets/shaders/instanced.vert"),
                FragmentShader.LoadFromFile("./assets/shaders/instanced.frag")
            );

            //_camera = new OrthographicCamera(Size.X / 100f, Size.Y / 100f);
            _camera = new PerspectiveCamera(90f, (float)Size.X / Size.Y);
            _camera.position.Z = 4.5f;

            _directionalLight.rotation.X = -90f;

            _skyboxMaterial = new(_skyboxProgram);
            _skyboxMaterial.cullMode = TriangleFace.Front;
            _skyboxMaterial.depthWrite = false;

            _planeMaterial = new(_shaderProgram);
            _planeMaterial.SetVec3("u_Color", 0.6f, 0.5f, 0.3f);
            _planeMaterial.SetFloat("u_Smoothness", 0.1f);
            _planeMaterial.SetTexture("u_Texture", Texture.Default);

            _objectMaterial = new(_shaderProgram);
            _objectMaterial.SetVec3("u_Color", 1f, 0f, 0f);
            _objectMaterial.SetFloat("u_Smoothness", 1f);
            _objectMaterial.SetTexture("u_Texture", Texture.Default);

            _postMaterial = new(_postProgram);
            _postMaterial.SetTexture("u_Texture", _postTarget.Texture);

            TextureSettings settings = TextureSettings.Default;
            settings.AddSetting(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
            settings.AddSetting(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);
            Texture grassTexture = new Texture("./assets/textures/grass.png", settings);

            _instancedMaterial = new(_instancedProgram);
            _instancedMaterial.SetTexture("u_Texture", grassTexture);
            _instancedMaterial.SetVec3("u_Color", 1f, 1f, 1f);
            _instancedMaterial.SetFloat("u_Smoothness", 0.1f);
            _instancedMaterial.cull = false;

            CursorState = CursorState.Grabbed;
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            float delta = (float)args.Time;

            // Movimentação da câmera, modo drone, em todas as direções.
            float cameraSpeed = 5f;
            if (KeyboardState.IsKeyDown(Keys.D))
            {
                _camera.position += _camera.Right * delta * cameraSpeed;
            }
            if (KeyboardState.IsKeyDown(Keys.A))
            {
                _camera.position -= _camera.Right * delta * cameraSpeed;
            }
            if (KeyboardState.IsKeyDown(Keys.E))
            {
                _camera.position += _camera.Up * delta * cameraSpeed;
            }
            if (KeyboardState.IsKeyDown(Keys.Q))
            {
                _camera.position -= _camera.Up * delta * cameraSpeed;
            }
            if (KeyboardState.IsKeyDown(Keys.W))
            {
                _camera.position += _camera.Forward * delta * cameraSpeed;
            }
            if (KeyboardState.IsKeyDown(Keys.S))
            {
                _camera.position -= _camera.Forward * delta * cameraSpeed;
            }

            // Rotação da câmera, modo drone, com limitação com a visão para cima e para baixo
            _camera.rotation -= new Vector3(MouseState.Delta.Y, MouseState.Delta.X, 0f) * 0.3f;
            _camera.rotation.X = MathF.Min(MathF.Max(_camera.rotation.X, -89f), 89f);
            if (KeyboardState.IsKeyDown(Keys.Escape))
            {
                CursorState = CursorState.Normal;
            }
        }

        private void DrawScene(Camera camera, bool drawGrass = true)
        {
            //Determinamos a cor de limpeza da tela e, na sequência, pedimos pra limpar os canais de cor.
            GL.ClearColor(0.0f, 0.3f, 0.5f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // Matrizes de visão e projeção, relacionadas à câmera
            camera.GlobalApply();

            // Aqui, fazemos o desenho de cada transform presente
            Material.SetGlobalTexture("u_ShadowMap", _shadowMap.Texture);

            _skyboxMaterial.Use();
            _skyboxTransform.Apply(_skyboxMaterial);// envio das matrizes
            _skyboxMesh.Draw();

            GL.CullFace(TriangleFace.Back);
            GL.DepthMask(true);

            _planeMaterial.Use();
            _planeTransform.Apply(_planeMaterial);// envio das matrizes
            _planeMesh.Draw();
            _objectMaterial.Use();
            _objectTransform.Apply(_objectMaterial);// envio das matrizes
            _objectMesh.Draw();

            if (drawGrass)
            {
                _instancedMaterial.Use();
                _instancedMesh.Draw(_instanceCount);
            }
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            float delta = (float)args.Time;
            _time += delta;

            //_skyboxTransform.position = _camera.position;

            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(TriangleFace.Back);

            _directionalLight.GlobalApply();
            _directionalLight.rotation.X += 1f * delta;

            Camera shadowMapCamera = _directionalLight.GetLightMapCamera(_camera.position);
            Material.SetGlobalMat4("u_Light", shadowMapCamera.ViewMatrix * shadowMapCamera.ProjectionMatrix);
            Material.SetGlobalFloat("u_Time", _time);

            _shadowMap.Use();
            DrawScene(shadowMapCamera, false);

            _postTarget.Use();
            DrawScene(_camera);

            RenderTarget.Reset(this);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            _postMaterial.Use();
            _postMesh.Draw();

            //Como todos comandos de desenho são feitos em um buffer, ou tela, secundário, precisamos pedir
            //que os buffers sejam trocados para que o novo desenho seja exibido ao usuário.
            SwapBuffers();
        }
    }
}
