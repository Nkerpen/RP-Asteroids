#version 430 core

layout(location=0) in vec3 v_Position;
layout(location=1) in vec3 v_Normal;
layout(location=2) in vec2 v_Uv;

out vec2 f_Uv;

uniform mat4 u_Model;
uniform mat4 u_View;
uniform mat4 u_Projection;
uniform float u_Time;

uniform int u_IsAsteroid;   
uniform int u_IsThrusting;  

uniform int u_IsOutline;
uniform float u_OutlineThickness = 0.15; 

void main() {
    f_Uv = v_Uv;
    vec3 modifiedPosition = v_Position;

    // 1. DEFORMACAO BASE (Geoides e Nave)
    if (u_IsAsteroid == 1) {
        float bump = sin(v_Position.x * 5.0) * cos(v_Position.y * 5.0) * sin(v_Position.z * 5.0);
        modifiedPosition += v_Normal * (bump * 0.25); 

        float pulse = sin(u_Time * 4.0 + v_Position.x) * 0.02;
        modifiedPosition += v_Normal * pulse;
    }

    if (u_IsAsteroid == 0 && u_IsThrusting == 1) {
        if (v_Position.y < 0.0) { 
            modifiedPosition.y -= abs(sin(u_Time * 25.0)) * 0.18;
            modifiedPosition.x *= 1.1; 
            modifiedPosition.z *= 1.1; 
        }
    }

    // 2. CORRECAO DO OUTLINE
    if (u_IsOutline == 1) {
        modifiedPosition *= (1.0 + u_OutlineThickness);
        modifiedPosition.z -= 0.1; 
    }

    gl_Position = vec4(modifiedPosition, 1.0) * u_Model * u_View * u_Projection;
}