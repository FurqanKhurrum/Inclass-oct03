using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;

namespace WindowEngine
{
    public class Game
    {
        private readonly Surface screen;
        private int vao, vbo, shaderProgram;

        public Game(int width, int height)
        {
            screen = new Surface(width, height);
        }

        public void Init()
        {
            // Set clear color to white as per your previous modification
            GL.ClearColor(1.0f, 1.0f, 1.0f, 1.0f);
            // Disable depth testing and culling for 2D rendering
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.CullFace);

            // Vertex data for a 300x300 pixel square centered in the window
            float centerX = screen.width / 2.0f;
            float centerY = screen.height / 2.0f;
            float halfSize = 150.0f; // Half of 300 pixels
            float[] vertices = {
                // Positions (in pixels)               // Colors (R, G, B)
                centerX - halfSize, centerY + halfSize, 0.0f,  0.0f, 0.0f, 0.0f,  // Bottom-left: black
                centerX + halfSize, centerY + halfSize, 0.0f,  0.0f, 0.0f, 1.0f,  // Bottom-right: light blue (255/255)
                centerX + halfSize, centerY - halfSize, 0.0f,  0.0f, 0.0f, 1.0f,  // Top-right: light blue (255/255)
                centerX - halfSize, centerY - halfSize, 0.0f,  0.0f, 0.0f, 0.0f   // Top-left: black
            };

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
                    gl_Position = vec4(normalized.x, -normalized.y, 0.0, 1.0); // Flip y for correct orientation
                    vColor = aColor;
                }";

            // Fragment shader
            string fragmentShaderSource = @"
                #version 330 core
                in vec3 vColor;
                out vec4 FragColor;
                void main()
                {
                    FragColor = vec4(vColor, 1.0);
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
            GL.Clear(ClearBufferMask.ColorBufferBit);
            RenderGL();
            CheckGLError("After Tick");
        }

        private void RenderGL()
        {
            GL.UseProgram(shaderProgram);
            GL.BindVertexArray(vao);
            GL.DrawArrays(PrimitiveType.TriangleFan, 0, 4); // Draw square as triangle fan
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