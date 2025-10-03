using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;

namespace WindowEngine
{
    public class Game
    {
        private readonly Surface screen;
        private int vao, vbo, shaderProgram;

        // World coordinate system parameters
        private float worldMinX = -10.0f;
        private float worldMaxX = 10.0f;
        private float worldMinY = -3.0f;
        private float worldMaxY = 3.0f;
        private float worldCenterX = 0.0f;
        private float worldCenterY = 0.0f;

        // Zoom and pan configuration
        private const float ZOOM_SPEED = 0.95f;      // Multiplier per zoom step (0.95 = zoom in 5%)
        private const float PAN_SPEED = 0.5f;        // World units to pan per second
        private const float MIN_ZOOM = 0.1f;         // Minimum world range (max zoom in)
        private const float MAX_ZOOM = 50.0f;        // Maximum world range (max zoom out)

        public Game(int width, int height)
        {
            screen = new Surface(width, height);
        }

        /// <summary>
        /// Transform X: World to Screen coordinates
        /// </summary>
        private float TX(float worldX)
        {
            float centeredX = worldX - worldCenterX;
            float worldWidth = worldMaxX - worldMinX;
            float normalizedX = (centeredX + worldWidth / 2.0f) / worldWidth;
            return normalizedX * screen.width;
        }

        /// <summary>
        /// Transform Y: World to Screen coordinates (with Y-inversion)
        /// </summary>
        private float TY(float worldY)
        {
            float centeredY = worldY - worldCenterY;
            float worldHeight = worldMaxY - worldMinY;
            float normalizedY = (-centeredY + worldHeight / 2.0f) / worldHeight;
            return normalizedY * screen.height;
        }

        public void SetWorldBounds(float minX, float maxX, float minY, float maxY)
        {
            worldMinX = minX;
            worldMaxX = maxX;
            worldMinY = minY;
            worldMaxY = maxY;
        }

        public void SetWorldCenter(float centerX, float centerY)
        {
            worldCenterX = centerX;
            worldCenterY = centerY;
        }

        public void UpdateScreenSize(int width, int height)
        {
            screen.width = width;
            screen.height = height;

            GL.UseProgram(shaderProgram);
            int resolutionLoc = GL.GetUniformLocation(shaderProgram, "uResolution");
            GL.Uniform2(resolutionLoc, (float)width, (float)height);
        }

        /// <summary>
        /// Handle keyboard input for zoom and pan
        /// </summary>
        public void HandleInput(KeyboardState keyboard, float deltaTime)
        {
            // Zoom controls
            if (keyboard.IsKeyDown(Keys.Z))
            {
                // Zoom IN - decrease world range
                float currentRangeX = worldMaxX - worldMinX;
                float currentRangeY = worldMaxY - worldMinY;

                float newRangeX = currentRangeX * ZOOM_SPEED;
                float newRangeY = currentRangeY * ZOOM_SPEED;

                // Clamp to minimum zoom
                if (newRangeX > MIN_ZOOM && newRangeY > MIN_ZOOM)
                {
                    SetWorldBounds(
                        worldCenterX - newRangeX / 2,
                        worldCenterX + newRangeX / 2,
                        worldCenterY - newRangeY / 2,
                        worldCenterY + newRangeY / 2
                    );
                }
            }

            if (keyboard.IsKeyDown(Keys.X))
            {
                // Zoom OUT - increase world range
                float currentRangeX = worldMaxX - worldMinX;
                float currentRangeY = worldMaxY - worldMinY;

                float newRangeX = currentRangeX / ZOOM_SPEED;
                float newRangeY = currentRangeY / ZOOM_SPEED;

                // Clamp to maximum zoom
                if (newRangeX < MAX_ZOOM && newRangeY < MAX_ZOOM)
                {
                    SetWorldBounds(
                        worldCenterX - newRangeX / 2,
                        worldCenterX + newRangeX / 2,
                        worldCenterY - newRangeY / 2,
                        worldCenterY + newRangeY / 2
                    );
                }
            }

            // Pan controls
            float panAmount = PAN_SPEED * deltaTime;

            if (keyboard.IsKeyDown(Keys.Left))
            {
                worldCenterX -= panAmount;
                float rangeX = worldMaxX - worldMinX;
                float rangeY = worldMaxY - worldMinY;
                SetWorldBounds(
                    worldCenterX - rangeX / 2,
                    worldCenterX + rangeX / 2,
                    worldCenterY - rangeY / 2,
                    worldCenterY + rangeY / 2
                );
            }

            if (keyboard.IsKeyDown(Keys.Right))
            {
                worldCenterX += panAmount;
                float rangeX = worldMaxX - worldMinX;
                float rangeY = worldMaxY - worldMinY;
                SetWorldBounds(
                    worldCenterX - rangeX / 2,
                    worldCenterX + rangeX / 2,
                    worldCenterY - rangeY / 2,
                    worldCenterY + rangeY / 2
                );
            }

            if (keyboard.IsKeyDown(Keys.Up))
            {
                worldCenterY += panAmount;
                float rangeX = worldMaxX - worldMinX;
                float rangeY = worldMaxY - worldMinY;
                SetWorldBounds(
                    worldCenterX - rangeX / 2,
                    worldCenterX + rangeX / 2,
                    worldCenterY - rangeY / 2,
                    worldCenterY + rangeY / 2
                );
            }

            if (keyboard.IsKeyDown(Keys.Down))
            {
                worldCenterY -= panAmount;
                float rangeX = worldMaxX - worldMinX;
                float rangeY = worldMaxY - worldMinY;
                SetWorldBounds(
                    worldCenterX - rangeX / 2,
                    worldCenterX + rangeX / 2,
                    worldCenterY - rangeY / 2,
                    worldCenterY + rangeY / 2
                );
            }

            // Reset view
            if (keyboard.IsKeyPressed(Keys.R))
            {
                worldCenterX = 0.0f;
                worldCenterY = 0.0f;
                SetWorldBounds(-10.0f, 10.0f, -3.0f, 3.0f);
                Console.WriteLine("View reset to default");
            }
        }

