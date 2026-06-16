using System;
using OpenTK.Mathematics;
using RP.Core;

namespace RP
{
    internal class Asteroid
    {
        public Transform transform;
        public Vector3 velocity;
        public Vector3 rotationSpeed;
        public Mesh mesh;
        public float radius;
        public int size; // 3 = Grande, 2 = Médio, 1 = Pequeno

        // O construtor agora recebe o "tamanho" em vez do raio manual
        public Asteroid(Vector3 startPosition, Vector3 startVelocity, int startSize)
        {
            transform = new Transform();
            transform.position = startPosition;
            size = startSize;

            // Define o raio baseando-se no tamanho (3, 2 ou 1)
            if (size == 3) radius = 1.5f;
            else if (size == 2) radius = 0.8f;
            else radius = 0.4f;

            Random rnd = new Random();

            // A escala visual também acompanha o raio daquele tamanho
            transform.scale = new Vector3(
    radius * (0.8f + (float)rnd.NextDouble() * 0.4f), // Varia o tamanho entre 80% e 120% do raio
    radius * (0.8f + (float)rnd.NextDouble() * 0.4f),
    radius * (0.8f + (float)rnd.NextDouble() * 0.4f)
);


            velocity = startVelocity;
            mesh = Primitive.CreateSphere(1f, 10, 8); // Esfera de raio 1 (a escala acima cuida do tamanho)

            rotationSpeed = new Vector3(
                ((float)rnd.NextDouble() * 2f - 1f) * 60f,
                ((float)rnd.NextDouble() * 2f - 1f) * 60f,
                ((float)rnd.NextDouble() * 2f - 1f) * 60f
            );
        }

        public void Update(float delta, float limitX, float limitY)
        {
            transform.position += velocity * delta;
            transform.rotation += rotationSpeed * delta;

            if (transform.position.X > limitX) transform.position.X = -limitX;
            if (transform.position.X < -limitX) transform.position.X = limitX;
            if (transform.position.Y > limitY) transform.position.Y = -limitY;
            if (transform.position.Y < -limitY) transform.position.Y = limitY;
        }
    }
}