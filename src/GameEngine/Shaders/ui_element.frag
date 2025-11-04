#version 450

layout(location = 0) in vec2 fragTexCoord;
layout(location = 1) in vec4 fragTintColor;

layout(location = 0) out vec4 outColor;

layout(set = 1, binding = 0) uniform sampler2D texSampler;

void main() {
    vec4 texColor = texture(texSampler, fragTexCoord);
    outColor = texColor * fragTintColor;
}