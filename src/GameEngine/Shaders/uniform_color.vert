#version 450

// Vertex inputs: position only
layout(location = 0) in vec2 inPos;

// Push constants: uniform color for all vertices
layout(push_constant) uniform PushConstants {
    vec4 color;
} pc;

// Output to fragment shader
layout(location = 0) out vec4 fragColor;

void main() {
    gl_Position = vec4(inPos, 0.0, 1.0);
    fragColor = pc.color;
}
