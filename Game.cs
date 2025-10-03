using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Drawing;
using System.Diagnostics;

namespace WindowEngine
{
    public class Game
    {
        private readonly Surface screen;
        private Surface map;
        private float[,] h;
        private int vao, vbo, shaderProgram;
        private float rotationAngle = 0.0f;
        private float flyoverZ = -5.0f;
        private const int MAP_SIZE = 128; // Change to 512 for performance test!

        private float[] vertexData;
        private int vertexCount;

        // FPS counter
        private Stopwatch fpsTimer;
        private int frameCount = 0;
        private double elapsedTime = 0;
        private double currentFPS = 0;

        // Animation
        private float timeAccumulator = 0.0f;
        private bool animateWaves = false;

        public Game(int width, int height)
        {
            screen = new Surface(width, height);
            fpsTimer = Stopwatch.StartNew();
        }

        public void UpdateScreenSize(int width, int height)
        {
            screen.width = width;
            screen.height = height;
            GL.Viewport(0, 0, width, height);
        }

        private void LoadHeightmap(string filename)
        {
            try
            {
                using (Bitmap bitmap = new Bitmap(filename))
                {
                    int width = Math.Min(bitmap.Width, MAP_SIZE);
                    int height = Math.Min(bitmap.Height, MAP_SIZE);

                    map = new Surface(width, height);
                    h = new float[width, height];

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            Color pixel = bitmap.GetPixel(x, y);
                            int grayscale = pixel.R;
                            map.pixels[x + y * width] = grayscale;
                            h[x, y] = grayscale / 256f;
                        }
                    }

                    Console.WriteLine($"Heightmap loaded: {width}x{height}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading heightmap: {ex.Message}");
                Console.WriteLine("Creating procedural terrain...");

                map = new Surface(MAP_SIZE, MAP_SIZE);
                h = new float[MAP_SIZE, MAP_SIZE];

                // Create interesting procedural terrain with multiple hills
                for (int y = 0; y < MAP_SIZE; y++)
                {
                    for (int x = 0; x < MAP_SIZE; x++)
                    {
                        float nx = x / (float)MAP_SIZE;
                        float ny = y / (float)MAP_SIZE;

                        // Multiple octaves of noise for interesting terrain
                        float height = 0;
                        height += 0.5f * PerlinNoise(nx * 2, ny * 2);
                        height += 0.25f * PerlinNoise(nx * 4, ny * 4);
                        height += 0.125f * PerlinNoise(nx * 8, ny * 8);

                        h[x, y] = Math.Clamp(height, 0, 1);
                    }
                }
            }
        }

        // Simple Perlin-like noise function
        private float PerlinNoise(float x, float y)
        {
            int xi = (int)x;
            int yi = (int)y;
            float xf = x - xi;
            float yf = y - yi;

            float n00 = DotGridGradient(xi, yi, x, y);
            float n10 = DotGridGradient(xi + 1, yi, x, y);
            float n01 = DotGridGradient(xi, yi + 1, x, y);
            float n11 = DotGridGradient(xi + 1, yi + 1, x, y);

            float u = Fade(xf);
            float v = Fade(yf);

            float nx0 = Lerp(n00, n10, u);
            float nx1 = Lerp(n01, n11, u);

            return Lerp(nx0, nx1, v);
        }

        private float DotGridGradient(int ix, int iy, float x, float y)
        {
            float dx = x - ix;
            float dy = y - iy;

            int hash = (ix * 374761393 + iy * 668265263) % 4;
            float gx = (hash & 1) == 0 ? 1 : -1;
            float gy = (hash & 2) == 0 ? 1 : -1;

            return dx * gx + dy * gy;
        }

        private float Fade(float t)
        {
            return t * t * t * (t * (t * 6 - 15) + 10);
        }

        private float Lerp(float a, float b, float t)
        {
            return a + t * (b - a);
        }

        private void PrepareVertexData()
        {
            int gridSize = MAP_SIZE - 1;
            int totalFloats = gridSize * gridSize * 2 * 3 * 6;
            vertexData = new float[totalFloats];

            Console.WriteLine($"Vertex buffer: {totalFloats} floats = {totalFloats * 4 / 1024 / 1024f:F2} MB");

            int index = 0;

            for (int z = 0; z < gridSize; z++)
            {
                for (int x = 0; x < gridSize; x++)
                {
                    float x0 = (x - MAP_SIZE / 2f) * 0.05f;
                    float x1 = ((x + 1) - MAP_SIZE / 2f) * 0.05f;
                    float z0 = (z - MAP_SIZE / 2f) * 0.05f;
                    float z1 = ((z + 1) - MAP_SIZE / 2f) * 0.05f;

                    float y00 = h[x, z] * 2.0f;
                    float y10 = h[x + 1, z] * 2.0f;
                    float y01 = h[x, z + 1] * 2.0f;
                    float y11 = h[x + 1, z + 1] * 2.0f;

                    float[] color00 = GetHeightColor(h[x, z]);
                    float[] color10 = GetHeightColor(h[x + 1, z]);
                    float[] color01 = GetHeightColor(h[x, z + 1]);
                    float[] color11 = GetHeightColor(h[x + 1, z + 1]);

                    // Triangle 1
                    AddVertex(ref index, x0, y00, z0, color00);
                    AddVertex(ref index, x0, y01, z1, color01);
                    AddVertex(ref index, x1, y10, z0, color10);

                    // Triangle 2
                    AddVertex(ref index, x1, y10, z0, color10);
                    AddVertex(ref index, x0, y01, z1, color01);
                    AddVertex(ref index, x1, y11, z1, color11);
                }
            }

            vertexCount = index / 6;
            Console.WriteLine($"Vertices: {vertexCount} | Triangles: {vertexCount / 3}");
        }

