using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;

namespace WindowEngine
{
    public class Game
    {
        private readonly Surface screen;
        private int vao, vbo, shaderProgram;
        private float angle = 0.0f;

        public Game(int width, int height)
        {
            screen = new Surface(width, height);
        }

        // Transform X: Convert world coordinates (-2 to 2) to screen coordinates (0 to width)
        // World space uses mathematical coordinates where (0,0) is at center
        private float TX(float x)
        {
            // Map [-2, 2] to [0, width]
            // Scale by width/4 (since range is 4 units) and shift to center
            return (x + 2.0f) * (screen.width / 4.0f);
        }

        // Transform Y: Convert world coordinates (-2 to 2) to screen coordinates (0 to height)
        // IMPORTANT: Y is inverted because:
        // - Mathematical/World coordinates: Y increases upward (bottom to top)
        // - Screen coordinates: Y increases downward (top to bottom)
        // - OpenGL traditionally uses bottom-left origin, but window systems use top-left
        // This inversion ensures our world space matches expected mathematical behavior
        private float TY(float y)
        {
            // Map [-2, 2] to [0, height] with Y-inversion
            // First invert y (negate it), then scale and shift
            return (-y + 2.0f) * (screen.height / 4.0f);
        }

        public void Init()
        {
            // Set clear color to black
            GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.CullFace);

            // Enable line smoothing for better visual quality
            GL.Enable(EnableCap.LineSmooth);
            GL.Hint(HintTarget.LineSmoothHint, HintMode.Nicest);
            GL.LineWidth(2.0f);

            // Create VAO and VBO for lines
            vao = GL.GenVertexArray();
            vbo = GL.GenBuffer();
            GL.BindVertexArray(vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);

            // Allocate buffer for 4 lines (8 vertices total) with position + color
            GL.BufferData(BufferTarget.ArrayBuffer, 8 * 6 * sizeof(float), IntPtr.Zero, BufferUsageHint.DynamicDraw);

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
            // Update rotation angle
            angle += 0.01f;

            GL.Clear(ClearBufferMask.ColorBufferBit);
            RenderGL();
            CheckGLError("After Tick");
        }

        private void RenderGL()
        {
            // Define square corners in world space (-2 to 2 range)
            // Base size is 1.0 unit, centered at origin
            float size = 1.0f;

            // Fun challenge: Make it pulse with sine wave
            float pulse = 1.0f + 0.3f * (float)Math.Sin(angle * 2.0); // Pulsate between 0.7 and 1.3
            size *= pulse;

            // Original square corners (before rotation)
            float[] corners = new float[8] {
                -size, -size,  // Bottom-left
                 size, -size,  // Bottom-right
                 size,  size,  // Top-right
                -size,  size   // Top-left
            };

            // Rotate corners using rotation matrix
            // Rotation formula: 
            // rx = x * cos(a) - y * sin(a)
            // ry = x * sin(a) + y * cos(a)
            float cosA = (float)Math.Cos(angle);
            float sinA = (float)Math.Sin(angle);

            float[] rotatedCorners = new float[8];
            for (int i = 0; i < 4; i++)
            {
                float x = corners[i * 2];
                float y = corners[i * 2 + 1];

                rotatedCorners[i * 2] = x * cosA - y * sinA;
                rotatedCorners[i * 2 + 1] = x * sinA + y * cosA;
            }

            // Convert to screen coordinates and create line vertices
            float[] vertices = new float[8 * 6]; // 8 vertices (4 lines * 2 endpoints), 6 floats each

            for (int i = 0; i < 4; i++)
            {
                int nextI = (i + 1) % 4;

                // Start vertex of line
                float x1 = TX(rotatedCorners[i * 2]);
                float y1 = TY(rotatedCorners[i * 2 + 1]);

                // End vertex of line
                float x2 = TX(rotatedCorners[nextI * 2]);
                float y2 = TY(rotatedCorners[nextI * 2 + 1]);

                int idx = i * 12; // 12 floats per line (2 vertices * 6 floats)

                // Start vertex
                vertices[idx + 0] = x1;
                vertices[idx + 1] = y1;
                vertices[idx + 2] = 0.0f;
                vertices[idx + 3] = 1.0f; // White color (R)
                vertices[idx + 4] = 1.0f; // White color (G)
                vertices[idx + 5] = 1.0f; // White color (B)

                // End vertex
                vertices[idx + 6] = x2;
                vertices[idx + 7] = y2;
                vertices[idx + 8] = 0.0f;
                vertices[idx + 9] = 1.0f; // White color (R)
                vertices[idx + 10] = 1.0f; // White color (G)
                vertices[idx + 11] = 1.0f; // White color (B)
            }

            // Upload vertex data
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, vertices.Length * sizeof(float), vertices);

            // Draw lines
            GL.UseProgram(shaderProgram);
            GL.BindVertexArray(vao);
            GL.DrawArrays(PrimitiveType.Lines, 0, 8);

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