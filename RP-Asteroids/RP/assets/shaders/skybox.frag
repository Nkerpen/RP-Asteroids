#version 460 core

in vec3 f_Normal;

out vec4 out_Color;

uniform vec3 u_DirectionalLightDirection;

void main() {
    vec3 groundColor = vec3(0.2, 0.6, 0.2);
    vec3 skyColor = vec3(0.6, 0.8, 1.0);
    vec3 horizonColor = vec3(0.6, 0.9, 0.9);

    vec3 dayColor = vec3(1.0);
    vec3 nightColor = vec3(0.05, 0.1, 0.4);
    vec3 twilightColor = vec3(1.4, 0.8, 0.5);

    if (f_Normal.y > 0) {
        out_Color = vec4(mix(horizonColor, skyColor, sqrt(f_Normal.y)), 1.0);

        float sunSize = 0.03;
        vec3 normal = normalize(f_Normal);
        float sunIntensity = max(dot(normal, -u_DirectionalLightDirection) - (1.0 - sunSize), 0.0) / sunSize;
        out_Color.rgb += vec3(sunIntensity);
    }
    else {
        out_Color = vec4(groundColor, 1.0);
    }

    float alpha = sqrt(abs(u_DirectionalLightDirection.y));
    vec3 tint = u_DirectionalLightDirection.y < 0 ? mix(twilightColor, dayColor, alpha) : mix(twilightColor, nightColor, alpha);
    out_Color.rgb *= tint;
}
