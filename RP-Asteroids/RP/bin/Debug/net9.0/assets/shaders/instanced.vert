#version 430 core

layout(location=0)in vec3 v_Position;
layout(location=1)in vec3 v_Normal;
layout(location=2)in vec2 v_Uv;
layout(location=3)in mat4 i_Model;
layout(location=7)in vec3 i_Color;

out vec3 f_Position;
out vec3 f_Normal;
out vec2 f_Uv;
out vec3 f_Color;

uniform mat4 u_View;
uniform mat4 u_Projection;
uniform mat4 u_Light;
uniform float u_Time;

void main() {
    float sway = 0.05;

    vec3 position = v_Position;
    position.x += sin(u_Time) * v_Position.y * sway;
    position.z += cos(u_Time) * v_Position.y * sway;

    f_Position = (vec4(position, 1.0) * i_Model).xyz;
    f_Normal = v_Normal;
    f_Uv = v_Uv;
    f_Color = i_Color;

    gl_Position = vec4(position, 1.0) * i_Model * u_View * u_Projection;
}
