#version 450

// Input from vertex shader
layout(location = 0) in vec2 fragTexCoord;

// Texture sampler (descriptor set 0, binding 0)
layout(set = 0, binding = 0) uniform sampler2D texSampler;

// Output color
layout(location = 0) out vec4 outColor;

void main() {
    // Sample texture using interpolated UV coordinates from vertex shader
    outColor = texture(texSampler, fragTexCoord);
    
    // UV coordinate visualization (for debugging):
    // outColor = vec4(fragTexCoord.x, fragTexCoord.y, 0.0, 1.0);
}
