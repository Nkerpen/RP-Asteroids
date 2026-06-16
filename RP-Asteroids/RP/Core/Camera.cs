using OpenTK.Mathematics;

namespace RP.Core
{
    internal abstract class Camera : Transform
    {
        public Matrix4 ViewMatrix => Matrix4.LookAt(position, position + Forward, Up);
        public abstract Matrix4 ProjectionMatrix { get; }
        public float near;
        public float far;

        public override void Apply(ShaderProgram program)
        {
            program.SetUniform("u_View", ViewMatrix);
            program.SetUniform("u_Projection", ProjectionMatrix);
            program.SetUniform("u_CameraRotation", RotationMatrix);
            program.SetUniform("u_CameraInverseRotation", InverseRotationMatrix);
            program.SetUniform("u_CameraPosition", position);
            program.SetUniform("u_CameraDirection", Forward);
        }

        public override void GlobalApply()
        {
            Material.SetGlobalMat4("u_View", ViewMatrix);
            Material.SetGlobalMat4("u_Projection", ProjectionMatrix);
            Material.SetGlobalMat4("u_CameraRotation", RotationMatrix);
            Material.SetGlobalMat4("u_CameraInverseRotation", InverseRotationMatrix);
            Material.SetGlobalVec3("u_CameraPosition", position);
            Material.SetGlobalVec3("u_CameraDirection", Forward);
        }
    }

    internal class OrthographicCamera : Camera
    {
        public float width;
        public float height;

        public override Matrix4 ProjectionMatrix => Matrix4.CreateOrthographic(width, height, near, far);

        public OrthographicCamera(float width = 5f, float height = 5f)
        {
            this.width = width;
            this.height = height;
            near = 0f;
            far = 500f;
        }
    }

    internal class PerspectiveCamera : Camera
    {
        public float fov;
        public float aspect;

        public override Matrix4 ProjectionMatrix => Matrix4.CreatePerspectiveFieldOfView(fov * MathF.PI / 180f, aspect, near, far);

        public PerspectiveCamera(float fov = 90f, float aspect = 1f)
        {
            this.fov = fov;
            this.aspect = aspect;
            near = 0.1f;
            far = 500f;
        }
    }
}