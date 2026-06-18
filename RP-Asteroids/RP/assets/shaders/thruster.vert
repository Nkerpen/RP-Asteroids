#version 430 core

// ====================================================================
// SECAO 1: ENTRADA DE DADOS DO MODELO 3D
// ====================================================================
layout(location=0) in vec3 v_Position;
layout(location=1) in vec3 v_Normal;
layout(location=2) in vec2 v_Uv; 

// ====================================================================
// SECAO 2: SAIDA PARA O FRAGMENT SHADER
// ====================================================================
out vec2 f_Uv;     
out vec3 f_Normal; 

// ====================================================================
// SECAO 3: VARIAVEIS GLOBAIS (UNIFORMS)
// ====================================================================
uniform mat4 u_Model;
uniform mat4 u_View;
uniform mat4 u_Projection; 

uniform float u_Time;
uniform float u_Drift; // Forca da inercia lateral calculada no Game.cs

uniform int u_IsOutline;
uniform float u_OutlineThickness = 0.15; 

// ====================================================================
// SECAO 4: LOGICA DE DEFORMACAO DO FOGO
// ====================================================================
void main() {
    // Comunicacao obrigatoria para evitar erros matematicos de pixel (NaN)
    f_Uv = v_Uv;
    f_Normal = mat3(u_Model) * v_Normal;

    vec3 pos = v_Position;

    // --- PASSO A: APLICA O OUTLINE ANTES DA FISICA ---
    // Expandimos o objeto enquanto ele ainda esta reto no eixo central
    if (u_IsOutline == 1) {
        pos *= (1.0 + u_OutlineThickness);
    }

    // --- PASSO B: DEFORMACAO NAO-LINEAR (CAUDA DINAMICA) ---
    // 1. Isolamos os vertices da metade inferior (longe da nave)
    float lowerHalf = max(0.0, -pos.y);
    
    // 2. Elevamos o valor ao quadrado (lowerHalf * lowerHalf)
    // Isso cria uma curva parabolica fluida e evita dobras duras na malha
    float bendFactor = (lowerHalf * lowerHalf) * u_Drift * 8.0;
    
    // 3. Aplicamos o arrasto apenas no eixo lateral (X)
    pos.x += bendFactor;

    // Projeta os vertices curvados na resolucao final da tela
    gl_Position = vec4(pos, 1.0) * u_Model * u_View * u_Projection;
}