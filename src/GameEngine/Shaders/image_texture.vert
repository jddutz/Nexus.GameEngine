#version 450

// Input vertex attributes
layout(location = 0) in vec2 inPosition;
layout(location = 1) in vec2 inTexCoord;

// Push constants for UV bounds and tint color
layout(push_constant) uniform PushConstants {
    vec2 uvMin;
    vec2 uvMax;
    vec4 tintColor;
} pc;

// Output to fragment shader
layout(location = 0) out vec2 fragTexCoord;

void main() {
    gl_Position = vec4(inPosition, 0.0, 1.0);
    
    // Apply UV bounds (for Fill mode cropping)
    // Manual interpolation: uvMin + (uvMax - uvMin) * inTexCoord
    fragTexCoord = pc.uvMin + (pc.uvMax - pc.uvMin) * inTexCoord;
}
