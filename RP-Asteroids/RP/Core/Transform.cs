using OpenTK.Mathematics;

namespace RP.Core
{
    internal class Transform
    {
        public Vector3 position = Vector3.Zero;// Posição do objeto no mundo, em x, y e z
        public Vector3 rotation = Vector3.Zero;// Rotação nos eixos x, y e z, em graus
        public Vector3 scale = Vector3.One;// Escala nos eixos x, y e z
        private Transform? _parent = null;
        public Transform? Parent => _parent;

        public Matrix4 TranslationMatrix => Matrix4.CreateTranslation(position);
        public Matrix4 RotationMatrix
        {
            get
            {
                return
                    Matrix4.CreateRotationX(rotation.X * MathF.PI / 180f) *
                    Matrix4.CreateRotationY(rotation.Y * MathF.PI / 180f) *
                    Matrix4.CreateRotationZ(rotation.Z * MathF.PI / 180f)
                ;
            }
        }
        public Matrix4 InverseRotationMatrix
        {
            get
            {
                return
                    Matrix4.CreateRotationZ(-rotation.Z * MathF.PI / 180f) *
                    Matrix4.CreateRotationY(-rotation.Y * MathF.PI / 180f) *
                    Matrix4.CreateRotationX(-rotation.X * MathF.PI / 180f)
                ;
            }
        }
        public Matrix4 ScaleMatrix => Matrix4.CreateScale(scale);
        public Matrix4 ModelMatrix => ScaleMatrix * RotationMatrix * TranslationMatrix * (Parent != null ? Parent.ModelMatrix : Matrix4.Identity);

        public Vector3 Forward => (new Vector4(0f, 0f, -1f, 1f) * RotationMatrix).Xyz;
        public Vector3 Right => (new Vector4(1f, 0f, 0f, 1f) * RotationMatrix).Xyz;
        public Vector3 Up => (new Vector4(0f, 1f, 0f, 1f) * RotationMatrix).Xyz;

        public void SetParent(Transform? parent)
        {
            // Um objeto não pode ser pai dele mesmo, ou gerar uma hierarquia infinita.
            // Por isso, fazemos uma checagem prévia.
            Transform? t = parent;
            while (t != null)
            {
                if (t == this)
                {
                    return;
                }
                t = t.Parent;
            }
            _parent = parent;
        }

        public virtual void Apply(ShaderProgram program)
        {
            program.SetUniform("u_Model", ModelMatrix);
            program.SetUniform("u_Rotation", RotationMatrix);
        }

        public void Apply(Material material)
        {
            Apply(material.Program);
        }

        public virtual void GlobalApply() {}
    }
}
