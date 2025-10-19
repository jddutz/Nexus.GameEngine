#version 450

layout(location = 0) in vec2 fragPos;
layout(location = 0) out vec4 outColor;

// Uniform buffer: 4 corner colors
layout(binding = 0) uniform CornerColors {
    vec4 topLeft;
    vec4 topRight;
    vec4 bottomLeft;
    vec4 bottomRight;
} colors;

void main() {
    // Convert from NDC [-1, 1] to UV [0, 1]
    // NDC: top-left = (-1, -1), bottom-right = (1, 1)
    // UV: top-left = (0, 0), bottom-right = (1, 1)
    vec2 uv = (fragPos + 1.0) * 0.5;
    
    // Bilinear interpolation:
    // 1. Interpolate top edge (topLeft -> topRight)
    vec4 topColor = mix(colors.topLeft, colors.topRight, uv.x);
    
    // 2. Interpolate bottom edge (bottomLeft -> bottomRight)
    vec4 bottomColor = mix(colors.bottomLeft, colors.bottomRight, uv.x);
    
    // 3. Interpolate between top and bottom
    outColor = mix(topColor, bottomColor, uv.y);
}
