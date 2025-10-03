#version 330 core

// INPUT: Per-vertex attributes from our VBO
layout(location = 0) in vec3 aPosition;  // Vertex position (x, y, z)
layout(location = 1) in vec3 aColor;     // Vertex color (r, g, b)

// OUTPUT: Data passed to fragment shader (interpolated across triangle)
out vec3 vColor;

// UNIFORMS: Data that stays constant for all vertices in a draw call
uniform mat4 uModel;        // Object-to-world transformation
uniform mat4 uView;         // World-to-camera transformation
uniform mat4 uProjection;   // Camera-to-screen projection

void main()
{
    // VERTEX SHADER PIPELINE:
    // 1. Transform vertex from object space -> world space -> camera space -> clip space
    // 2. GPU will later divide by w for perspective (creates screen coordinates)
    gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0);
    
    // 3. Pass color to fragment shader (will be interpolated across triangle)
    vColor = aColor;
}