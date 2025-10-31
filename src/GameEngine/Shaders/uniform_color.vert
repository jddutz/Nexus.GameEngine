#version 450

// Vertex inputs: position only
layout(location = 0) in vec2 inPos;

// Push constants: view/projection matrix and uniform color
layout(push_constant) uniform PushConstants {
    mat4 viewProj;
    vec4 color;
} pc;

// Output to fragment shader
layout(location = 0) out vec4 fragColor;

void main() {
    // Transform vertex from pixel space into NDC using the provided view-projection matrix
    gl_Position = pc.viewProj * vec4(inPos, 0.0, 1.0);
    fragColor = pc.color;
}
