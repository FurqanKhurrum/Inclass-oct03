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
            // Configure window settings
            var windowSettings = GameWindowSettings.Default;
            var nativeSettings = new NativeWindowSettings
            {
                Size = new Vector2i(800, 600), // 800x600 window
                Title = "Square Demo",
                Profile = ContextProfile.Core, // OpenGL 3.3 Core Profile
                APIVersion = new Version(3, 3)
            };

            // Create window and game
            using var window = new GameWindow(windowSettings, nativeSettings);
            var game = new Game(800, 600);

            // Initialize OpenGL
            window.Load += () =>
            {
                game.Init();
                Console.WriteLine($"OpenGL Version: {GL.GetString(StringName.Version)}");
            };

            // Render loop
            window.RenderFrame += (args) =>
            {
                game.Tick();
                window.SwapBuffers(); // Ensure content is displayed
            };

            // Cleanup
            window.Unload += () => game.Cleanup();

            // Run the window
            window.Run();
        }
    }
}