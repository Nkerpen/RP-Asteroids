#version 430 core

// ====================================================================
// SECAO 1: DADOS RECEBIDOS DO VERTEX SHADER
// ====================================================================
in vec2 f_Uv;
in vec3 f_Normal; 

// ====================================================================
// SECAO 2: SAIDA DE COR DO PIXEL
// ====================================================================
out vec4 out_Color;

// ====================================================================
// SECAO 3: VARIAVEIS DE ESTADO
// ====================================================================
uniform int u_IsOutline;

// ====================================================================
// SECAO 4: LOGICA DE PINTURA (PIXEL POR PIXEL)
// ====================================================================
void main() {
    // PASSO A: RENDERIZACAO DO CONTORNO
    if (u_IsOutline == 1) {
        // Pinta os pixels da malha expandida puramente de branco
        out_Color = vec4(1.0, 1.0, 1.0, 1.0); 
    } 
    // PASSO B: RENDERIZACAO DO MIOLO (LATARIA/ROCHA)
    else {
        // Define uma fonte de luz direcional imaginaria
        vec3 lightDir = normalize(vec3(0.5, 1.0, 0.5));
        vec3 normal = normalize(f_Normal);

        // O Produto Escalar (Dot) revela quanta luz atinge o pixel atual
        float lightIntensity = max(dot(normal, lightDir), 0.0);
        
        // Multiplica a luz por um valor baixo para criar o tom cinza muito escuro
        vec3 subtleLighting = vec3(lightIntensity * 0.15);

        // Aplica a cor final na tela
        out_Color = vec4(subtleLighting, 1.0);
    }
}