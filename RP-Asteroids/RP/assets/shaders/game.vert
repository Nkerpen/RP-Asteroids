#version 430 core

// ====================================================================
// SECAO 1: ENTRADA DE DADOS DO MODELO 3D
// Recebe a posicao, normal e coordenadas de textura originais da malha
// ====================================================================
layout(location=0) in vec3 v_Position;
layout(location=1) in vec3 v_Normal;
layout(location=2) in vec2 v_Uv;

// ====================================================================
// SECAO 2: SAIDA PARA O FRAGMENT SHADER
// Variaveis que serao passadas adiante para o calculo de luz e cor
// ====================================================================
out vec2 f_Uv;
out vec3 f_Normal;

// ====================================================================
// SECAO 3: VARIAVEIS GLOBAIS (UNIFORMS)
// Dados enviados pelo Game.cs a cada frame
// ====================================================================
uniform mat4 u_Model;
uniform mat4 u_View;
uniform mat4 u_Projection;
uniform float u_Time;

uniform int u_IsAsteroid;   
uniform int u_IsThrusting;

uniform int u_IsOutline;
uniform float u_OutlineThickness = 0.125; 

// ====================================================================
// SECAO 4: LOGICA DE TRANSFORMACAO E DEFORMACAO
// ====================================================================
void main() {
    f_Uv = v_Uv;
    
    // Calcula a direcao da normal no espaco tridimensional do mundo
    f_Normal = mat3(u_Model) * v_Normal;
    
    vec3 modifiedPosition = v_Position;

    // --- DEFORMACAO PROCEDURAL DOS ASTEROIDES (Geoide) ---
    if (u_IsAsteroid == 1) {
        float freq = 3.5; // Frequencia do ruido para criar amassados pequenos e grandes
        // Cria um ruido 3D baseado nas normais para amassar a esfera
        float bump = sin(v_Normal.x * freq) * cos(v_Normal.y * freq) * sin(v_Normal.z * freq);
        
        float localSize = length(v_Position);
        modifiedPosition += v_Normal * (bump * localSize * 0.4); 

        // Pulso de animacao continua
        float pulse = sin(u_Time * 4.0 + v_Position.x) * (localSize * 0.04);
        modifiedPosition += v_Normal * pulse;
    }

    // --- TREMOR DA NAVE AO ACELERAR ---
    if (u_IsAsteroid == 0 && u_IsThrusting == 1) {
        // Deforma apenas a parte de tras da nave (Y negativo)
        if (v_Position.y < 0.0) { 
            modifiedPosition.y -= abs(sin(u_Time * 25.0)) * 0.18;
            modifiedPosition.x *= 1.1; 
            modifiedPosition.z *= 1.1; 
        }
    }

    // --- TECNICA DE INVERSE HULL (OUTLINE) ---
    // Aumenta a escala do objeto a partir do seu centro geometrico
    if (u_IsOutline == 1) {
        modifiedPosition *= (1.0 + u_OutlineThickness);
    }

    // Calcula a posicao final projetada na tela do jogador
    gl_Position = vec4(modifiedPosition, 1.0) * u_Model * u_View * u_Projection;
}