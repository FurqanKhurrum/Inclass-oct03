using OpenTK.Graphics.OpenGL4;
using System.Drawing;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace WindowEngine
{
    public class Game
    {
        private readonly Surface screen;
        private Surface map;
        private float[,] h;
        private int vao, vbo, ebo, shaderProgram;
        private float rotationAngle = 0.0f;
        private float flyoverZ = -5.0f;
        private const int MAP_SIZE = 128;

        public Game(int width, int height)
        {
            screen = new Surface(width, height);
        }

        public void UpdateScreenSize(int width, int height)
        {
            screen.width = width;
            screen.height = height;

            // Update viewport
            GL.Viewport(0, 0, width, height);
        }

        /// <summary>
        /// Load heightmap from PNG file
        /// Extracts grayscale values and converts to height data
        /// </summary>
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

                    // Extract height data from grayscale values
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            Color pixel = bitmap.GetPixel(x, y);
                            // Use red channel for grayscale (R=G=B in grayscale images)
                            int grayscale = pixel.R;

                            // Store in map pixels
                            map.pixels[x + y * width] = grayscale;

                            // Convert to height: 0-255 -> 0.0-1.0
                            h[x, y] = grayscale / 256f;
                        }
                    }

                    Console.WriteLine($"Heightmap loaded: {width}x{height}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading heightmap: {ex.Message}");
                Console.WriteLine("Creating default flat terrain...");

                // Create default flat terrain
                map = new Surface(MAP_SIZE, MAP_SIZE);
                h = new float[MAP_SIZE, MAP_SIZE];

                // Create a simple test pattern
                for (int y = 0; y < MAP_SIZE; y++)
                {
                    for (int x = 0; x < MAP_SIZE; x++)
                    {
                        // Create a simple hill in the center
                        float dx = (x - MAP_SIZE / 2f) / (MAP_SIZE / 4f);
                        float dy = (y - MAP_SIZE / 2f) / (MAP_SIZE / 4f);
                        float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                        h[x, y] = Math.Max(0, 1.0f - dist * 0.5f);
                    }
                }
            }
        }

        /// <summary>
        /// Generate vertex and index data for the terrain mesh
        /// Creates a grid of quads (as triangles) from heightmap
        /// </summary>
        private void GenerateTerrainMesh(out float[] vertices, out uint[] indices)
        {
            int gridSize = MAP_SIZE - 1; // 127x127 quads
            int vertexCount = MAP_SIZE * MAP_SIZE;
            int indexCount = gridSize * gridSize * 6; // 6 indices per quad (2 triangles)

            vertices = new float[vertexCount * 6]; // x, y, z, r, g, b per vertex
            indices = new uint[indexCount];

            int vertexIndex = 0;

            // Generate vertices
            for (int y = 0; y < MAP_SIZE; y++)
            {
                for (int x = 0; x < MAP_SIZE; x++)
                {
                    // Scale and center the terrain
                    float posX = (x - MAP_SIZE / 2f) * 0.05f;
                    float posY = h[x, y] * 2.0f; // Height (scaled up for visibility)
                    float posZ = (y - MAP_SIZE / 2f) * 0.05f;

                    vertices[vertexIndex++] = posX;
                    vertices[vertexIndex++] = posY;
                    vertices[vertexIndex++] = posZ;

                    // Color based on height (green lowlands, brown/white highlands)
                    float heightValue = h[x, y];
                    if (heightValue < 0.3f)
                    {
                        // Low areas - green/blue (water/grass)
                        vertices[vertexIndex++] = 0.2f;
                        vertices[vertexIndex++] = 0.5f + heightValue;
                        vertices[vertexIndex++] = 0.3f;
                    }
                    else if (heightValue < 0.7f)
                    {
                        // Mid areas - brown/green (hills)
                        vertices[vertexIndex++] = 0.4f + heightValue * 0.3f;
                        vertices[vertexIndex++] = 0.5f;
                        vertices[vertexIndex++] = 0.2f;
                    }
                    else
                    {
                        // High areas - white/gray (mountains)
                        vertices[vertexIndex++] = 0.8f + heightValue * 0.2f;
                        vertices[vertexIndex++] = 0.8f + heightValue * 0.2f;
                        vertices[vertexIndex++] = 0.9f + heightValue * 0.1f;
                    }
                }
            }

            // Generate indices for quads (2 triangles per quad)
            int indexIndex = 0;
            for (int y = 0; y < gridSize; y++)
            {
                for (int x = 0; x < gridSize; x++)
                {
                    uint topLeft = (uint)(y * MAP_SIZE + x);
                    uint topRight = (uint)(y * MAP_SIZE + x + 1);
                    uint bottomLeft = (uint)((y + 1) * MAP_SIZE + x);
                    uint bottomRight = (uint)((y + 1) * MAP_SIZE + x + 1);

                    // First triangle
                    indices[indexIndex++] = topLeft;
                    indices[indexIndex++] = bottomLeft;
                    indices[indexIndex++] = topRight;

                    // Second triangle
                    indices[indexIndex++] = topRight;
                    indices[indexIndex++] = bottomLeft;
                    indices[indexIndex++] = bottomRight;
                }
            }
        }

        public void Init()
        {
            // Enable depth testing for 3D
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Less);

            // Enable backface culling for performance
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);

            GL.ClearColor(0.2f, 0.3f, 0.4f, 1.0f); // Sky blue background

            // Load heightmap
            Console.WriteLine("Loading heightmap...");
            LoadHeightmap("heightmap.png");

            // Generate terrain mesh
            GenerateTerrainMesh(out float[] vertices, out uint[] indices);

            // Create VAO
            vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);

            // Create VBO
            vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            // Create EBO (Element Buffer Object)
            ebo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

            // Position attribute (location = 0)
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            // Color attribute (location = 1)
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            // Vertex shader - handles 3D transformations
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

            Console.WriteLine("3D Terrain initialized successfully!");
            Console.WriteLine("Controls:");
            Console.WriteLine("  Arrow Keys - Rotate camera");
            Console.WriteLine("  Z/X - Flyover closer/farther");
            Console.WriteLine("  R - Reset view");

            CheckGLError("After Init");
        }

        public void HandleInput(KeyboardState keyboard, float deltaTime)
        {
            // Rotation controls
            if (keyboard.IsKeyDown(Keys.Left))
                rotationAngle -= 1.0f * deltaTime * 60f;

            if (keyboard.IsKeyDown(Keys.Right))
                rotationAngle += 1.0f * deltaTime * 60f;

            // Flyover controls
            if (keyboard.IsKeyDown(Keys.Z))
                flyoverZ += 2.0f * deltaTime; // Move closer

            if (keyboard.IsKeyDown(Keys.X))
                flyoverZ -= 2.0f * deltaTime; // Move farther

            // Clamp flyover distance
            flyoverZ = Math.Clamp(flyoverZ, -15.0f, -2.0f);

            // Reset view
            if (keyboard.IsKeyPressed(Keys.R))
            {
                rotationAngle = 0.0f;
                flyoverZ = -5.0f;
                Console.WriteLine("View reset");
            }
        }

        public void Tick()
        {
            // Auto-rotate slowly for demo effect
            rotationAngle += 0.2f;

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            RenderGL();
            CheckGLError("After Tick");
        }

        private void RenderGL()
        {
            GL.UseProgram(shaderProgram);

            // Model matrix - rotation around Y axis
            Matrix4 model = Matrix4.CreateRotationY(MathHelper.DegreesToRadians(rotationAngle));

            // View matrix - camera looking at terrain from above and behind
            Vector3 cameraPos = new Vector3(0, 2.0f, flyoverZ);
            Vector3 cameraTarget = new Vector3(0, 0, 0);
            Vector3 cameraUp = new Vector3(0, 1, 0);
            Matrix4 view = Matrix4.LookAt(cameraPos, cameraTarget, cameraUp);

            // Projection matrix - perspective
            float aspectRatio = screen.width / (float)screen.height;
            Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(45.0f), // FOV
                aspectRatio,
                0.1f,  // Near plane
                100.0f // Far plane
            );

            // Set uniforms
            int modelLoc = GL.GetUniformLocation(shaderProgram, "uModel");
            int viewLoc = GL.GetUniformLocation(shaderProgram, "uView");
            int projectionLoc = GL.GetUniformLocation(shaderProgram, "uProjection");

            GL.UniformMatrix4(modelLoc, false, ref model);
            GL.UniformMatrix4(viewLoc, false, ref view);
            GL.UniformMatrix4(projectionLoc, false, ref projection);

            // Draw terrain
            GL.BindVertexArray(vao);
            int indexCount = (MAP_SIZE - 1) * (MAP_SIZE - 1) * 6;
            GL.DrawElements(PrimitiveType.Triangles, indexCount, DrawElementsType.UnsignedInt, 0);

            CheckGLError("After RenderGL");
        }

        public void Cleanup()
        {
            GL.DeleteBuffer(vbo);
            GL.DeleteBuffer(ebo);
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