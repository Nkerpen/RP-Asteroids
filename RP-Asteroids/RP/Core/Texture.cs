using OpenTK.Graphics.OpenGL;
using StbImageSharp;

namespace RP.Core
{
    internal class TextureSettings
    {
        private List<(TextureParameterName, int)> _integerSettings = new();
        private List<(TextureParameterName, float)> _floatSettings = new();

        public static TextureSettings Default
        {
            get
            {
                TextureSettings settings = new();

                settings.AddSetting(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
                settings.AddSetting(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                settings.AddSetting(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                settings.AddSetting(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

                return settings;
            }
        }

        public void AddSetting(TextureParameterName name, int value)
        {
            _integerSettings.Add((name, value));
        }

        public void AddSetting(TextureParameterName name, float value)
        {
            _floatSettings.Add((name, value));
        }

        public void Apply()
        {
            foreach (var setting in _integerSettings)
            {
                GL.TexParameter(TextureTarget.Texture2D, setting.Item1, setting.Item2);
            }
            foreach (var setting in _floatSettings)
            {
                GL.TexParameter(TextureTarget.Texture2D, setting.Item1, setting.Item2);
            }
        }
    }

    internal class Texture
    {
        private int _id;
        public int Id => _id;

        private static Texture? _defaultTexture = null;
        public static Texture Default
        {
            get
            {
                if (_defaultTexture == null)
                {
                    _defaultTexture = new Texture(1, 1);
                }
                return _defaultTexture;
            }
        }

        public Texture(string path) : this(path, TextureSettings.Default) { }

        public Texture(string path, TextureSettings settings)
        {
            ImageResult result = ImageResult.FromMemory(File.ReadAllBytes(path), ColorComponents.RedGreenBlueAlpha);

            _id = GL.GenTexture();
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _id);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, result.Width, result.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, result.Data);
            settings.Apply();
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        }

        public Texture(int width, int height) : this(width, height, TextureSettings.Default, false) { }

        public Texture(int width, int height, TextureSettings settings) : this(width, height, settings, false) { }

        protected Texture(int width, int height, TextureSettings settings, bool depth)
        {
            _id = GL.GenTexture();
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _id);


            PixelInternalFormat internalFormat = depth ? PixelInternalFormat.DepthComponent32 : PixelInternalFormat.Rgba;
            PixelFormat format = depth ? PixelFormat.DepthComponent : PixelFormat.Rgba;
            GL.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, width, height, 0, format, PixelType.UnsignedByte, IntPtr.Zero);
            settings.Apply();
            uint[] data = { ~0u };
            GL.ClearTexImage(_id, 0, format, PixelType.UnsignedByte, data);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        }

        public void Use(int unit)
        {
            GL.ActiveTexture((TextureUnit)(TextureUnit.Texture0 + unit));
            GL.BindTexture(TextureTarget.Texture2D, _id);
        }
    }

    internal class DepthTexture : Texture
    {
        public DepthTexture(int width, int height) : base(width, height, TextureSettings.Default, true) { }

        public DepthTexture(int width, int height, TextureSettings settings) : base(width, height, settings, true) { }

    }
}
