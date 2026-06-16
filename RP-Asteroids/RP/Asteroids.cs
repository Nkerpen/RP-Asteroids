using System;
using OpenTK.Mathematics;
using RP.Core;

namespace RP
{
    internal class Asteroid
    {
        public Transform transform;
        public Vector3 velocity;
        public Vector3 rotationSpeed; // Velocidade de giro para cada eixo (X, Y, Z)
        public Mesh mesh;
        public float radius;

        public Asteroid(Vector3 startPosition, Vector3 startVelocity, float startRadius)
        {
            transform = new Transform();
            transform.position = startPosition;
            
            Random rnd = new Random();
            transform.scale = new Vector3(
                1f + ((float)rnd.NextDouble() * 0.4f - 0.2f),
                1f + ((float)rnd.NextDouble() * 0.4f - 0.2f),
                1f + ((float)rnd.NextDouble() * 0.4f - 0.2f)
            );

            velocity = startVelocity;
            radius = startRadius;

            // Usa a esfera tridimensional da sua base de código (com poucos segmentos para parecer retrô)
            mesh = Primitive.CreateSphere(radius, 10, 8); 

            // Define velocidades de rotação aleatórias para os eixos X, Y e Z
            rotationSpeed = new Vector3(
                ((float)rnd.NextDouble() * 2f - 1f) * 60f,
                ((float)rnd.NextDouble() * 2f - 1f) * 60f,
                ((float)rnd.NextDouble() * 2f - 1f) * 60f
            );
        }

        public void Update(float delta, float limitX, float limitY)
        {
            // Movimento linear no plano XY
            transform.position += velocity * delta;

            // Rotação contínua em todos os eixos 3D
            transform.rotation += rotationSpeed * delta;

            // Screen Wrap (Mantém o jogo preso na área da tela)
            if (transform.position.X > limitX) transform.position.X = -limitX;
            if (transform.position.X < -limitX) transform.position.X = limitX;
            if (transform.position.Y > limitY) transform.position.Y = -limitY;
            if (transform.position.Y < -limitY) transform.position.Y = limitY;
        }
    }
}