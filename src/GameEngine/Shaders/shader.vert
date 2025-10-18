#version 450

// Vertex input: just position
layout(location = 0) in vec2 inPos;

// Push constants: per-vertex colors
layout(push_constant) uniform PushConstants {
    vec4 color0;  // Top-left
    vec4 color1;  // Bottom-left
    vec4 color2;  // Top-right
    vec4 color3;  // Bottom-right
} pushConstants;

// Output to fragment shader
layout(location = 0) out vec4 fragColor;

void main() {
    gl_Position = vec4(inPos, 0.0, 1.0);
    
    // Select color based on vertex index
    // Assuming triangle strip order: 0=TL, 1=BL, 2=TR, 3=BR
    if (gl_VertexIndex == 0)
        fragColor = pushConstants.color0;
    else if (gl_VertexIndex == 1)
        fragColor = pushConstants.color1;
    else if (gl_VertexIndex == 2)
        fragColor = pushConstants.color2;
    else
        fragColor = pushConstants.color3;
}