#version 330 core

in vec2 TexCoord;
out vec4 FragColor;

uniform vec4 uBackgroundColor;
uniform float uFade;

void main()
{
    FragColor = vec4(uBackgroundColor.rgb, uBackgroundColor.a * uFade);
}