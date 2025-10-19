#version 450

layout(location = 0) in vec2 fragPos;
layout(location = 0) out vec4 outColor;

// Uniform buffer: gradient definition (updated when gradient changes)
layout(binding = 0) uniform GradientUBO {
    vec4 colors[32];      // Color at each stop
    float positions[32];  // Position of each stop (0.0 to 1.0)
    int stopCount;        // Number of active stops (2-32)
} gradient;

// Push constants: animation parameters (updated every frame)
layout(push_constant) uniform PushConstants {
    vec2 center;          // Center point in NDC
    float radius;         // Gradient radius
    float padding;
} pc;

// Sample the gradient at position t (0.0 to 1.0)
vec4 sampleGradient(float t) {
    // Clamp to valid range
    t = clamp(t, 0.0, 1.0);
    
    // Handle edge cases
    if (gradient.stopCount < 2) {
        return gradient.colors[0];
    }
    
    // If t is before first stop, use first color
    if (t <= gradient.positions[0]) {
        return gradient.colors[0];
    }
    
    // If t is after last stop, use last color
    int lastIndex = gradient.stopCount - 1;
    if (t >= gradient.positions[lastIndex]) {
        return gradient.colors[lastIndex];
    }
    
    // Find the two stops that surround t
    for (int i = 0; i < gradient.stopCount - 1; i++) {
        float pos1 = gradient.positions[i];
        float pos2 = gradient.positions[i + 1];
        
        if (t >= pos1 && t <= pos2) {
            // Interpolate between the two colors
            float localT = (t - pos1) / (pos2 - pos1);
            return mix(gradient.colors[i], gradient.colors[i + 1], localT);
        }
    }
    
    // Fallback (should never reach here)
    return gradient.colors[0];
}

void main() {
    // Convert fragPos from NDC [-1, 1] to normalized coordinates [0, 1]
    // Note: Flip Y for Vulkan's coordinate system (Y increases downward)
    vec2 normalizedPos = vec2(
        (fragPos.x + 1.0) * 0.5,
        (-fragPos.y + 1.0) * 0.5
    );
    
    // Calculate distance from center (both in normalized [0,1] space)
    float dist = distance(normalizedPos, pc.center);
    
    // Normalize distance by radius to get t value [0, 1]
    float t = dist / pc.radius;
    
    // Sample gradient and output color
    outColor = sampleGradient(t);
}
