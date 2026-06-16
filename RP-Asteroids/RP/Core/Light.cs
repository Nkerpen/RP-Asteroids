using OpenTK.Mathematics;

namespace RP.Core
{
    // Classe de Luz Direcional, representando a luz do sol, que ilumina toda a cena independente de sua posição.
    // Utilizamos a classe transform como base para aproveitar os dados de rotação e vetores de direção.
    class DirectionalLight : Transform
    {
        public Vector3 Direction => Forward;
        public Vector3 color = Vector3.One;

        public override void Apply(ShaderProgram program)
        {
            program.SetUniform("u_DirectionalLightDirection", Direction);
            program.SetUniform("u_DirectionalLightColor", color);
        }

        public override void GlobalApply()
        {
            Material.SetGlobalVec3("u_DirectionalLightDirection", Direction);
            Material.SetGlobalVec3("u_DirectionalLightColor", color);
        }

        public Camera GetLightMapCamera(Vector3 position)
        {
            OrthographicCamera camera = new(100f, 100f);
            camera.rotation = rotation;
            camera.position = position;
            camera.near = -50f;
            camera.far = 50f;

            return camera;
        }
    }
}
