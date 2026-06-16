#version 430 core

in vec2 f_Uv;
out vec4 out_Color;

uniform sampler2D u_Texture;
uniform float u_Time;

vec2 applyCrtCurvature(vec2 uv) {
    uv = uv * 2.0 - 1.0; 
    
    uv.x *= 1.0 + pow(abs(uv.y) / 4.5, 2.0);
    uv.y *= 1.0 + pow(abs(uv.x) / 3.5, 2.0);
    
    uv = uv * 0.5 + 0.5; 
    return uv;
}

void main() {
    vec2 curvedUv = applyCrtCurvature(f_Uv);

    if (curvedUv.x < 0.0 || curvedUv.x > 1.0 || curvedUv.y < 0.0 || curvedUv.y > 1.0) {
        out_Color = vec4(0.0, 0.0, 0.0, 1.0);
        return;
    }

    vec4 sceneColor = texture(u_Texture, curvedUv);

    float scanlinePattern = sin(curvedUv.y * 750.0) * 0.02;
    sceneColor.rgb -= scanlinePattern;

    sceneColor.rgb *= 0.995 + 0.005 * sin(u_Time * 140.0);

    out_Color = sceneColor;
}