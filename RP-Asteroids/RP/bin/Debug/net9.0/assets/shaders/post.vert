#version 430 core

layout(location = 0) in vec3 v_Position;
layout(location = 1) in vec3 v_Normal;
layout(location = 2) in vec2 v_Uv;

out vec2 f_Uv;

void main() {
    f_Uv = v_Uv;

    gl_Position = vec4(v_Position, 1.0);
}
