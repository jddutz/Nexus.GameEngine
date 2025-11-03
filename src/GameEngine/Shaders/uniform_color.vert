#version 450

// Vertex inputs: position only
layout(location = 0) in vec2 inPos;

// UBO: Camera ViewProjection matrix (set=0, binding=0)
layout(set = 0, binding = 0) uniform ViewProjectionUBO {
    mat4 viewProjection;
} camera;

// Push constants: model matrix (64 bytes) + color (16 bytes) = 80 bytes
// Must match UniformColorPushConstants struct layout in C#
layout(push_constant) uniform PushConstants {
    mat4 model;
    vec4 color;
} pc;

// Output to fragment shader
layout(location = 0) out vec4 fragColor;

void main() {
    // Transform pipeline: Local -> World -> Clip
    // 1. Apply model matrix to transform vertex from local to world space
    vec4 worldPos = pc.model * vec4(inPos, 0.0, 1.0);
    
    // 2. Apply camera's view-projection matrix to transform to clip space
    gl_Position = camera.viewProjection * worldPos;
    
    // Pass color to fragment shader
    fragColor = pc.color;
}
