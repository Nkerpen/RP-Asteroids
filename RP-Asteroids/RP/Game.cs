using System;
using System.Collections.Generic;
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

        // Elementos Principais do Jogo
        private Mesh _shipMesh;
        private Transform _shipTransform = new();
        private Vector3 _shipVelocity = Vector3.Zero;
        private List<Asteroid> _asteroids = new List<Asteroid>();
        private Random _random = new Random();
        
        // Parâmetros de Movimentação da Nave (Física de Inércia)
        private float _shipRotationSpeed = 220f; // Graus por segundo
        private float _shipThrust = 12f;
        private float _drag = 0.65f; // Fator de atrito espacial

        // Materiais e Pipeline de Pós-Processamento
        private Material _gameMaterial; // Material unificado dos objetos em cena
        private Material _postMaterial;
        private Mesh _postMesh;
        private RenderTarget _postTarget;

        private readonly Camera _camera;

        // Limites de tela calculados dinamicamente para o Screen Wrap
        private float _limitX;
        private float _limitY;

        public Game(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) 
            : base(gameWindowSettings, nativeWindowSettings)
        {
            // Ajuste da Câmera: Ortográfica fixa para o plano bidimensional XY
            float cameraHeight = 20f;
            float aspect = (float)Size.X / Size.Y;
            _camera = new OrthographicCamera(cameraHeight * aspect, cameraHeight);
            _camera.position = new Vector3(0f, 0f, 10f); // Recuada no eixo Z olhando para a origem
            _camera.rotation = Vector3.Zero;

            // Define os limites matemáticos exatos da Viewport ortográfica
            _limitY = cameraHeight / 2f;
            _limitX = _limitY * aspect;

            // Criação das geometrias essenciais
            _shipMesh = Primitive.CreateCone(0.4f, 1.2f, 3); // Cone de 3 lados atua perfeitamente como triângulo clássico
            _postMesh = Primitive.CreatePost();
            _postTarget = RenderTarget.CreateColor(Size.X, Size.Y);

            // Spawn de 5 asteroides iniciais em posições e direções aleatórias
            for (int i = 0; i < 5; i++)
            {
                Vector3 pos = new Vector3(
                    ((float)_random.NextDouble() * 2f - 1f) * _limitX,
                    ((float)_random.NextDouble() * 2f - 1f) * _limitY,
                    0f
            );
    
                Vector3 vel = new Vector3(
                    ((float)_random.NextDouble() * 2f - 1f) * 3f, // Velocidade X aleatória
                    ((float)_random.NextDouble() * 2f - 1f) * 3f, // Velocidade Y aleatória
                    0f
            );

                _asteroids.Add(new Asteroid(pos, vel, 1.5f)); // Raio inicial de 1.5
            }

            // Vinculação dos novos Shaders customizados do GDD
            ShaderProgram gameProgram = new ShaderProgram(
                VertexShader.LoadFromFile("./assets/shaders/game.vert"),
                FragmentShader.LoadFromFile("./assets/shaders/game.frag")
            );

            ShaderProgram postProgram = new ShaderProgram(
                VertexShader.LoadFromFile("./assets/shaders/post.vert"),
                FragmentShader.LoadFromFile("./assets/shaders/post.frag")
            );

            // Configuração e parametrização limpa de materiais
            _gameMaterial = new Material(gameProgram);
            _gameMaterial.cull = false; // Desativado para garantir visibilidade plena do triângulo no plano 2D

            _postMaterial = new Material(postProgram);
            _postMaterial.SetTexture("u_Texture", _postTarget.Texture);

            CursorState = CursorState.Normal;
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);
            float delta = (float)args.Time;

            // --- PROCESSAMENTO DE INPUTS E MOVIMENTAÇÃO DA NAVE ---
            // Rotação sobre o próprio eixo Z
            if (KeyboardState.IsKeyDown(Keys.A))
                _shipTransform.rotation.Z += _shipRotationSpeed * delta;
            if (KeyboardState.IsKeyDown(Keys.D))
                _shipTransform.rotation.Z -= _shipRotationSpeed * delta;

            // Propulsão frontal (Utiliza o vetor direcional local "Up" gerado pela rotação em Z)
            bool isThrusting = KeyboardState.IsKeyDown(Keys.W) || KeyboardState.IsKeyDown(Keys.Up);
            if (isThrusting)
            {
                _shipVelocity += _shipTransform.Up * _shipThrust * delta;
            }

            // Repasse do estado de aceleração para o Vertex Shader deformar a malha
            _gameMaterial.Program.SetUniform("u_IsThrusting", isThrusting ? 1 : 0);

            // Integração de Euler simples com amortecimento (Atrito)
            _shipVelocity -= _shipVelocity * _drag * delta;
            _shipTransform.position += _shipVelocity * delta;

            // --- SISTEMA DE SCREEN WRAP ---
            if (_shipTransform.position.X > _limitX)   _shipTransform.position.X = -_limitX;
            if (_shipTransform.position.X < -_limitX)  _shipTransform.position.X = _limitX;
            if (_shipTransform.position.Y > _limitY)   _shipTransform.position.Y = -_limitY;
            if (_shipTransform.position.Y < -_limitY)  _shipTransform.position.Y = _limitY;

            if (KeyboardState.IsKeyDown(Keys.Escape))
            {
                Close();
            }

            foreach (var asteroid in _asteroids)
            {
                asteroid.Update(delta, _limitX, _limitY);
            }
        }

        private void DrawScene(Camera camera)
        {
            // Fundo escuro absoluto de espaço de Arcade
            GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            camera.GlobalApply();
            Material.SetGlobalFloat("u_Time", _time);

            _gameMaterial.Use();
            GL.Enable(EnableCap.CullFace);

            // ==========================================
            // 1. RENDERIZAÇÃO DA NAVE DO JOGADOR
            // ==========================================
            _gameMaterial.Program.SetUniform("u_IsAsteroid", 0);
            _shipTransform.Apply(_gameMaterial);

            // Passada A: Desenha o casco traseiro expandido em Branco
            GL.CullFace(TriangleFace.Front); // Oculta as faces da frente
            _gameMaterial.Program.SetUniform("u_IsOutline", 1);
            _shipMesh.Draw();

            // Passada B: Desenha o preenchimento normal por cima
            GL.CullFace(TriangleFace.Back); // Oculta as faces de trás (Padrão)
            _gameMaterial.Program.SetUniform("u_IsOutline", 0);
            _shipMesh.Draw();


            // ==========================================
            // 2. RENDERIZAÇÃO DOS ASTEROIDES
            // ==========================================
            _gameMaterial.Program.SetUniform("u_IsAsteroid", 1);
            foreach (var asteroid in _asteroids)
            {
                asteroid.transform.Apply(_gameMaterial);

                // Passada A: Desenha o contorno do Asteroide
                GL.CullFace(TriangleFace.Front);
                _gameMaterial.Program.SetUniform("u_IsOutline", 1);
                asteroid.mesh.Draw();

                // Passada B: Desenha o centro do Asteroide
                GL.CullFace(TriangleFace.Back);
                _gameMaterial.Program.SetUniform("u_IsOutline", 0);
                asteroid.mesh.Draw();
            }
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);
            float delta = (float)args.Time;
            _time += delta;

            GL.Enable(EnableCap.DepthTest);

            // PASSO 1: Captura o desenho da cena gráfica dentro do Render Target secundário
            _postTarget.Use();
            DrawScene(_camera);

            // PASSO 2: Retorna ao Buffer de tela principal para aplicar o filtro de fragmento CRT
            RenderTarget.Reset(this);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            
            _postMaterial.Use();
            _postMesh.Draw();

            SwapBuffers();
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, Size.X, Size.Y);
            
            // Recriação e vinculação dinâmica do buffer de pós-processamento para evitar distorções de escala
            _postTarget.Destroy();
            _postTarget = RenderTarget.CreateColor(Size.X, Size.Y);
            _postMaterial.SetTexture("u_Texture", _postTarget.Texture);
        }
    }
}