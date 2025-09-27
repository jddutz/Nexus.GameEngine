namespace Nexus.GameEngine.Graphics.Resources;

/// <summary>
/// Static shader resource definitions for commonly used shaders.
/// </summary>
public static class Shaders
{
    /// <summary>
    /// Solid color background shader for BackgroundLayer components.
    /// Supports tinting, saturation adjustment, and fade effects.
    /// </summary>
    public static readonly ShaderDefinition BackgroundSolid = new()
    {
        Name = "BackgroundSolid",
        IsPersistent = true,  // Core shader used by many background components - never purge
        VertexSource = @"
#version 330 core
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec2 aTexCoord;
out vec2 TexCoord;
void main() {
    gl_Position = vec4(aPosition, 1.0);
    TexCoord = aTexCoord;
}",
        FragmentSource = @"
#version 330 core
out vec4 FragColor;
uniform vec4 uBackgroundColor;
uniform vec4 uTint;
uniform float uSaturation;
uniform float uFade;
void main() {
    vec4 color = uBackgroundColor * uTint;
    vec3 gray = vec3(dot(color.rgb, vec3(0.299, 0.587, 0.114)));
    color.rgb = mix(gray, color.rgb, uSaturation);
    FragColor = vec4(color.rgb, color.a * uFade);
}"
    };

    /// <summary>
    /// Textured background shader for BackgroundLayer components with image assets.
    /// Supports UV scaling, offset, tinting, saturation, and fade effects.
    /// </summary>
    public static readonly ShaderDefinition BackgroundTexture = new()
    {
        Name = "BackgroundTexture",
        IsPersistent = true,  // Core shader used by many background components - never purge
        VertexSource = @"
#version 330 core
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec2 aTexCoord;
out vec2 TexCoord;
uniform vec2 uTextureScale;
uniform vec2 uTextureOffset;
void main() {
    gl_Position = vec4(aPosition, 1.0);
    TexCoord = aTexCoord * uTextureScale + uTextureOffset;
}",
        FragmentSource = @"
#version 330 core
out vec4 FragColor;
in vec2 TexCoord;
uniform sampler2D uTexture;
uniform vec4 uTint;
uniform float uSaturation;
uniform float uFade;
void main() {
    vec4 texColor = texture(uTexture, TexCoord);
    vec4 color = texColor * uTint;
    vec3 gray = vec3(dot(color.rgb, vec3(0.299, 0.587, 0.114)));
    color.rgb = mix(gray, color.rgb, uSaturation);
    FragColor = vec4(color.rgb, color.a * uFade);
}"
    };

    /// <summary>
    /// Basic sprite shader for 2D sprite rendering with transformation matrix.
    /// Supports world transformation and color tinting.
    /// </summary>
    public static readonly ShaderDefinition BasicSprite = new()
    {
        Name = "BasicSprite",
        IsPersistent = true,  // Core sprite shader used by many components - never purge
        VertexSource = @"
#version 330 core
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec2 aTexCoord;
out vec2 TexCoord;
uniform mat4 uTransform;
void main() {
    gl_Position = uTransform * vec4(aPosition, 1.0);
    TexCoord = aTexCoord;
}",
        FragmentSource = @"
#version 330 core
out vec4 FragColor;
in vec2 TexCoord;
uniform sampler2D uTexture;
uniform vec4 uTint;
void main() {
    FragColor = texture(uTexture, TexCoord) * uTint;
}"
    };

    /// <summary>
    /// UI shader for user interface elements with orthographic projection.
    /// Optimized for UI rendering with alpha blending support.
    /// </summary>
    public static readonly ShaderDefinition UIShader = new()
    {
        Name = "UIShader",
        IsPersistent = true,  // Core UI shader - never purge
        VertexSource = @"
#version 330 core
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec2 aTexCoord;
out vec2 TexCoord;
uniform mat4 uProjection;
uniform mat4 uTransform;
void main() {
    gl_Position = uProjection * uTransform * vec4(aPosition, 1.0);
    TexCoord = aTexCoord;
}",
        FragmentSource = @"
#version 330 core
out vec4 FragColor;
in vec2 TexCoord;
uniform sampler2D uTexture;
uniform vec4 uColor;
uniform float uOpacity;
void main() {
    vec4 texColor = texture(uTexture, TexCoord);
    FragColor = vec4(texColor.rgb * uColor.rgb, texColor.a * uColor.a * uOpacity);
}"
    };

    /// <summary>
    /// Basic shader fallback for components that don't specify a specific shader.
    /// Simple vertex transformation with color output.
    /// </summary>
    public static readonly ShaderDefinition BasicShader = new()
    {
        Name = "BasicShader",
        IsPersistent = true,  // Fallback shader - never purge
        VertexSource = @"
#version 330 core
layout (location = 0) in vec3 aPosition;
uniform mat4 uTransform;
void main() {
    gl_Position = uTransform * vec4(aPosition, 1.0);
}",
        FragmentSource = @"
#version 330 core
out vec4 FragColor;
uniform vec4 uColor;
void main() {
    FragColor = uColor;
}"
    };
}