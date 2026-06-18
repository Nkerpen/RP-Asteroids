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
        private Mesh _debrisMesh;
        private Mesh _thrusterMesh;
        private Transform _shipTransform = new();
        private Vector3 _shipVelocity = Vector3.Zero;
        private float _shipRotationSpeed = 220f;
        private float _shipThrust = 12f;
        private float _drag = 0.65f;
        private bool _isThrusting = false;
        private float _visualDrift = 0f;

        // Estado do Jogo
        private bool _isGameOver = false;
        private int _score = 0;
        private float _survivalTime = 0f; // Novo: Temporizador de sobrevivencia

        // Spawner Progressivo
        private float _spawnTimer = 0f;
        private float _spawnInterval = 4.0f;

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
        private Material _thrusterMaterial;
        private Mesh _postMesh;
        private RenderTarget _postTarget;

        private readonly Camera _camera;

        // Limites da tela
        private float _limitX;
        private float _limitY;

        public Game(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
            float cameraHeight = 20f;
            float aspect = (float)Size.X / Size.Y;
            _camera = new OrthographicCamera(cameraHeight * aspect, cameraHeight);
            _camera.position = new Vector3(0f, 0f, 10f);
            _camera.rotation = Vector3.Zero;

            _limitY = cameraHeight / 2f;
            _limitX = _limitY * aspect;

            //_shipMesh = Primitive.CreateCone(0.4f, 1.2f, 3);
            _shipMesh = CreateCustomShipMesh();
            _debrisMesh = Primitive.CreateCone(0.5f, 1.0f, 3);
            _thrusterMesh = CreateSegmentedThruster(5);
            _postMesh = Primitive.CreatePost();
            _postTarget = RenderTarget.CreateColor(Size.X, Size.Y);

            _projectileMesh = Primitive.CreateCube(1f);

            ShaderProgram gameProgram = new ShaderProgram(
                VertexShader.LoadFromFile("./assets/shaders/game.vert"),
                FragmentShader.LoadFromFile("./assets/shaders/game.frag")
            );

            ShaderProgram postProgram = new ShaderProgram(
                VertexShader.LoadFromFile("./assets/shaders/post.vert"),
                FragmentShader.LoadFromFile("./assets/shaders/post.frag")
            );

            ShaderProgram thrusterProgram = new ShaderProgram(
            VertexShader.LoadFromFile("./assets/shaders/thruster.vert"),
            FragmentShader.LoadFromFile("./assets/shaders/game.frag")
            );
            _thrusterMaterial = new Material(thrusterProgram);

            _gameMaterial = new Material(gameProgram);
            _gameMaterial.cull = false;

            _postMaterial = new Material(postProgram);
            _postMaterial.SetTexture("u_Texture", _postTarget.Texture);



            CursorState = CursorState.Normal;

            RestartGame();
        }

        private void RestartGame()
        {
            _isGameOver = false;
            _score = 0;
            _survivalTime = 0f;
            _spawnTimer = 0f;
            _spawnInterval = 4.0f;

            Title = $"Asteroids OpenGL - Score: {_score} | Time: 0.0s";

            _shipTransform.position = Vector3.Zero;
            _shipTransform.rotation = Vector3.Zero;
            _shipVelocity = Vector3.Zero;

            _shipTransform.scale = new Vector3(0.5f, 0.5f, 0.5f);

            _shipDebris.Clear();
            _debrisVelocities.Clear();
            _projectiles.Clear();
            _asteroids.Clear();

            // Asteroides iniciais com Zona Segura
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
                } while (pos.Length < 4.0f);

                Vector3 vel = new Vector3(
                    ((float)_random.NextDouble() * 2f - 1f) * 3f,
                    ((float)_random.NextDouble() * 2f - 1f) * 3f,
                    0f
                );

                int randomSize = _random.Next(1, 4);
                _asteroids.Add(new Asteroid(pos, vel, randomSize));
            }
        }

        private void SpawnAsteroidAtEdge()
        {
            Vector3 pos = Vector3.Zero;
            int side = _random.Next(0, 4); // 0=Cima, 1=Baixo, 2=Esquerda, 3=Direita

            if (side == 0) pos = new Vector3(((float)_random.NextDouble() * 2f - 1f) * _limitX, _limitY + 1f, 0f);
            else if (side == 1) pos = new Vector3(((float)_random.NextDouble() * 2f - 1f) * _limitX, -_limitY - 1f, 0f);
            else if (side == 2) pos = new Vector3(-_limitX - 1f, ((float)_random.NextDouble() * 2f - 1f) * _limitY, 0f);
            else pos = new Vector3(_limitX + 1f, ((float)_random.NextDouble() * 2f - 1f) * _limitY, 0f);

            // Direciona levemente para o centro da tela
            Vector3 vel = new Vector3(
                ((float)_random.NextDouble() * 2f - 1f) * 2f - (pos.X * 0.1f),
                ((float)_random.NextDouble() * 2f - 1f) * 2f - (pos.Y * 0.1f),
                0f
            );

            int randomSize = _random.Next(2, 4);
            _asteroids.Add(new Asteroid(pos, vel, randomSize));
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);
            float delta = (float)args.Time;

            if (!_isGameOver)
            {
                // ATUALIZAR TEMPORIZADOR NA JANELA
                _survivalTime += delta;
                Title = $"Asteroids OpenGL - Score: {_score} | Time: {Math.Round(_survivalTime, 1)}s";

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

                Vector3 rightVector = new Vector3(_shipTransform.Up.Y, -_shipTransform.Up.X, 0f);
                float targetDrift = Vector3.Dot(_shipVelocity, rightVector);

                if (Math.Abs(targetDrift) < 0.3f)
                {
                    targetDrift = 0f;
                }

                _visualDrift = MathHelper.Lerp(_visualDrift, targetDrift, 200f * delta);

                // --- NAVE: SCREEN WRAP ---
                if (_shipTransform.position.X > _limitX) _shipTransform.position.X = -_limitX;
                if (_shipTransform.position.X < -_limitX) _shipTransform.position.X = _limitX;
                if (_shipTransform.position.Y > _limitY) _shipTransform.position.Y = -_limitY;
                if (_shipTransform.position.Y < -_limitY) _shipTransform.position.Y = _limitY;

                // --- SPAWNER PROGRESSIVO ---
                _spawnTimer += delta;
                if (_spawnTimer >= _spawnInterval)
                {
                    _spawnTimer = 0f;
                    if (_spawnInterval > 1.0f) _spawnInterval -= 0.1f;
                    SpawnAsteroidAtEdge();
                }

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

                            // Pontuacao
                            if (destroyed.size == 3) _score += 20;
                            else if (destroyed.size == 2) _score += 50;
                            else _score += 100;

                            Title = $"Asteroids OpenGL - Score: {_score} | Time: {Math.Round(_survivalTime, 1)}s";

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
                        Title = $"GAME OVER - Press 'R' to Restart | Score: {_score} | Time: {Math.Round(_survivalTime, 1)}s";

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
                _isThrusting = false;
                for (int i = 0; i < _shipDebris.Count; i++)
                {
                    _shipDebris[i].position += _debrisVelocities[i] * delta;
                    _shipDebris[i].rotation.X += 300f * delta;
                    _shipDebris[i].rotation.Y += 200f * delta;
                }

                if (KeyboardState.IsKeyDown(Keys.R)) RestartGame();
            }

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

            GL.Enable(EnableCap.CullFace);
            GL.Enable(EnableCap.DepthTest);

            // ==========================================
            // 1. RENDERIZACAO DA NAVE, FOGO (OU DESTROCOS)
            // ==========================================
            _gameMaterial.Program.SetUniform("u_IsAsteroid", 0);
            _gameMaterial.Program.SetUniform("u_IsThrusting", _isThrusting ? 1 : 0);

            if (!_isGameOver)
            {
                // Desenha a Nave
                _shipTransform.Apply(_gameMaterial);
                GL.CullFace(TriangleFace.Front);
                _gameMaterial.Program.SetUniform("u_IsOutline", 1);
                _shipMesh.Draw();
                GL.CullFace(TriangleFace.Back);
                _gameMaterial.Program.SetUniform("u_IsOutline", 0);
                _shipMesh.Draw();

                // === NOVO: DESENHA O FOGO DO PROPULSOR (ESTILO VETOR E DINÂMICO) ===
                if (_isThrusting)
                {
                    Transform fireTransform = new Transform();
                    fireTransform.position = _shipTransform.position - _shipTransform.Up * 0.75f;
                    fireTransform.rotation = _shipTransform.rotation;
                    fireTransform.position.Z -= 0.1f;

                    float flickerY = 0.6f + (float)Math.Sin(_time * 40.0) * 0.2f;
                    float flickerX = 0.45f + (float)Math.Cos(_time * 50.0) * 0.05f;
                    fireTransform.scale = new Vector3(flickerX, flickerY, flickerX);

                    _thrusterMaterial.Use();
                    _thrusterMaterial.Program.SetUniform("u_Drift", _visualDrift * -0.025f);

                    fireTransform.Apply(_thrusterMaterial);
                    GL.CullFace(TriangleFace.Front);

                    // Aplica o material do thruster
                    _thrusterMaterial.Use();

                    // Renderiza (A base fixa na nave, a ponta dobra no shader)
                    GL.CullFace(TriangleFace.Front);
                    _thrusterMaterial.Program.SetUniform("u_IsOutline", 1);
                    _thrusterMesh.Draw();
                    GL.CullFace(TriangleFace.Back);
                    _thrusterMaterial.Program.SetUniform("u_IsOutline", 0);
                    _thrusterMesh.Draw();
                }
            }
            else
            {
                // ... (o foreach var debris in _shipDebris continua igualzinho aqui) ...
                foreach (var debris in _shipDebris)
                {
                    debris.Apply(_gameMaterial);

                    GL.CullFace(TriangleFace.Front);
                    _gameMaterial.Program.SetUniform("u_IsOutline", 1);
                    _debrisMesh.Draw();

                    GL.CullFace(TriangleFace.Back);
                    _gameMaterial.Program.SetUniform("u_IsOutline", 0);
                    _debrisMesh.Draw();
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

        private Mesh CreateCustomShipMesh()
        {
            // ARRAY DE VERTICES
            // Formato: Posicao XYZ, Normal XYZ, UV XY
            float[] vertices = {
            // Indice 0: Bico da Nave (Frente)
                0.0f,  1.0f,  0.0f,    0f, 1f, 0f,    0.5f, 1.0f,
            // Indice 1: Asa Esquerda (Tras)
                -0.8f, -1.0f,  0.0f,   -1f, 0f, 0f,    0.0f, 0.0f,
            // Indice 2: Asa Direita (Tras)
                 0.8f, -1.0f,  0.0f,    1f, 0f, 0f,    1.0f, 0.0f,
            // Indice 3: Cabine superior (Eixo Z positivo)
                 0.0f, -0.6f,  0.4f,    0f, 0f, 1f,    0.5f, 0.5f,
            // Indice 4: Barriga (Eixo Z negativo)
                 0.0f, -0.6f, -0.4f,    0f, 0f,-1f,    0.5f, 0.5f
        };

            // ARRAY DE INDICES (Montando os triangulos ligando os pontos acima)
            uint[] indices = {
                0, 1, 3, // Triangulo Frontal Esquerdo Superior
                0, 3, 2, // Triangulo Frontal Direito Superior
                0, 2, 4, // Triangulo Frontal Direito Inferior
                0, 4, 1, // Triangulo Frontal Esquerdo Inferior
                1, 4, 3, // Turbina Esquerda (Parte de tras)
                2, 3, 4  // Turbina Direita (Parte de tras)
        };

            return new Mesh(vertices, indices);
        }

        private Mesh CreateSegmentedThruster(int verticalSegments = 5)
        {
            List<float> vertices = new();
            List<uint> indices = new();

            int sides = 3; // Mantém o visual de triângulo 3D
            float baseRadius = 0.45f;

            // 1. Gerar os vértices em fatias
            for (int i = 0; i <= verticalSegments; i++)
            {
                float v = (float)i / verticalSegments; // Vai de 0.0 (Base) até 1.0 (Ponta)
                float y = 0.5f - v; // Posiciona entre Y = 0.5 e Y = -0.5
                float radius = baseRadius * (1.0f - v); // O raio diminui até virar 0 na ponta

                for (int j = 0; j <= sides; j++)
                {
                    float u = (float)j / sides;
                    float angle = u * MathF.Tau;
                    float x = MathF.Sin(angle) * radius;
                    float z = MathF.Cos(angle) * radius;

                    // Posição XYZ, Normal XYZ simplificada, UV
                    vertices.AddRange(new float[] { x, y, z });
                    vertices.AddRange(new float[] { x, 0.2f, z });
                    vertices.AddRange(new float[] { u, v });
                }
            }

            // 2. Ligar os vértices criando os triângulos
            int vertsPerRow = sides + 1;
            for (int i = 0; i < verticalSegments; i++)
            {
                for (int j = 0; j < sides; j++)
                {
                    uint top1 = (uint)(i * vertsPerRow + j);
                    uint top2 = top1 + 1;
                    uint bot1 = (uint)((i + 1) * vertsPerRow + j);
                    uint bot2 = bot1 + 1;

                    indices.AddRange(new uint[] { top1, bot1, top2 });
                    indices.AddRange(new uint[] { top2, bot1, bot2 });
                }
            }

            return new Mesh(vertices.ToArray(), indices.ToArray());
        }
    }
}