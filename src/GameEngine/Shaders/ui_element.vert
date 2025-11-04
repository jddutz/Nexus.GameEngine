#version 450

layout(location = 0) in vec2 inPos;
layout(location = 1) in vec2 inTexCoord;

layout(location = 0) out vec2 fragTexCoord;
layout(location = 1) out vec4 fragTintColor;

layout(set = 0, binding = 0) uniform ViewProjectionUBO {
    mat4 viewProjection;
};

layout(push_constant) uniform PushConstants {
    mat4 model;
    vec4 tintColor;
    vec4 uvRect;  // (minU, minV, maxU, maxV)
};

void main() {
    gl_Position = viewProjection * model * vec4(inPos, 0.0, 1.0);
    
    // Transform vertex UVs from base geometry (0..1) to atlas UVs using push constant uvRect
    // inTexCoord is the base quad UV (0..1), uvRect defines the sub-rectangle in the atlas
    vec2 uvMin = uvRect.xy;
    vec2 uvMax = uvRect.zw;
    fragTexCoord = mix(uvMin, uvMax, inTexCoord);
    
    fragTintColor = tintColor;
}