        private void AddVertex(ref int index, float x, float y, float z, float[] color)
        {
            vertexData[index++] = x;
            vertexData[index++] = y;
            vertexData[index++] = z;
            vertexData[index++] = color[0];
            vertexData[index++] = color[1];
            vertexData[index++] = color[2];
        }

        private float[] GetHeightColor(float height)
        {
            if (height < 0.3f)
                return new float[] { 0.2f, 0.5f + height, 0.3f };
            else if (height < 0.7f)
                return new float[] { 0.4f + height * 0.3f, 0.5f, 0.2f };
            else
                return new float[] { 0.8f + height * 0.2f, 0.8f + height * 0.2f, 0.9f + height * 0.1f };
        }

        public void Init()
        {
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Less);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
            GL.ClearColor(0.2f, 0.3f, 0.4f, 1.0f);

            LoadHeightmap("heightmap.png");
            PrepareVertexData();

            // Create VBO
            vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer,
                         vertexData.Length * sizeof(float),
                         vertexData,
                         BufferUsageHint.StaticDraw);

            // Create VAO
            vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            string vertexShaderSource = @"
                #version 330 core
                layout(location = 0) in vec3 aPosition;
                layout(location = 1) in vec3 aColor;
                
                out vec3 vColor;
                
                uniform mat4 uModel;
                uniform mat4 uView;
                uniform mat4 uProjection;
                
                void main()
                {
                    gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0);
                    vColor = aColor;
                }";

            string fragmentShaderSource = @"
                #version 330 core
                in vec3 vColor;
                out vec4 FragColor;
                
                void main()
                {
                    FragColor = vec4(vColor, 1.0);
                }";

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

            Console.WriteLine("\n=== CONTROLS ===");
            Console.WriteLine("Arrow Keys: Rotate");
            Console.WriteLine("Z/X: Zoom in/out");
            Console.WriteLine("W: Toggle wave animation");
            Console.WriteLine("R: Reset view");
            Console.WriteLine("ESC: Exit");

            CheckGLError("After Init");
        }

        public void HandleInput(KeyboardState keyboard, float deltaTime)
        {
            if (keyboard.IsKeyDown(Keys.Left))
                rotationAngle -= 1.0f * deltaTime * 60f;

            if (keyboard.IsKeyDown(Keys.Right))
                rotationAngle += 1.0f * deltaTime * 60f;

            if (keyboard.IsKeyDown(Keys.Z))
                flyoverZ += 2.0f * deltaTime;

            if (keyboard.IsKeyDown(Keys.X))
                flyoverZ -= 2.0f * deltaTime;

            flyoverZ = Math.Clamp(flyoverZ, -15.0f, -2.0f);

            if (keyboard.IsKeyPressed(Keys.W))
            {
                animateWaves = !animateWaves;
                Console.WriteLine($"Wave animation: {(animateWaves ? "ON" : "OFF")}");
            }

            if (keyboard.IsKeyPressed(Keys.R))
            {
                rotationAngle = 0.0f;
                flyoverZ = -5.0f;
                animateWaves = false;
                Console.WriteLine("View reset");
            }
        }

        public void Tick()
        {
            rotationAngle += 0.2f;

            if (animateWaves)
            {
                timeAccumulator += 0.016f;
            }

            // FPS counter
            frameCount++;
            elapsedTime = fpsTimer.Elapsed.TotalSeconds;

            if (elapsedTime >= 1.0)
            {
                currentFPS = frameCount / elapsedTime;
                Console.WriteLine($"FPS: {currentFPS:F1} | Vertices: {vertexCount} | Draw calls: 1");
                frameCount = 0;
                fpsTimer.Restart();
            }

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            RenderGL();
            CheckGLError("After Tick");
        }

        private void RenderGL()
        {
            GL.UseProgram(shaderProgram);

            // Model matrix with optional wave animation
            Matrix4 model = Matrix4.CreateRotationY(MathHelper.DegreesToRadians(rotationAngle));

            if (animateWaves)
            {
                // Add wavy motion
                model = Matrix4.CreateScale(1.0f, 1.0f + 0.1f * (float)Math.Sin(timeAccumulator), 1.0f) * model;
            }

            // View matrix
            Vector3 cameraPos = new Vector3(0, 2.0f, flyoverZ);
            Vector3 cameraTarget = new Vector3(0, 0, 0);
            Vector3 cameraUp = new Vector3(0, 1, 0);
            Matrix4 view = Matrix4.LookAt(cameraPos, cameraTarget, cameraUp);

            // Projection matrix
            float aspectRatio = screen.width / (float)screen.height;
            Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(45.0f),
                aspectRatio,
                0.1f,
                100.0f
            );

            // Set uniforms
            int modelLoc = GL.GetUniformLocation(shaderProgram, "uModel");
            int viewLoc = GL.GetUniformLocation(shaderProgram, "uView");
            int projectionLoc = GL.GetUniformLocation(shaderProgram, "uProjection");

            GL.UniformMatrix4(modelLoc, false, ref model);
            GL.UniformMatrix4(viewLoc, false, ref view);
            GL.UniformMatrix4(projectionLoc, false, ref projection);

            // EFFICIENT RENDERING: Single draw call for entire terrain!
            // This is the power of VBOs - all vertex data already on GPU
            GL.BindVertexArray(vao);
            GL.DrawArrays(PrimitiveType.Triangles, 0, vertexCount);

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
            this.pixels = new int[width * height];
        }
    }
}