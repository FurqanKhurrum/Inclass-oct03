using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;

namespace WindowEngine
{
    public class Game
    {
        private readonly Surface screen;
        private int vao, vbo, shaderProgram;
        private int frameCount = 0;

        public Game(int width, int height)
        {
            screen = new Surface(width, height);
        }

        // Helper function to create color from RGB components
        private int CreateColor(int r, int g, int b)
        {
            return (r << 16) + (g << 8) + b;
        }

        public void Init()
        {
            // Set clear color to black
            GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.CullFace);

            // Create a 300x300 pixel square centered in the window
            int squareSize = 300;
            float centerX = screen.width / 2.0f;
            float centerY = screen.height / 2.0f;
            float startX = centerX - squareSize / 2.0f;
            float startY = centerY - squareSize / 2.0f;

            // Create vertices for each pixel in the square
            // We'll use one vertex per pixel for maximum control
            int totalPixels = squareSize * squareSize;
            float[] vertices = new float[totalPixels * 6]; // 6 floats per vertex (3 pos + 3 color)

            int index = 0;
            for (int y = 0; y < squareSize; y++)
            {
                for (int x = 0; x < squareSize; x++)
                {
                    // Calculate position
                    float pixelX = startX + x;
                    float pixelY = startY + y;

                    // Calculate color based on position
                    // Red increases with x (0-255)
                    int red = (x * 255) / squareSize;
                    // Green increases with y (0-255)
                    int green = (y * 255) / squareSize;
                    // Blue will be animated in the shader using time
                    int blue = 0; // Initial blue value

                    // Position
                    vertices[index++] = pixelX;
                    vertices[index++] = pixelY;
                    vertices[index++] = 0.0f;

                    // Color (normalized to 0-1 range for OpenGL)
                    vertices[index++] = red / 255.0f;
                    vertices[index++] = green / 255.0f;
                    vertices[index++] = blue / 255.0f;
                }
            }

            // Create and bind VAO and VBO
            vao = GL.GenVertexArray();
            vbo = GL.GenBuffer();
            GL.BindVertexArray(vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            // Vertex attributes: position (3 floats) + color (3 floats)
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            // Vertex shader
            string vertexShaderSource = @"
                #version 330 core
                layout(location = 0) in vec3 aPosition;
                layout(location = 1) in vec3 aColor;
                out vec3 vColor;
                uniform vec2 uResolution;
                void main()
                {
                    vec2 normalized = (aPosition.xy / uResolution) * 2.0 - 1.0;
                    gl_Position = vec4(normalized.x, -normalized.y, 0.0, 1.0);
                    vColor = aColor;
                }";

            // Fragment shader with animated blue tint
            string fragmentShaderSource = @"
                #version 330 core
                in vec3 vColor;
                out vec4 FragColor;
                uniform float uTime;
                void main()
                {
                    // Add blue tint that fades over time using sine wave
                    float blueTint = (sin(uTime) + 1.0) * 0.5; // Oscillates between 0 and 1
                    vec3 finalColor = vColor + vec3(0.0, 0.0, blueTint * 0.5); // Add up to 0.5 blue
                    FragColor = vec4(finalColor, 1.0);
                }";

            // Compile shaders
            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexShaderSource);
            GL.CompileShader(vertexShader);
            CheckShaderError(vertexShader, "Vertex Shader");

            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentShaderSource);
            GL.CompileShader(fragmentShader);
            CheckShaderError(fragmentShader, "Fragment Shader");

            // Link shader program
            shaderProgram = GL.CreateProgram();
            GL.AttachShader(shaderProgram, vertexShader);
            GL.AttachShader(shaderProgram, fragmentShader);
            GL.LinkProgram(shaderProgram);
            CheckProgramError(shaderProgram);

            // Clean up shaders
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            // Set resolution uniform
            GL.UseProgram(shaderProgram);
            int resolutionLoc = GL.GetUniformLocation(shaderProgram, "uResolution");
            GL.Uniform2(resolutionLoc, (float)screen.width, (float)screen.height);

            CheckGLError("After Init");
        }

        public void Tick()
        {
            frameCount++;

            GL.Clear(ClearBufferMask.ColorBufferBit);
            RenderGL();
            CheckGLError("After Tick");
        }

        private void RenderGL()
        {
            GL.UseProgram(shaderProgram);
            GL.BindVertexArray(vao);

            // Update time uniform for animated blue tint
            int timeLoc = GL.GetUniformLocation(shaderProgram, "uTime");
            float timeValue = frameCount * 0.05f; // Slow down the animation
            GL.Uniform1(timeLoc, timeValue);

            // Enable point rendering
            GL.PointSize(1.0f);

            // Draw all pixels as points
            GL.DrawArrays(PrimitiveType.Points, 0, 300 * 300);

            CheckGLError("After RenderGL");
        }

        public void Cleanup()
        {
            GL.DeleteBuffer(vbo);
            GL.DeleteVertexArray(vao);
            GL.DeleteProgram(shaderProgram);
        }

        private void CheckGLError(string context)
        {
            ErrorCode error = GL.GetError();
            if (error != ErrorCode.NoError)
            {
                Console.WriteLine($"OpenGL Error at {context}: {error}");
            }
        }

        private void CheckShaderError(int shader, string name)
        {
            GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetShaderInfoLog(shader);
                Console.WriteLine($"{name} Compilation Error: {infoLog}");
            }
        }

        private void CheckProgramError(int program)
        {
            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetProgramInfoLog(program);
                Console.WriteLine($"Program Link Error: {infoLog}");
            }
        }
    }

    public class Surface
    {
        public int[] pixels;
        public int width, height;

        public Surface(int width, int height)
        {
            this.width = width;
            this.height = height;
            pixels = new int[width * height];
        }
    }
}