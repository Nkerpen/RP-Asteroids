#version 430 core

layout(location=0) in vec3 v_Position;
layout(location=1) in vec3 v_Normal;

uniform mat4 u_Model;
uniform mat4 u_View;
uniform mat4 u_Projection;
uniform float u_Time;
uniform float u_Drift; 

uniform int u_IsOutline;
uniform float u_OutlineThickness = 0.15; 

void main() {
    vec3 pos = v_Position;

    // 1. DEFORMACAO NAO-LINEAR (Dobra a partir do meio)
    // A metade de cima (y > 0.0) retorna 0 e fica rigida.
    // A metade de baixo (y < 0.0) gera um valor positivo e entorta.
    // Multiplicamos por 4.0 para manter a mesma intensidade maxima na ponta.
    float bendFactor = max(0.0, pos.y) * u_Drift * 4.0;
    
    pos.x += bendFactor * 0.5;

    // 2. LOGICA DE OUTLINE
    if (u_IsOutline == 1) {
        pos *= (1.0 + u_OutlineThickness);
    }

    gl_Position = vec4(pos, 1.0) * u_Model * u_View * u_Projection;
}