#version 430 core

in vec2 f_Uv;
in vec3 f_Normal; // RECEBENDO A NORMAL

out vec4 out_Color;

uniform int u_IsOutline;

void main() {
    if(u_IsOutline == 1) {
        out_Color = vec4(1.0, 1.0, 1.0, 1.0); // Contorno branco brilhante
    } else {
        // Direcao de uma luz imaginaria (Vindo de cima, da direita e da frente)
        vec3 lightDir = normalize(vec3(0.5, 1.0, 0.5));

        // Garante que a normal esta perfeitamente alinhada
        vec3 normal = normalize(f_Normal);

        // Calcula a intensidade da luz batendo na face (de 0.0 a 1.0)
        float lightIntensity = max(dot(normal, lightDir), 0.0);

        // Multiplica por 0.25 para deixar a luz BEM fraca e sutil (apenas 25% de forca)
        vec3 subtleLighting = vec3(lightIntensity * 0.20);

        out_Color = vec4(subtleLighting, 1.0);
    }
}