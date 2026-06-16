#version 430 core

in vec2 f_Uv;
in vec3 f_Normal; 

out vec4 out_Color;

uniform int u_IsOutline;
uniform int u_IsThruster; // NOVO: Controle da cor do fogo

void main() {
    if (u_IsOutline == 1) {
        out_Color = vec4(1.0, 1.0, 1.0, 1.0); 
    } else {
        if (u_IsThruster == 1) {
            // Se for o propulsor, pinta de Laranja Escaldante brilhante
            out_Color = vec4(1.0, 0.4, 0.0, 1.0); 
        } else {
            vec3 lightDir = normalize(vec3(0.5, 1.0, 0.5));
            vec3 normal = normalize(f_Normal);

            float lightIntensity = max(dot(normal, lightDir), 0.0);
            vec3 subtleLighting = vec3(lightIntensity * 0.15);

            out_Color = vec4(subtleLighting, 1.0);
        }
    }
}