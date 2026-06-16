#version 430 core
layout(location=0)in vec3 v_Position;
layout(location=1)in vec3 v_Normal;
layout(location=2)in vec2 v_Uv;

out vec3 f_Normal;

uniform mat4 u_CameraInverseRotation;
uniform mat4 u_Projection;

void main() {
    f_Normal = v_Normal;

    gl_Position = vec4(v_Position, 1.0) * u_CameraInverseRotation * u_Projection;
}
