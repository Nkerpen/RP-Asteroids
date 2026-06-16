#version 430 core

in vec3 f_Position;
in vec3 f_Normal;
in vec2 f_Uv;
in vec3 f_Color;

out vec4 out_Color;

uniform sampler2D u_Texture;
uniform vec3 u_Color = vec3(1, 0, 0);
uniform float u_Smoothness = 1.0;
uniform vec3 u_AmbientLight = vec3(0.1, 0.1, 0.2);
uniform vec3 u_DirectionalLightDirection = vec3(0, -1, 0);
uniform vec3 u_DirectionalLightColor = vec3(1);
uniform vec3 u_CameraPosition;

void main() {
    vec4 color = texture(u_Texture, f_Uv);
    color.rgb *= u_Color * f_Color;

    if (color.a < 0.5) {
        discard;
    }

    vec3 normal = normalize(f_Normal);

    float lightIntensity = max(dot(u_DirectionalLightDirection, -normal), 0.0);
    lightIntensity *= sqrt(max(-u_DirectionalLightDirection.y, 0.0));// luz direcional nao ilumina de noite
    vec3 diffuse = u_DirectionalLightColor * lightIntensity;

    vec3 reflectDirection = reflect(u_DirectionalLightDirection, normal);
    vec3 toCameraDirection = normalize(u_CameraPosition - f_Position);
    float specularIntensity = lightIntensity * u_Smoothness * pow(max(dot(reflectDirection, toCameraDirection), 0.0), max(32.0 * u_Smoothness, 1.0));

    vec3 specular = specularIntensity * u_DirectionalLightColor;

    out_Color = vec4(color.rgb * (u_AmbientLight + diffuse) + specular, color.a);
}