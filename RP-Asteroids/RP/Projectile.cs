using OpenTK.Mathematics;
using RP.Core;

namespace RP
{
    internal class Projectile
    {
        public Transform transform;
        public Vector3 velocity;
        public float lifespan = 1.2f; // Segundos até desaparecer se não acertar nada

        public Projectile(Vector3 pos, Vector3 dir)
        {
            transform = new Transform();
            transform.position = pos;
            
            // Deixa ele com cara de tiro laser (esticado no Y, fino no X e Z)
            transform.scale = new Vector3(0.08f, 0.4f, 0.08f); 
            
            // Rotaciona o tiro para alinhar visualmente com a direção em que está indo
            float angle = MathF.Atan2(dir.Y, dir.X) - (MathF.PI / 2f); 
            transform.rotation.Z = angle * (180f / MathF.PI);
            
            velocity = dir * 18f; // Velocidade alta do projétil
        }

        public void Update(float delta, float limitX, float limitY)
        {
            transform.position += velocity * delta;
            lifespan -= delta;

            if (transform.position.X > limitX) transform.position.X = -limitX;
            if (transform.position.X < -limitX) transform.position.X = limitX;
            if (transform.position.Y > limitY) transform.position.Y = -limitY;
            if (transform.position.Y < -limitY) transform.position.Y = limitY;
        }
    }
}