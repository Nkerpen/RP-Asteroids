#version 430 core

// ====================================================================
// SECAO 1: ENTRADA E SAIDA
// ====================================================================
in vec2 f_Uv;
out vec4 out_Color;

// ====================================================================
// SECAO 2: VARIAVEIS GLOBAIS (UNIFORMS)
// ====================================================================
uniform sampler2D u_Texture;
uniform float u_Time;

// ====================================================================
// SECAO 3: FUNCOES DE DEFORMACAO DA TELA
// ====================================================================
vec2 applyCrtCurvature(vec2 uv) {
    // Move o centro da tela para 0.0 para facilitar a matematica de curva
    uv = uv * 2.0 - 1.0;
    
    // Curva as bordas usando potencia
    uv.x *= 1.0 + pow(abs(uv.y) / 4.5, 2.0);
    uv.y *= 1.0 + pow(abs(uv.x) / 3.5, 2.0);
    
    // Retorna para o espaco de coordenadas de textura (0.0 a 1.0)
    uv = uv * 0.5 + 0.5; 
    return uv;
}

// ====================================================================
// SECAO 4: RENDERIZACAO FINAL (CRT + ABERRACAO CROMATICA)
// ====================================================================
void main() {
    // 1. Aplica a curvatura da TV de tubo
    vec2 curvedUv = applyCrtCurvature(f_Uv);
    
    // Corta os pixels que foram repuxados para fora da tela (bordas pretas arredondadas)
    if (curvedUv.x < 0.0 || curvedUv.x > 1.0 || curvedUv.y < 0.0 || curvedUv.y > 1.0) {
        out_Color = vec4(0.0, 0.0, 0.0, 1.0);
        return;
    }

    // --- NOVO: ABERRACAO CROMATICA ---
    // Encontra o vetor que aponta do centro da tela para o pixel atual
    vec2 dirFromCenter = curvedUv - vec2(0.5);
    
    // Forca da aberracao. Aumente para separar mais as cores!
    float chromaticIntensity = 0.0035; 
    
    // Calcula coordenadas UV separadas para cada cor
    vec2 uvR = curvedUv + dirFromCenter * chromaticIntensity;       // Empurra o Vermelho para fora
    vec2 uvG = curvedUv;                                            // Mantem o Verde no lugar original
    vec2 uvB = curvedUv - dirFromCenter * chromaticIntensity;       // Puxa o Azul para dentro

    // Amostra a textura 3 vezes usando as coordenadas separadas
    float r = texture(u_Texture, uvR).r;
    float g = texture(u_Texture, uvG).g;
    float b = texture(u_Texture, uvB).b;
    
    // Monta a cor final a partir dos canais separados
    vec4 sceneColor = vec4(r, g, b, 1.0);

    // --- EFEITOS DE CRT ORIGINAIS ---
    // Aplica as linhas de escaneamento (Scanlines)
    float scanlinePattern = sin(curvedUv.y * 750.0) * 0.02;
    sceneColor.rgb -= scanlinePattern;

    // Aplica o piscar (Flicker) subtil da tela
    sceneColor.rgb *= 0.995 + 0.005 * sin(u_Time * 140.0);

    // Entrega a cor final para o monitor
    out_Color = sceneColor;
}