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

        // Generic world coordinate system parameters
        // These define the "camera" or "viewport" into your world
        private float worldMinX = -10.0f;
        private float worldMaxX = 10.0f;
        private float worldMinY = -10.0f;
        private float worldMaxY = 10.0f;
        private float worldCenterX = 0.0f;
        private float worldCenterY = 0.0f;

        public Game(int width, int height)
        {
            screen = new Surface(width, height);
        }

        /// <summary>
        /// Generic Transform X: Converts world X coordinate to screen X coordinate
        /// This is resolution-independent and works for any world range!
        /// 
        /// How it works:
        /// 1. Offset by center to make coordinate relative to center
        /// 2. Calculate world width (range of X values)
        /// 3. Normalize to [0, 1] range
        /// 4. Scale to screen width
        /// 
        /// Benefits for mobile/responsive design:
        /// - Automatically adapts to any screen size (phone, tablet, desktop)
        /// - World coordinates stay consistent regardless of resolution
        /// - Easy to implement pinch-zoom by adjusting worldMin/Max
        /// - Maintains aspect ratio correctly
        /// </summary>
        private float TX(float worldX)
        {
            // Apply world center offset
            float centeredX = worldX - worldCenterX;

            // Calculate world range
            float worldWidth = worldMaxX - worldMinX;

            // Normalize to [0, 1] range, then scale to screen width
            // Add worldWidth/2 to shift from centered coords to left-aligned
            float normalizedX = (centeredX + worldWidth / 2.0f) / worldWidth;

            return normalizedX * screen.width;
        }

        /// <summary>
        /// Generic Transform Y: Converts world Y coordinate to screen Y coordinate
        /// Includes Y-axis inversion to match screen coordinate system
        /// 
        /// Y-Inversion explained:
        /// - Mathematical/World space: Y+ is UP (Cartesian coordinates)
        /// - Screen space: Y+ is DOWN (raster graphics convention)
        /// - OpenGL: Traditionally Y+ is UP (bottom-left origin)
        /// - Window systems (Windows, macOS, mobile): Y+ is DOWN (top-left origin)
        /// 
        /// This function bridges these coordinate systems seamlessly.
        /// 
        /// Mobile game benefits:
        /// - Portrait/landscape rotation handled by adjusting world bounds
        /// - Different aspect ratios (16:9, 18:9, 4:3) all work correctly
        /// - Safe areas (notches, rounded corners) can be handled by adjusting worldMin/Max
        /// </summary>
        private float TY(float worldY)
        {
            // Apply world center offset
            float centeredY = worldY - worldCenterY;

            // Calculate world range
            float worldHeight = worldMaxY - worldMinY;

            // Normalize to [0, 1] range with Y-inversion
            // Note the negation of centeredY to flip the Y axis
            float normalizedY = (-centeredY + worldHeight / 2.0f) / worldHeight;

            return normalizedY * screen.height;
        }

        /// <summary>
        /// Helper method to set world view bounds - useful for zooming and panning
        /// Example uses:
        /// - Zoom in: SetWorldBounds(-5, 5, -5, 5) - shows smaller area, objects appear larger
        /// - Zoom out: SetWorldBounds(-20, 20, -20, 20) - shows larger area, objects appear smaller
        /// - Pan right: SetWorldBounds(-5, 15, -10, 10) - shifts view to the right
        /// </summary>
        public void SetWorldBounds(float minX, float maxX, float minY, float maxY)
        {
            worldMinX = minX;
            worldMaxX = maxX;
            worldMinY = minY;
            worldMaxY = maxY;
        }

        /// <summary>
        /// Helper method to set world center point - useful for following a player/camera
        /// </summary>
        public void SetWorldCenter(float centerX, float centerY)
        {
            worldCenterX = centerX;
            worldCenterY = centerY;
        }

        /// <summary>
        /// Updates screen size and shader uniforms when window is resized
        /// </summary>
        public void UpdateScreenSize(int width, int height)
        {
            screen.width = width;
            screen.height = height;

            // Update the resolution uniform in the shader
            GL.UseProgram(shaderProgram);
            int resolutionLoc = GL.GetUniformLocation(shaderProgram, "uResolution");
            GL.Uniform2(resolutionLoc, (float)width, (float)height);
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

            // Test different zoom levels and center points
            // Uncomment these to test the generic transformer:

            // Default view: -10 to 10 range (zoomed out)
            SetWorldBounds(-10.0f, 10.0f, -10.0f, 10.0f);

            // Zoomed in view (objects appear larger)
            // SetWorldBounds(-5.0f, 5.0f, -5.0f, 5.0f);

            // Zoomed out view (objects appear smaller)
            // SetWorldBounds(-20.0f, 20.0f, -20.0f, 20.0f);

            // Off-center view (panned to the right and up)
            // SetWorldCenter(3.0f, 2.0f);

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
            angle += 0.02f; // Slower rotation for easier viewing

            // Optional: Animate zoom effect
            // Uncomment to see dynamic zooming
            // float zoom = 10.0f + 5.0f * (float)Math.Sin(angle * 0.5);
            // SetWorldBounds(-zoom, zoom, -zoom, zoom);

            GL.Clear(ClearBufferMask.ColorBufferBit);
            RenderGL();
            CheckGLError("After Tick");
        }

        private void RenderGL()
        {
            // Define square corners in world space
            // Using size 2.0 in our -10 to 10 world
            float size = 2.0f;

            // Pulsing size effect
            float pulse = 1.0f + 0.3f * (float)Math.Sin(angle * 2.0);
            size *= pulse;

            // Original square corners (before rotation)
            float[] corners = new float[8] {
                -size, -size,  // Bottom-left
                 size, -size,  // Bottom-right
                 size,  size,  // Top-right
                -size,  size   // Top-left
            };

            // Rotate corners
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

            // Convert to screen coordinates using our generic transformers
            float[] vertices = new float[8 * 6];

            for (int i = 0; i < 4; i++)
            {
                int nextI = (i + 1) % 4;

                // Transform world coordinates to screen coordinates
                float x1 = TX(rotatedCorners[i * 2]);
                float y1 = TY(rotatedCorners[i * 2 + 1]);
                float x2 = TX(rotatedCorners[nextI * 2]);
                float y2 = TY(rotatedCorners[nextI * 2 + 1]);

                int idx = i * 12;

                // Start vertex
                vertices[idx + 0] = x1;
                vertices[idx + 1] = y1;
                vertices[idx + 2] = 0.0f;
                vertices[idx + 3] = 1.0f; // White
                vertices[idx + 4] = 1.0f;
                vertices[idx + 5] = 1.0f;

                // End vertex
                vertices[idx + 6] = x2;
                vertices[idx + 7] = y2;
                vertices[idx + 8] = 0.0f;
                vertices[idx + 9] = 1.0f; // White
                vertices[idx + 10] = 1.0f;
                vertices[idx + 11] = 1.0f;
            }

            // Upload and draw
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, vertices.Length * sizeof(float), vertices);

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