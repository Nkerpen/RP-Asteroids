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

        // Elementos da Nave
        private Mesh _shipMesh;
        private Transform _shipTransform = new();
        private Vector3 _shipVelocity = Vector3.Zero;
        private float _shipRotationSpeed = 220f;
        private float _shipThrust = 12f;
        private float _drag = 0.65f;
        private bool _isThrusting = false; // Guardamos o estado de aceleracao para o Shader

        // Estado do Jogo
        private bool _isGameOver = false;

        // Destrocos da Nave (Explosao)
        private List<Transform> _shipDebris = new List<Transform>();
        private List<Vector3> _debrisVelocities = new List<Vector3>();

        // Listas de Entidades
        private List<Asteroid> _asteroids = new List<Asteroid>();
        private Random _random = new Random();

        // Projeteis
        private List<Projectile> _projectiles = new List<Projectile>();
        private Mesh _projectileMesh;

        // Materiais e Post-Processing
        private Material _gameMaterial;
        private Material _postMaterial;
        private Mesh _postMesh;
        private RenderTarget _postTarget;

        private readonly Camera _camera;

        // Limites da tela para o Screen Wrap
        private float _limitX;
        private float _limitY;

        public Game(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
            // 1. Configuracao da Camera Ortografica
            float cameraHeight = 20f;
            float aspect = (float)Size.X / Size.Y;
            _camera = new OrthographicCamera(cameraHeight * aspect, cameraHeight);
            _camera.position = new Vector3(0f, 0f, 10f);
            _camera.rotation = Vector3.Zero;

            _limitY = cameraHeight / 2f;
            _limitX = _limitY * aspect;

            // 2. Criacao das Geometrias
            _shipMesh = Primitive.CreateCone(0.4f, 1.2f, 3);
            _postMesh = Primitive.CreatePost();
            _postTarget = RenderTarget.CreateColor(Size.X, Size.Y);

            // MALHA DO PROJETIL: Inicializada AQUI para evitar NullReferenceException
            _projectileMesh = Primitive.CreateCube(1f);

            // 3. Compilacao dos Shaders
            ShaderProgram gameProgram = new ShaderProgram(
                VertexShader.LoadFromFile("./assets/shaders/game.vert"),
                FragmentShader.LoadFromFile("./assets/shaders/game.frag")
            );

            ShaderProgram postProgram = new ShaderProgram(
                VertexShader.LoadFromFile("./assets/shaders/post.vert"),
                FragmentShader.LoadFromFile("./assets/shaders/post.frag")
            );

            // 4. Configuracao dos Materiais
            _gameMaterial = new Material(gameProgram);
            _gameMaterial.cull = false;

            _postMaterial = new Material(postProgram);
            _postMaterial.SetTexture("u_Texture", _postTarget.Texture);

            CursorState = CursorState.Normal;

            // Inicia o jogo criando os primeiros asteroides
            RestartGame();
        }

        private void RestartGame()
        {
            _isGameOver = false;

            // Reseta a Nave
            _shipTransform.position = Vector3.Zero;
            _shipTransform.rotation = Vector3.Zero;
            _shipVelocity = Vector3.Zero;

            // Limpa listas de objetos antigos
            _shipDebris.Clear();
            _debrisVelocities.Clear();
            _projectiles.Clear();
            _asteroids.Clear();

            // Recria os Asteroides com Zona Segura e Tamanhos Aleatorios
            for (int i = 0; i < 5; i++)
            {
                Vector3 pos;
                do
                {
                    pos = new Vector3(
                        ((float)_random.NextDouble() * 2f - 1f) * _limitX,
                        ((float)_random.NextDouble() * 2f - 1f) * _limitY,
                        0f
                    );
                } while (pos.Length < 4.0f); // Zona segura (distancia > 4 da nave)

                Vector3 vel = new Vector3(
                    ((float)_random.NextDouble() * 2f - 1f) * 3f,
                    ((float)_random.NextDouble() * 2f - 1f) * 3f,
                    0f
                );

                int randomSize = _random.Next(1, 4); // Sorteia entre Tamanho 1 e 3
                _asteroids.Add(new Asteroid(pos, vel, randomSize));
            }
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);
            float delta = (float)args.Time;

            if (!_isGameOver)
            {
                // --- NAVE: INPUT E FISICA ---
                if (KeyboardState.IsKeyDown(Keys.A)) _shipTransform.rotation.Z += _shipRotationSpeed * delta;
                if (KeyboardState.IsKeyDown(Keys.D)) _shipTransform.rotation.Z -= _shipRotationSpeed * delta;

                _isThrusting = KeyboardState.IsKeyDown(Keys.W) || KeyboardState.IsKeyDown(Keys.Up);
                if (_isThrusting)
                {
                    _shipVelocity += _shipTransform.Up * _shipThrust * delta;
                }

                _shipVelocity -= _shipVelocity * _drag * delta;
                _shipTransform.position += _shipVelocity * delta;

                // --- NAVE: SCREEN WRAP ---
                if (_shipTransform.position.X > _limitX) _shipTransform.position.X = -_limitX;
                if (_shipTransform.position.X < -_limitX) _shipTransform.position.X = _limitX;
                if (_shipTransform.position.Y > _limitY) _shipTransform.position.Y = -_limitY;
                if (_shipTransform.position.Y < -_limitY) _shipTransform.position.Y = _limitY;

                // --- PROJETEIS: ATIRAR ---
                if (KeyboardState.IsKeyPressed(Keys.Space))
                {
                    _projectiles.Add(new Projectile(_shipTransform.position + _shipTransform.Up * 0.5f, _shipTransform.Up));
                }

                // --- PROJETEIS: ATUALIZAR E REMOVER ---
                for (int i = _projectiles.Count - 1; i >= 0; i--)
                {
                    _projectiles[i].Update(delta, _limitX, _limitY);
                    if (_projectiles[i].lifespan <= 0f)
                    {
                        _projectiles.RemoveAt(i);
                    }
                }

                // --- COLISAO: PROJETIL X ASTEROIDE ---
                for (int i = _projectiles.Count - 1; i >= 0; i--)
                {
                    bool hit = false;
                    for (int j = _asteroids.Count - 1; j >= 0; j--)
                    {
                        float dist = (_projectiles[i].transform.position - _asteroids[j].transform.position).Length;

                        if (dist < _asteroids[j].radius)
                        {
                            hit = true;
                            Asteroid destroyed = _asteroids[j];
                            _asteroids.RemoveAt(j);

                            if (destroyed.size > 1)
                            {
                                int newSize = destroyed.size - 1;
                                for (int k = 0; k < 2; k++)
                                {
                                    Vector3 newVel = destroyed.velocity;
                                    newVel.X += ((float)_random.NextDouble() * 2f - 1f) * 2f;
                                    newVel.Y += ((float)_random.NextDouble() * 2f - 1f) * 2f;
                                    _asteroids.Add(new Asteroid(destroyed.transform.position, newVel, newSize));
                                }
                            }
                            break;
                        }
                    }
                    if (hit) _projectiles.RemoveAt(i);
                }

                // --- COLISAO: NAVE X ASTEROIDE ---
                float shipRadius = 0.4f;
                foreach (var asteroid in _asteroids)
                {
                    float distance = (_shipTransform.position - asteroid.transform.position).Length;
                    if (distance < (shipRadius + asteroid.radius))
                    {
                        _isGameOver = true;

                        int debrisCount = _random.Next(3, 7);
                        for (int i = 0; i < debrisCount; i++)
                        {
                            Transform debrisTransform = new Transform();
                            debrisTransform.position = _shipTransform.position;
                            debrisTransform.scale = new Vector3(
                                0.1f + (float)_random.NextDouble() * 0.4f,
                                0.1f + (float)_random.NextDouble() * 0.5f,
                                0.1f + (float)_random.NextDouble() * 0.3f
                            );
                            debrisTransform.rotation = new Vector3(
                                (float)_random.NextDouble() * 360f,
                                (float)_random.NextDouble() * 360f,
                                (float)_random.NextDouble() * 360f
                            );
                            _shipDebris.Add(debrisTransform);

                            float angle = (float)_random.NextDouble() * MathF.Tau;
                            float speed = 3f + (float)_random.NextDouble() * 6f;
                            _debrisVelocities.Add(new Vector3(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed, 0f));
                        }
                        break;
                    }
                }
            }
            else
            {
                // --- GAME OVER: ANIMAR DESTROCOS ---
                _isThrusting = false; // Desliga o jato da nave principal
                for (int i = 0; i < _shipDebris.Count; i++)
                {
                    _shipDebris[i].position += _debrisVelocities[i] * delta;
                    _shipDebris[i].rotation.X += 300f * delta;
                    _shipDebris[i].rotation.Y += 200f * delta;
                }

                // --- GAME OVER: REINICIAR ---
                if (KeyboardState.IsKeyDown(Keys.R))
                {
                    RestartGame();
                }
            }

            // --- ATUALIZAR ASTEROIDES (Ocorre sempre) ---
            foreach (var asteroid in _asteroids)
            {
                asteroid.Update(delta, _limitX, _limitY);
            }

            if (KeyboardState.IsKeyDown(Keys.Escape)) Close();
        }

        private void DrawScene(Camera camera)
        {
            GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            camera.GlobalApply();
            Material.SetGlobalFloat("u_Time", _time);

            _gameMaterial.Use();

            // ATIVANDO AS PROTEÇÕES MATEMÁTICAS 3D
            GL.Enable(EnableCap.CullFace);
            GL.Enable(EnableCap.DepthTest);

            // ==========================================
            // 1. RENDERIZACAO DA NAVE
            // ==========================================
            _gameMaterial.Program.SetUniform("u_IsAsteroid", 0);
            _gameMaterial.Program.SetUniform("u_IsThrusting", _isThrusting ? 1 : 0);

            if (!_isGameOver)
            {
                _shipTransform.Apply(_gameMaterial);

                // Passada A: Desenha o contorno usando apenas as faces de trás do 3D
                GL.CullFace(TriangleFace.Front);
                _gameMaterial.Program.SetUniform("u_IsOutline", 1);
                _shipMesh.Draw();

                // Passada B: Desenha o miolo preto usando as faces da frente do 3D
                GL.CullFace(TriangleFace.Back);
                _gameMaterial.Program.SetUniform("u_IsOutline", 0);
                _shipMesh.Draw();
            }
            else
            {
                foreach (var debris in _shipDebris)
                {
                    debris.Apply(_gameMaterial);

                    GL.CullFace(TriangleFace.Front);
                    _gameMaterial.Program.SetUniform("u_IsOutline", 1);
                    _shipMesh.Draw();

                    GL.CullFace(TriangleFace.Back);
                    _gameMaterial.Program.SetUniform("u_IsOutline", 0);
                    _shipMesh.Draw();
                }
            }

            // ==========================================
            // 2. RENDERIZACAO DOS PROJETEIS
            // ==========================================
            _gameMaterial.Program.SetUniform("u_IsAsteroid", 0);
            _gameMaterial.Program.SetUniform("u_IsThrusting", 0);
            foreach (var proj in _projectiles)
            {
                proj.transform.Apply(_gameMaterial);

                GL.CullFace(TriangleFace.Front);
                _gameMaterial.Program.SetUniform("u_IsOutline", 1);
                _projectileMesh.Draw();

                GL.CullFace(TriangleFace.Back);
                _gameMaterial.Program.SetUniform("u_IsOutline", 0);
                _projectileMesh.Draw();
            }

            // ==========================================
            // 3. RENDERIZACAO DOS ASTEROIDES
            // ==========================================
            _gameMaterial.Program.SetUniform("u_IsAsteroid", 1);
            foreach (var asteroid in _asteroids)
            {
                asteroid.transform.Apply(_gameMaterial);

                GL.CullFace(TriangleFace.Front);
                _gameMaterial.Program.SetUniform("u_IsOutline", 1);
                asteroid.mesh.Draw();

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

            _postTarget.Use();
            DrawScene(_camera);

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

            _postTarget.Destroy();
            _postTarget = RenderTarget.CreateColor(Size.X, Size.Y);
            _postMaterial.SetTexture("u_Texture", _postTarget.Texture);
        }
    }
}