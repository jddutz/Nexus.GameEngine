#version 450

// Vertex inputs: position only
layout(location = 0) in vec2 inPos;

// Uniform buffer: per-vertex colors
layout(binding = 0) uniform VertexColorsUBO {
    vec4 colors[4];  // One color per vertex (TL, BL, TR, BR)
} vertexColors;

// Output to fragment shader
layout(location = 0) out vec4 fragColor;

void main() {
    gl_Position = vec4(inPos, 0.0, 1.0);
    // Use gl_VertexIndex to look up the color for this vertex
    fragColor = vertexColors.colors[gl_VertexIndex];
}
