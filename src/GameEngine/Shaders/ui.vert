#version 450

layout(location = 0) in vec2 inPos;
layout(location = 1) in vec2 inTexCoord;

layout(location = 0) out vec2 fragTexCoord;
layout(location = 1) out vec4 fragTintColor;

layout(set = 0, binding = 0) uniform ViewProjectionUBO {
    mat4 viewProjection;
} camera;

layout(push_constant) uniform PushConstants {
    mat4 world;       // Position/rotation transform (no size scaling)
    vec4 tintColor;
    vec4 uvRect;      // (minU, minV, maxU, maxV)
    vec2 size;        // Element size in pixels (width, height)
    vec2 pivot;       // Relative base point (dx, dy) in the range [0,1]
};

void main() {
    // Compute local position in element space (relative to pivot, scaled by size)
    vec4 p = vec4((inPos - pivot) * size, 0, 1);

    // Apply model (world) and view-projection transforms so the element
    // is placed correctly in clip space. `world` contains position/rotation
    // (no size scaling) so we feed the sized local position as a vec4
    // with w=1.0 to allow translation.

    // gl_Position = p;
    // gl_Position = vec4(xy.x / 640.0, xy.y / 360.0, 0.0, 1.0);
    // gl_Position = camera.viewProjection * p;
    gl_Position = camera.viewProjection * world * p;
    
    // Transform vertex UVs from base geometry (0..1) to atlas UVs using push constant uvRect
    // inTexCoord is the base quad UV (0..1), uvRect defines the sub-rectangle in the atlas
    vec2 uvMin = uvRect.xy;
    vec2 uvMax = uvRect.zw;
    fragTexCoord = mix(uvMin, uvMax, inTexCoord);
    
    // Preserve provided alpha in tintColor (was previously forced to 1.0)
    fragTintColor = tintColor;
}