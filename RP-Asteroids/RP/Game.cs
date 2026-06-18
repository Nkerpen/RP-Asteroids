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
        // ====================================================================
        // SEÇÃO 1: VARIÁVEIS E ESTADO GLOBAL
        // Aqui ficam declarados todos os componentes que existem na memória 
        // durante o ciclo de vida do jogo.
        // ====================================================================

        // --- SISTEMA E CÂMERA ---
        private float _time = 0f;          // Tempo global (usado para animações nos shaders)
        private readonly Camera _camera;   // Câmera ortográfica (2D)
        private float _limitX;             // Limite horizontal da tela para o "Screen Wrap"
        private float _limitY;             // Limite vertical da tela para o "Screen Wrap"

        // --- FÍSICA E CONTROLE DA NAVE ---
        private Transform _shipTransform = new();
        private Vector3 _shipVelocity = Vector3.Zero;
        private float _shipRotationSpeed = 220f; // Velocidade de giro (graus por segundo)
        private float _shipThrust = 12f;         // Força de aceleração do motor
        private float _drag = 0.65f;             // Atrito do espaço (freia a nave aos poucos)
        private bool _isThrusting = false;       // Flag visual: a nave está acelerando agora?
        private float _visualDrift = 0f;         // Valor suavizado da derrapagem (para o shader do fogo)

        // --- PROGRESSÃO E GAME OVER ---
        private bool _isGameOver = false;
        private int _score = 0;
        private float _survivalTime = 0f;
        private float _spawnTimer = 0f;
        private float _spawnInterval = 4.0f;     // Tempo (em seg) entre o surgimento de cada asteroide

        // --- ENTIDADES E LISTAS ---
        private List<Asteroid> _asteroids = new List<Asteroid>();
        private List<Projectile> _projectiles = new List<Projectile>();
        private Random _random = new Random();

        // --- DESTROÇOS (EXPLOSÃO) ---
        private List<Transform> _shipDebris = new List<Transform>();
        private List<Vector3> _debrisVelocities = new List<Vector3>();

        // --- RECURSOS GRÁFICOS (MESHES E MATERIAIS) ---
        // Meshes são as geometrias 3D (os vértices). Materiais são os Shaders aplicados a elas.
        private Mesh _shipMesh;
        private Mesh _debrisMesh;
        private Mesh _thrusterMesh;
        private Mesh _projectileMesh;
        private Material _gameMaterial;      // Shader principal (game.vert / game.frag)
        private Material _thrusterMaterial;  // Shader específico do fogo (thruster.vert)
        
        // --- POST-PROCESSING (EFEITO CRT) ---
        private Mesh _postMesh;              // Um quadrado plano que cobre a tela toda
        private RenderTarget _postTarget;    // Uma "tela de mentira" onde desenhamos o jogo antes de ir pro monitor
        private Material _postMaterial;      // Shader de Pós-Processamento (Aplica a distorção da TV de tubo)


        // ====================================================================
        // SEÇÃO 2: INICIALIZAÇÃO
        // Métodos chamados quando o jogo abre ou quando o jogador reinicia.
        // ====================================================================

        public Game(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
            // 1. Configura as proporções da tela e os limites virtuais
            float cameraHeight = 20f;
            float aspect = (float)Size.X / Size.Y;
            _camera = new OrthographicCamera(cameraHeight * aspect, cameraHeight);
            _camera.position = new Vector3(0f, 0f, 10f);
            _camera.rotation = Vector3.Zero;
            _limitY = cameraHeight / 2f;
            _limitX = _limitY * aspect;

            // 2. Instancia as Geometrias (Meshes)
            _shipMesh = CreateCustomShipMesh();
            _debrisMesh = Primitive.CreateCone(0.5f, 1.0f, 3);
            _thrusterMesh = CreateSegmentedThruster(5);
            _projectileMesh = Primitive.CreateCube(1f);
            
            // 3. Configura o Post-Processing
            _postMesh = Primitive.CreatePost();
            _postTarget = RenderTarget.CreateColor(Size.X, Size.Y);

            // 4. Compila os Shaders e os empacota em Materiais
            ShaderProgram gameProgram = new ShaderProgram(
                VertexShader.LoadFromFile("./assets/shaders/game.vert"),
                FragmentShader.LoadFromFile("./assets/shaders/game.frag")
            );
            _gameMaterial = new Material(gameProgram);
            _gameMaterial.cull = false;

            ShaderProgram thrusterProgram = new ShaderProgram(
                VertexShader.LoadFromFile("./assets/shaders/thruster.vert"),
                FragmentShader.LoadFromFile("./assets/shaders/game.frag")
            );
            _thrusterMaterial = new Material(thrusterProgram);

            ShaderProgram postProgram = new ShaderProgram(
                VertexShader.LoadFromFile("./assets/shaders/post.vert"),
                FragmentShader.LoadFromFile("./assets/shaders/post.frag")
            );
            _postMaterial = new Material(postProgram);
            _postMaterial.SetTexture("u_Texture", _postTarget.Texture);

            CursorState = CursorState.Normal;
            
            // Dá a partida inicial do jogo
            RestartGame();
        }

        /// <summary>
        /// Zera todas as estatísticas, limpa a tela e recria as condições iniciais.
        /// </summary>
        private void RestartGame()
        {
            _isGameOver = false;
            _score = 0;
            _survivalTime = 0f;
            _spawnTimer = 0f;
            _spawnInterval = 4.0f;
            Title = $"Asteroids OpenGL - Score: {_score} | Time: 0.0s";

            // Reseta a física da nave
            _shipTransform.position = Vector3.Zero;
            _shipTransform.rotation = Vector3.Zero;
            _shipVelocity = Vector3.Zero;
            _shipTransform.scale = new Vector3(0.5f, 0.5f, 0.5f);

            // Limpa as entidades da partida anterior
            _shipDebris.Clear();
            _debrisVelocities.Clear();
            _projectiles.Clear();
            _asteroids.Clear();

            // Sorteia os 5 asteroides iniciais respeitando uma "Zona Segura" longe do centro
            for (int i = 0; i < 5; i++)
            {
                Vector3 pos;
                do {
                    pos = new Vector3(
                        ((float)_random.NextDouble() * 2f - 1f) * _limitX,
                        ((float)_random.NextDouble() * 2f - 1f) * _limitY, 0f
                    );
                } while (pos.Length < 4.0f); // Só aceita posições com distância maior que 4 da nave

                Vector3 vel = new Vector3(
                    ((float)_random.NextDouble() * 2f - 1f) * 3f,
                    ((float)_random.NextDouble() * 2f - 1f) * 3f, 0f
                );

                _asteroids.Add(new Asteroid(pos, vel, _random.Next(1, 4)));
            }
        }


        // ====================================================================
        // SEÇÃO 3: LÓGICA DO JOGO (UPDATE LOOP)
        // Executado dezenas de vezes por segundo. Lida com Input, Física e Colisões.
        // ====================================================================

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);
            float delta = (float)args.Time;

            if (!_isGameOver)
            {
                // ATUALIZA O TEMPO NA TELA
                _survivalTime += delta;
                Title = $"Asteroids OpenGL - Score: {_score} | Time: {Math.Round(_survivalTime, 1)}s";

                // --- 1. CONTROLES E FÍSICA DA NAVE ---
                if (KeyboardState.IsKeyDown(Keys.A)) _shipTransform.rotation.Z += _shipRotationSpeed * delta;
                if (KeyboardState.IsKeyDown(Keys.D)) _shipTransform.rotation.Z -= _shipRotationSpeed * delta;

                _isThrusting = KeyboardState.IsKeyDown(Keys.W) || KeyboardState.IsKeyDown(Keys.Up);
                if (_isThrusting)
                {
                    // Acelera a nave na direção que ela está apontando
                    _shipVelocity += _shipTransform.Up * _shipThrust * delta;
                }

                // Aplica o arrasto (desaceleração) e move a nave
                _shipVelocity -= _shipVelocity * _drag * delta;
                _shipTransform.position += _shipVelocity * delta;

                // --- 2. CÁLCULO DA DERRAPAGEM (PARA O FOGO) ---
                // Descobre qual é a "Direita" da nave girando o vetor "Cima" em 90 graus
                Vector3 rightVector = new Vector3(_shipTransform.Up.Y, -_shipTransform.Up.X, 0f);
                
                // O Produto Escalar (Dot) diz o quanto da velocidade está indo para os lados
                float targetDrift = Vector3.Dot(_shipVelocity, rightVector);
                if (Math.Abs(targetDrift) < 0.3f) targetDrift = 0f; // Zona Morta (evita tremedeira)

                // Interpolação suave: anima o fogo voltando pro centro como uma mola
                _visualDrift = MathHelper.Lerp(_visualDrift, targetDrift, 8f * delta);

                // --- 3. SCREEN WRAP (TELA INFINITA) ---
                if (_shipTransform.position.X > _limitX) _shipTransform.position.X = -_limitX;
                if (_shipTransform.position.X < -_limitX) _shipTransform.position.X = _limitX;
                if (_shipTransform.position.Y > _limitY) _shipTransform.position.Y = -_limitY;
                if (_shipTransform.position.Y < -_limitY) _shipTransform.position.Y = _limitY;

                // --- 4. SPAWNER DE INIMIGOS (PROGRESSIVO) ---
                _spawnTimer += delta;
                if (_spawnTimer >= _spawnInterval)
                {
                    _spawnTimer = 0f;
                    if (_spawnInterval > 1.0f) _spawnInterval -= 0.1f; // Aumenta a dificuldade
                    SpawnAsteroidAtEdge();
                }

                // --- 5. TIROS E COLISÕES DOS PROJÉTEIS ---
                if (KeyboardState.IsKeyPressed(Keys.Space))
                {
                    _projectiles.Add(new Projectile(_shipTransform.position + _shipTransform.Up * 0.5f, _shipTransform.Up));
                }

                for (int i = _projectiles.Count - 1; i >= 0; i--)
                {
                    _projectiles[i].Update(delta, _limitX, _limitY);
                    if (_projectiles[i].lifespan <= 0f) _projectiles.RemoveAt(i); // Tiro expirou
                }

                // Verifica se algum tiro acertou alguma pedra (Algoritmo de Colisão Circular)
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

                            // Dá pontos dependendo do tamanho (Menores valem mais)
                            if (destroyed.size == 3) _score += 20;
                            else if (destroyed.size == 2) _score += 50;
                            else _score += 100;
                            Title = $"Asteroids OpenGL - Score: {_score} | Time: {Math.Round(_survivalTime, 1)}s";

                            // Estilhaça o asteroide em 2 menores
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
                            break; // Se achou um alvo, para de verificar essa bala
                        }
                    }
                    if (hit) _projectiles.RemoveAt(i);
                }

                // --- 6. COLISÃO NAVE X ASTEROIDES ---
                float shipRadius = 0.4f;
                foreach (var asteroid in _asteroids)
                {
                    float distance = (_shipTransform.position - asteroid.transform.position).Length;
                    if (distance < (shipRadius + asteroid.radius))
                    {
                        _isGameOver = true; // Bateu!
                        Title = $"GAME OVER - Press 'R' to Restart | Score: {_score} | Time: {Math.Round(_survivalTime, 1)}s";

                        // Gera os estilhaços da explosão da nave
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
                                (float)_random.NextDouble() * 360f, (float)_random.NextDouble() * 360f, (float)_random.NextDouble() * 360f
                            );
                            _shipDebris.Add(debrisTransform);

                            // Sorteia direções radiais de explosão
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
                // --- TELA DE GAME OVER (Animação e Restart) ---
                _isThrusting = false;
                for (int i = 0; i < _shipDebris.Count; i++)
                {
                    _shipDebris[i].position += _debrisVelocities[i] * delta;
                    _shipDebris[i].rotation.X += 300f * delta;
                    _shipDebris[i].rotation.Y += 200f * delta;
                }

                if (KeyboardState.IsKeyDown(Keys.R)) RestartGame();
            }

            // Os asteroides viajam sempre, mesmo no Game Over
            foreach (var asteroid in _asteroids)
                asteroid.Update(delta, _limitX, _limitY);

            if (KeyboardState.IsKeyDown(Keys.Escape)) Close();
        }

        /// <summary>
        /// Sorteia uma das 4 bordas da tela e empurra um asteroide novo em direção ao centro.
        /// </summary>
        private void SpawnAsteroidAtEdge()
        {
            Vector3 pos = Vector3.Zero;
            int side = _random.Next(0, 4);

            if (side == 0) pos = new Vector3(((float)_random.NextDouble() * 2f - 1f) * _limitX, _limitY + 1f, 0f);
            else if (side == 1) pos = new Vector3(((float)_random.NextDouble() * 2f - 1f) * _limitX, -_limitY - 1f, 0f);
            else if (side == 2) pos = new Vector3(-_limitX - 1f, ((float)_random.NextDouble() * 2f - 1f) * _limitY, 0f);
            else pos = new Vector3(_limitX + 1f, ((float)_random.NextDouble() * 2f - 1f) * _limitY, 0f);

            Vector3 vel = new Vector3(
                ((float)_random.NextDouble() * 2f - 1f) * 2f - (pos.X * 0.1f),
                ((float)_random.NextDouble() * 2f - 1f) * 2f - (pos.Y * 0.1f), 0f
            );

            _asteroids.Add(new Asteroid(pos, vel, _random.Next(2, 4)));
        }


        // ====================================================================
        // SEÇÃO 4: PIPELINE DE RENDERIZAÇÃO
        // Controla como as geometrias vão do código para os pixels na tela.
        // ====================================================================

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);
            float delta = (float)args.Time;
            _time += delta;

            GL.Enable(EnableCap.DepthTest); // Liga a noção de profundidade (eixo Z)

            // PASSO 1: Desenha o jogo na "Tela Falsa" (_postTarget)
            _postTarget.Use();
            DrawScene(_camera);

            // PASSO 2: Limpa o monitor e pega a imagem final
            RenderTarget.Reset(this);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // PASSO 3: Aplica o Shader CRT por cima da imagem inteira e entrega pro monitor
            _postMaterial.Use();
            _postMesh.Draw();

            SwapBuffers();
        }

        /// <summary>
        /// Onde o desenho 3D realmente acontece. Utiliza a técnica 'Inverse Hull' para o Outline.
        /// </summary>
        private void DrawScene(Camera camera)
        {
            GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            camera.GlobalApply();
            Material.SetGlobalFloat("u_Time", _time);

            _gameMaterial.Use();
            GL.Enable(EnableCap.CullFace); 
            GL.Enable(EnableCap.DepthTest);

            // --- 1. RENDERIZA NAVE OU DESTROÇOS ---
            _gameMaterial.Program.SetUniform("u_IsAsteroid", 0);
            _gameMaterial.Program.SetUniform("u_IsThrusting", _isThrusting ? 1 : 0);

            if (!_isGameOver)
            {
                // Técnica Inverse Hull:
                // 1. Desenha as faces de TRÁS infladas e pintadas de branco
                _shipTransform.Apply(_gameMaterial);
                GL.CullFace(TriangleFace.Front);
                _gameMaterial.Program.SetUniform("u_IsOutline", 1);
                _shipMesh.Draw();

                // 2. Desenha as faces da FRENTE em tamanho normal e pintadas de preto/sombra
                GL.CullFace(TriangleFace.Back);
                _gameMaterial.Program.SetUniform("u_IsOutline", 0);
                _shipMesh.Draw();

                // --- DESENHA O PROPULSOR (FOGO) ---
                if (_isThrusting)
                {
                    Transform fireTransform = new Transform();
                    fireTransform.position = _shipTransform.position - _shipTransform.Up * 0.75f;
                    fireTransform.rotation = _shipTransform.rotation;
                    fireTransform.position.Z -= 0.1f; // Recuo para evitar 'Z-Fighting' com a nave

                    // Tremulação visual da chama
                    float flickerY = 0.6f + (float)Math.Sin(_time * 40.0) * 0.2f;
                    float flickerX = 0.45f + (float)Math.Cos(_time * 50.0) * 0.05f;
                    fireTransform.scale = new Vector3(flickerX, flickerY, flickerX);

                    // Troca de material porque a matemática de dobra mora no thruster.vert
                    _thrusterMaterial.Use();
                    _thrusterMaterial.Program.SetUniform("u_Drift", _visualDrift * -0.025f);
                    fireTransform.Apply(_thrusterMaterial);

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
                // Desenha os estilhaços de Game Over
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

            // --- 2. RENDERIZA PROJÉTEIS ---
            _gameMaterial.Use(); // Volta pro material base
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

            // --- 3. RENDERIZA ASTEROIDES ---
            // Aciona a lógica de geração de "Geóide" no Vertex Shader 
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

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, Size.X, Size.Y);

            // Se o usuário redimensionar a janela, recriamos a tela do post-processing no tamanho certo
            _postTarget.Destroy();
            _postTarget = RenderTarget.CreateColor(Size.X, Size.Y);
            _postMaterial.SetTexture("u_Texture", _postTarget.Texture);
        }


        // ====================================================================
        // SEÇÃO 5: GERADORES DE GEOMETRIA (MESH)
        // Classes que definem fisicamente os pontos 3D no espaço
        // ====================================================================

        /// <summary>
        /// Esculpe a nave em formato de "Dardo/Viper" especificando os vértices matematicamente.
        /// </summary>
        private Mesh CreateCustomShipMesh()
        {
            float[] vertices = {
                // Pos(XYZ)          Normal(XYZ)      UV(XY)
                 0.0f,  1.0f,  0.0f,  0f, 1f, 0f,     0.5f, 1.0f, // 0: Bico Frontal
                -0.8f, -1.0f,  0.0f, -1f, 0f, 0f,     0.0f, 0.0f, // 1: Asa Esquerda
                 0.8f, -1.0f,  0.0f,  1f, 0f, 0f,     1.0f, 0.0f, // 2: Asa Direita
                 0.0f, -0.6f,  0.4f,  0f, 0f, 1f,     0.5f, 0.5f, // 3: Cabine
                 0.0f, -0.6f, -0.4f,  0f, 0f,-1f,     0.5f, 0.5f  // 4: Barriga
            };

            uint[] indices = {
                0, 1, 3, // Cima-Esq
                0, 3, 2, // Cima-Dir
                0, 2, 4, // Baixo-Dir
                0, 4, 1, // Baixo-Esq
                1, 4, 3, // Motor-Esq
                2, 3, 4  // Motor-Dir
            };
            return new Mesh(vertices, indices);
        }

        /// <summary>
        /// Gera um cone subdividido em anéis. Sem esses anéis extras no meio do cone, 
        /// o Vertex Shader não teria "dobradiças" para curvar o fogo parabolicamente.
        /// </summary>
        private Mesh CreateSegmentedThruster(int verticalSegments = 5)
        {
            List<float> vertices = new();
            List<uint> indices = new();

            int sides = 3; 
            float baseRadius = 0.45f;

            for (int i = 0; i <= verticalSegments; i++)
            {
                float v = (float)i / verticalSegments;
                float y = 0.5f - v; 
                float radius = baseRadius * (1.0f - v); 

                for (int j = 0; j <= sides; j++)
                {
                    float u = (float)j / sides;
                    float angle = u * MathF.Tau;
                    float x = MathF.Sin(angle) * radius;
                    float z = MathF.Cos(angle) * radius;

                    vertices.AddRange(new float[] { x, y, z });     // Pos
                    vertices.AddRange(new float[] { x, 0.2f, z });  // Normal
                    vertices.AddRange(new float[] { u, v });        // UV
                }
            }

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