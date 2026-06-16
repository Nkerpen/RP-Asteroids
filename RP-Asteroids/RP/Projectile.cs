using System;
using OpenTK.Mathematics;
using RP.Core;

namespace RP
{
    internal class Projectile
    {
        public Transform transform;
        public Vector3 velocity;
        public float lifespan = 1f;

        public Projectile(Vector3 pos, Vector3 dir)
        {
            transform = new Transform();
            transform.position = pos;

            transform.scale = new Vector3(0.10f, 0.5f, 0.10f);

            float angle = MathF.Atan2(dir.Y, dir.X) - (MathF.PI / 2f);
            transform.rotation.Z = angle * (180f / MathF.PI);

            velocity = dir * 18f;
        }

        public void Update(float delta, float limitX, float limitY)
        {
            transform.position += velocity * delta;
            lifespan -= delta;

            // EFEITO VISUAL: O projetil encolhe no eixo Y (comprimento) e X/Z (espessura) 
            // conforme o seu tempo de vida (lifespan) chega perto de zero.
            // O valor 1.2f e o tempo de vida total que definimos no construtor.
            float lifePercentage = lifespan / 1f;
            transform.scale = new Vector3(
                0.10f * lifePercentage,
                0.5f * lifePercentage,
                0.10f * lifePercentage
            );

            if (transform.position.X > limitX) transform.position.X = -limitX;
            if (transform.position.X < -limitX) transform.position.X = limitX;
            if (transform.position.Y > limitY) transform.position.Y = -limitY;
            if (transform.position.Y < -limitY) transform.position.Y = limitY;
        }
    }
}