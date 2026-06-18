#version 430 core

layout(location=0) in vec3 v_Position;
layout(location=1) in vec3 v_Normal;
layout(location=2) in vec2 v_Uv; // Puxa a UV da malha

out vec2 f_Uv;     // Envia para o game.frag
out vec3 f_Normal; // Envia para o game.frag

uniform mat4 u_Model;
uniform mat4 u_View;
// A variavel abaixo estava digitada incorretamente no seu codigo original
uniform mat4 u_Projection; 
uniform float u_Time;
uniform float u_Drift; 

uniform int u_IsOutline;
uniform float u_OutlineThickness = 0.15; 

void main() {
    // Comunicacao com o Fragment Shader
    f_Uv = v_Uv;
    f_Normal = mat3(u_Model) * v_Normal;

    vec3 pos = v_Position;

    // 1. APLICA O OUTLINE PRIMEIRO
    if (u_IsOutline == 1) {
        pos *= (1.0 + u_OutlineThickness);
    }

    // 2. DEFORMACAO NAO-LINEAR SUAVE (Curva Parabolica)
    float lowerHalf = max(0.0, -pos.y);
    float bendFactor = (lowerHalf * lowerHalf) * u_Drift * 8.0;
    
    pos.x += bendFactor;

    // Multiplicacao final usando a variavel correta: u_Projection
    gl_Position = vec4(pos, 1.0) * u_Model * u_View * u_Projection;
}