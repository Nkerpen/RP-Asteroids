#version 430 core

in vec2 f_Uv;
out vec4 out_Color;

uniform sampler2D u_Texture;
uniform float u_Time;

// Funcao matematica para distorcer as coordenadas com base em uma lente
vec2 applyCrtCurvature(vec2 uv) {
    uv = uv * 2.0 - 1.0; 

    // AJUSTE DE CURVATURA: Aumentar os divisores deixa a tela mais plana. 
    // Mudei de 4.5 e 3.5 para 8.0 e 7.0.
    uv.x *= 1.0 + pow(abs(uv.y) / 5.5, 2.0);
    uv.y *= 1.0 + pow(abs(uv.x) / 4.0, 2.0);

    uv = uv * 0.5 + 0.5;
    return uv;
}

void main() {
    vec2 curvedUv = applyCrtCurvature(f_Uv);

    // Cria as bordas pretas fisicas (vignette)
    if(curvedUv.x < 0.0 || curvedUv.x > 1.0 || curvedUv.y < 0.0 || curvedUv.y > 1.0) {
        out_Color = vec4(0.0, 0.0, 0.0, 1.0);
        return;
    }

    vec4 sceneColor = texture(u_Texture, curvedUv);

    // AJUSTE DE SCANLINES
    // Se quiser menos linhas horizontais, diminua o numero apos o "y *". Aumentar o multiplicador (0.04) deixa as linhas mais escuras.
    float scanlinePattern = sin(curvedUv.y * 750.0) * 0.02;
    sceneColor.rgb -= scanlinePattern;

    // AJUSTE DE CINTILACAO (FLICKER)
    sceneColor.rgb *= 0.995 + 0.02 * sin(u_Time * 140.0);

    out_Color = sceneColor;
}