#version 430 core

layout(location=0)in vec3 v_Position;
layout(location=1)in vec3 v_Normal;
layout(location=2)in vec2 v_Uv;

out vec3 f_Position;
out vec3 f_Normal;
out vec2 f_Uv;
out vec3 f_LightPosition;

uniform mat4 u_Model;
uniform mat4 u_View;
uniform mat4 u_Projection;
uniform mat4 u_Light;
uniform mat4 u_Rotation;

void main() {
    f_Position = (vec4(v_Position, 1.0) * u_Model).xyz;
    f_Normal = (vec4(v_Normal, 1.0) * u_Rotation).xyz;
    f_Uv = v_Uv;
    f_LightPosition = ((vec4(v_Position, 1) * u_Model * u_Light).xyz + vec3(1)) / 2.0;

    gl_Position = vec4(v_Position, 1.0) * u_Model * u_View * u_Projection;
}
