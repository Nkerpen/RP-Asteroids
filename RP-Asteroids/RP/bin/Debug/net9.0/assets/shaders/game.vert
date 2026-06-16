#version 430 core

layout(location=0) in vec3 v_Position;
layout(location=1) in vec3 v_Normal;
layout(location=2) in vec2 v_Uv;

out vec2 f_Uv;
out vec3 f_Normal;

uniform mat4 u_Model;
uniform mat4 u_View;
uniform mat4 u_Projection;
uniform float u_Time;

uniform int u_IsAsteroid;   
uniform int u_IsThrusting;  

uniform int u_IsOutline;
uniform float u_OutlineThickness = 0.125; 

void main() {
    f_Uv = v_Uv;
    f_Normal = mat3(u_Model) * v_Normal;
    
    vec3 modifiedPosition = v_Position;

    // 1. DEFORMACAO BASE
    if (u_IsAsteroid == 1) {
        float freq = 3.5;
        float bump = sin(v_Normal.x * freq) * cos(v_Normal.y * freq) * sin(v_Normal.z * freq);
        
        float localSize = length(v_Position);

        modifiedPosition += v_Normal * (bump * localSize * 0.4); 

        float pulse = sin(u_Time * 4.0 + v_Position.x) * (localSize * 0.04);
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
    }

    gl_Position = vec4(modifiedPosition, 1.0) * u_Model * u_View * u_Projection;
}