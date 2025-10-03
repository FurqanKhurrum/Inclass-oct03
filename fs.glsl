#version 330 core

// INPUT: Interpolated data from vertex shader
// Each fragment gets a smoothly blended value between the three vertices
in vec3 vColor;

// OUTPUT: Final pixel color
out vec4 FragColor;

void main()
{
    // FRAGMENT SHADER PIPELINE:
    // This runs for EVERY pixel inside the triangle
    // For our terrain, this runs millions of times per frame!
    
    // Simple pass-through: just use the interpolated vertex color
    FragColor = vec4(vColor, 1.0);
    
    // ADVANCED EFFECTS (commented out for now):
    // Water effects: Add sine wave ripples based on position
    // vec2 uv = gl_FragCoord.xy / 800.0;
    // float wave = sin(uv.x * 20.0 + time) * 0.1;
    // FragColor = vec4(vColor + wave, 1.0);
    
    // Fog: Distance-based color fading
    // float fogAmount = gl_FragCoord.z / gl_FragCoord.w;
    // vec3 fogColor = vec3(0.7, 0.8, 0.9);
    // FragColor = mix(vec4(vColor, 1.0), vec4(fogColor, 1.0), fogAmount);
}