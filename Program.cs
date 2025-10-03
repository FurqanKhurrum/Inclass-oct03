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
                Size = new Vector2i(800, 600),
                Title = "Exercise 5: Interactive Function Plotter",
                Profile = ContextProfile.Core,
                APIVersion = new Version(3, 3)
            };

            using var window = new GameWindow(windowSettings, nativeSettings);
            var game = new Game(800, 600);

            window.Load += () =>
            {
                game.Init();
                Console.WriteLine($"OpenGL Version: {GL.GetString(StringName.Version)}");
                Console.WriteLine("=== CONTROLS ===");
                Console.WriteLine("Z - Zoom In");
                Console.WriteLine("X - Zoom Out");
                Console.WriteLine("Arrow Keys - Pan camera");
                Console.WriteLine("R - Reset view");
                Console.WriteLine("ESC - Exit");
            };

            // Handle window resize
            window.Resize += (args) =>
            {
                GL.Viewport(0, 0, args.Width, args.Height);
                game.UpdateScreenSize(args.Width, args.Height);
            };

            // Handle keyboard input in update frame
            window.UpdateFrame += (args) =>
            {
                // Pass keyboard state to game for handling
                game.HandleInput(window.KeyboardState, (float)args.Time);

                // ESC to exit
                if (window.KeyboardState.IsKeyDown(Keys.Escape))
                {
                    window.Close();
                }
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