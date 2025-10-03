using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
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
                Size = new Vector2i(800, 600),
                Title = "Exercise 4: Generic Coordinate Transformers",
                Profile = ContextProfile.Core,
                APIVersion = new Version(3, 3)
            };

            using var window = new GameWindow(windowSettings, nativeSettings);
            var game = new Game(800, 600);

            window.Load += () =>
            {
                game.Init();
                Console.WriteLine($"OpenGL Version: {GL.GetString(StringName.Version)}");
            };

            // Handle window resize
            window.Resize += (args) =>
            {
                GL.Viewport(0, 0, args.Width, args.Height);

                // Update the game's screen dimensions
                game.UpdateScreenSize(args.Width, args.Height);

                Console.WriteLine($"Window resized to: {args.Width}x{args.Height}");
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