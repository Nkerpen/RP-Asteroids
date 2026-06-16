#version 430 core

in vec2 f_Uv;
out vec4 out_Color;

uniform vec3 u_ObjectColor = vec3(0.0, 0.0, 0.0); 
uniform int u_IsOutline;

void main() {
    if (u_IsOutline == 1) {
        out_Color = vec4(1.0, 1.0, 1.0, 1.0); 
    } else {
        out_Color = vec4(u_ObjectColor, 1.0); 
    }
}