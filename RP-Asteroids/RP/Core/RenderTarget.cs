using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Desktop;

namespace RP.Core
{
    internal class RenderTarget
    {
        private int _width;
        private int _height;
        private int _id;

        private Texture _texture;
        public Texture Texture => _texture;
        private int _depthBuffer = 0;

        private RenderTarget(int width, int height, Texture texture)
        {
            _width = width;
            _height = height;
            _id = GL.GenFramebuffer();
            _texture = texture;
        }

        public void Destroy()
        {
            if (_depthBuffer != 0)
            {
                GL.DeleteRenderbuffer(_depthBuffer);
            }
            GL.DeleteFramebuffer(_id);
        }

        public static RenderTarget CreateColor(int width, int height)
        {
            TextureSettings settings = new();
            settings.AddSetting(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            settings.AddSetting(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            settings.AddSetting(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            settings.AddSetting(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            Texture texture = new(width, height, settings);

            RenderTarget renderTarget = new(width, height, texture);

            renderTarget._depthBuffer = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, renderTarget._depthBuffer);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent32, width, height);

            int prev = GL.GetInteger(GetPName.FramebufferBinding);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, renderTarget._id);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, renderTarget._depthBuffer);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, renderTarget._texture.Id, 0);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, prev);

            return renderTarget;
        }

        public static RenderTarget CreateDepth(int width, int height)
        {
            TextureSettings settings = new TextureSettings();
            settings.AddSetting(TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            settings.AddSetting(TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            settings.AddSetting(TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            settings.AddSetting(TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            Texture texture = new DepthTexture(width, height, settings);

            RenderTarget renderTarget = new(width, height, texture);

            int prev = GL.GetInteger(GetPName.FramebufferBinding);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, renderTarget._id);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, renderTarget._texture.Id, 0);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, prev);

            return renderTarget;
        }

        public void Use()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _id);
            GL.Viewport(0, 0, _width, _height);
        }

        public static void Reset(GameWindow window)
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Viewport(0, 0, window.Size.X, window.Size.Y);
        }
    }
}
