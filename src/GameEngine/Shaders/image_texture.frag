#version 450

// Input from vertex shader
layout(location = 0) in vec2 fragTexCoord;

// Push constants (must match vertex shader declaration)
layout(push_constant) uniform PushConstants {
    vec2 uvMin;
    vec2 uvMax;
    vec4 tintColor;
} pc;

// Texture sampler (descriptor set 0, binding 0)
layout(set = 0, binding = 0) uniform sampler2D texSampler;

// Output color
layout(location = 0) out vec4 outColor;

void main() {
    // Sample texture and multiply by tint color
    vec4 texSample = texture(texSampler, fragTexCoord);
    outColor = texSample * pc.tintColor;
    
    // UV coordinate visualization (for debugging):
    // outColor = vec4(fragTexCoord.x, fragTexCoord.y, 0.0, 1.0);
}
