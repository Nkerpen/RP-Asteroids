#version 430 core

in vec2 f_Uv;
out vec4 out_Color;

uniform sampler2D u_Texture;
uniform float u_Time;

// Funcao matematica para distorcer as coordenadas com base em uma lente
vec2 applyCrtCurvature(vec2 uv) {
    uv = uv * 2.0 - 1.0; 
    uv.x *= 1.0 + pow(abs(uv.y) / 4.5, 2.0);
    uv.y *= 1.0 + pow(abs(uv.x) / 3.5, 2.0);
    uv = uv * 0.5 + 0.5; 
    return uv;
}

void main() {
    vec2 curvedUv = applyCrtCurvature(f_Uv);

    // Cria as bordas pretas fisicas
    if (curvedUv.x < 0.0 || curvedUv.x > 1.0 || curvedUv.y < 0.0 || curvedUv.y > 1.0) {
        out_Color = vec4(0.0, 0.0, 0.0, 1.0);
        return;
    }

    vec4 sceneColor = texture(u_Texture, curvedUv);

    // Linhas de Varredura (Scanlines)
    float scanlinePattern = sin(curvedUv.y * 750.0) * 0.12;
    sceneColor.rgb -= scanlinePattern;

    // Ruido analogico e cintilacao
    sceneColor.rgb *= 0.98 + 0.02 * sin(u_Time * 140.0);

    out_Color = sceneColor;
}