        public void Init()
        {
            GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.CullFace);
            GL.Enable(EnableCap.LineSmooth);
            GL.Hint(HintTarget.LineSmoothHint, HintMode.Nicest);
            GL.LineWidth(1.5f);

            // Initial view
            SetWorldBounds(-10.0f, 10.0f, -3.0f, 3.0f);

            // Create VAO and VBO
            vao = GL.GenVertexArray();
            vbo = GL.GenBuffer();
            GL.BindVertexArray(vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);

            // Allocate large buffer for function plot and axes
            GL.BufferData(BufferTarget.ArrayBuffer, 10000 * 6 * sizeof(float), IntPtr.Zero, BufferUsageHint.DynamicDraw);

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

            shaderProgram = GL.CreateProgram();
            GL.AttachShader(shaderProgram, vertexShader);
            GL.AttachShader(shaderProgram, fragmentShader);
            GL.LinkProgram(shaderProgram);
            CheckProgramError(shaderProgram);

            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

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
            int vertexCount = 0;
            float[] vertices = new float[10000 * 6];

            // Draw X-axis (red)
            float xAxisY = TY(0);
            if (xAxisY >= 0 && xAxisY <= screen.height)
            {
                vertices[vertexCount++] = 0;
                vertices[vertexCount++] = xAxisY;
                vertices[vertexCount++] = 0;
                vertices[vertexCount++] = 1.0f; // Red
                vertices[vertexCount++] = 0.0f;
                vertices[vertexCount++] = 0.0f;

                vertices[vertexCount++] = screen.width;
                vertices[vertexCount++] = xAxisY;
                vertices[vertexCount++] = 0;
                vertices[vertexCount++] = 1.0f;
                vertices[vertexCount++] = 0.0f;
                vertices[vertexCount++] = 0.0f;
            }

            // Draw Y-axis (green)
            float yAxisX = TX(0);
            if (yAxisX >= 0 && yAxisX <= screen.width)
            {
                vertices[vertexCount++] = yAxisX;
                vertices[vertexCount++] = 0;
                vertices[vertexCount++] = 0;
                vertices[vertexCount++] = 0.0f;
                vertices[vertexCount++] = 1.0f; // Green
                vertices[vertexCount++] = 0.0f;

                vertices[vertexCount++] = yAxisX;
                vertices[vertexCount++] = screen.height;
                vertices[vertexCount++] = 0;
                vertices[vertexCount++] = 0.0f;
                vertices[vertexCount++] = 1.0f;
                vertices[vertexCount++] = 0.0f;
            }

            // Plot y = sin(x) function (blue)
            int numPoints = 500;
            for (int i = 0; i < numPoints - 1; i++)
            {
                float t1 = i / (float)(numPoints - 1);
                float t2 = (i + 1) / (float)(numPoints - 1);

                float x1 = worldMinX + t1 * (worldMaxX - worldMinX);
                float y1 = (float)Math.Sin(x1);

                float x2 = worldMinX + t2 * (worldMaxX - worldMinX);
                float y2 = (float)Math.Sin(x2);

                float screenX1 = TX(x1);
                float screenY1 = TY(y1);
                float screenX2 = TX(x2);
                float screenY2 = TY(y2);

                vertices[vertexCount++] = screenX1;
                vertices[vertexCount++] = screenY1;
                vertices[vertexCount++] = 0;
                vertices[vertexCount++] = 0.3f; // Light blue
                vertices[vertexCount++] = 0.5f;
                vertices[vertexCount++] = 1.0f;

                vertices[vertexCount++] = screenX2;
                vertices[vertexCount++] = screenY2;
                vertices[vertexCount++] = 0;
                vertices[vertexCount++] = 0.3f;
                vertices[vertexCount++] = 0.5f;
                vertices[vertexCount++] = 1.0f;
            }

            // Upload and draw
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, vertexCount * sizeof(float), vertices);

            GL.UseProgram(shaderProgram);
            GL.BindVertexArray(vao);
            GL.DrawArrays(PrimitiveType.Lines, 0, vertexCount / 6);

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
            OpenTK.Graphics.OpenGL4.ErrorCode error = GL.GetError();
            if (error != OpenTK.Graphics.OpenGL4.ErrorCode.NoError)
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