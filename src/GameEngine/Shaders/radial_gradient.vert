#version 450

// Vertex input: position only
layout(location = 0) in vec2 inPos;

// Output to fragment shader
layout(location = 0) out vec2 fragPos;

void main() {
    gl_Position = vec4(inPos, 0.0, 1.0);
    fragPos = inPos;  // Pass normalized device coordinates [-1,1] to fragment shader
}
