#version 430 core

layout(location = 0) in vec3 v_Position;
layout(location = 1) in vec3 v_Normal;

uniform mat4 u_Model;
uniform mat4 u_View;
uniform mat4 u_Projection;
uniform float u_Time;
uniform float u_Drift; // O quanto a chama dobra

// Adicionamos estas duas variaveis de controle de Outline
uniform int u_IsOutline;
uniform float u_OutlineThickness = 0.125;

void main() {
    vec3 pos = v_Position;

    // 1. DEFORMACAO (Dobra da ponta)
    float bendFactor = (0.5 - pos.y) * u_Drift * 1.5;
    pos.x += bendFactor * 0.5;

    // 2. LOGICA DE OUTLINE (A parte que estava faltando!)
    if(u_IsOutline == 1) {
        // Escala o vertice para fora para criar o contorno
        pos *= (1.0 + u_OutlineThickness);
    }

    gl_Position = vec4(pos, 1.0) * u_Model * u_View * u_Projection;
}