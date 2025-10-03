using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;

namespace WindowEngine
{
    class Program
    {
        static void Main(string[] args)
        {
            var windowSettings = GameWindowSettings.Default;
            var nativeSettings = new NativeWindowSettings
            {
                Size = new Vector2i(1024, 768),
                Title = "Exercise 6: 3D Landscape Renderer",
                Profile = ContextProfile.Core,
                APIVersion = new Version(3, 3)
            };

            using var window = new GameWindow(windowSettings, nativeSettings);
            var game = new Game(1024, 768);

            window.Load += () =>
            {
                game.Init();
                Console.WriteLine($"OpenGL Version: {GL.GetString(StringName.Version)}");
            };

            window.Resize += (args) =>
            {
                game.UpdateScreenSize(args.Width, args.Height);
            };

            window.UpdateFrame += (args) =>
            {
                game.HandleInput(window.KeyboardState, (float)args.Time);

                if (window.KeyboardState.IsKeyDown(Keys.Escape))
                    window.Close();
            };

            window.RenderFrame += (args) =>
            {
                game.Tick();
                window.SwapBuffers();
            };

            window.Unload += () => game.Cleanup();

            window.Run();
        }
    }
}