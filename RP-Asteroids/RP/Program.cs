// See https://aka.ms/new-console-template for more information


using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using RP;
using StbImageSharp;

StbImage.stbi_set_flip_vertically_on_load(1);

NativeWindowSettings settings = new()
{
    Title = "Meu Jogo",
    ClientSize = new(800, 600),
    Flags = ContextFlags.ForwardCompatible
};
Game game = new(GameWindowSettings.Default, settings);
game.Run();
