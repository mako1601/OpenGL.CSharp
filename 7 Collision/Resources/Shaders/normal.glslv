#version 460 core

layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec2 aTexCoords;
layout (location = 3) in vec3 aTangent;
layout (location = 4) in vec3 aBitangent;

out VS_OUT {
    vec3 FragPosition;
    vec2 TexCoords;
    vec3 TangentLightPosition;
    vec3 TangentViewPosition;
    vec3 TangentFragPosition;
} vs_out;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

uniform vec3 uLightPosition;
uniform vec3 uViewPosition;

void main() {
    gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0);

    vs_out.FragPosition = vec3(uModel * vec4(aPosition, 1.0));   
    vs_out.TexCoords = aTexCoords;

    mat3 normalMatrix = transpose(inverse(mat3(uModel)));
    vec3 T = normalize(normalMatrix * aTangent);
    vec3 N = normalize(normalMatrix * aNormal);
    T = normalize(T - dot(T, N) * N);
    vec3 B = cross(N, T);

    mat3 TBN = transpose(mat3(T, B, N));    
    vs_out.TangentLightPosition = TBN * uLightPosition;
    vs_out.TangentViewPosition  = TBN * uViewPosition;
    vs_out.TangentFragPosition  = TBN * vs_out.FragPosition;
}