#version 460 core

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aNormal;
layout(location = 2) in vec2 aTexCoords;

out VS_OUT {
    vec3 FragPosition;
    vec3 Normal;
    vec2 TexCoords;
    vec4 FragPositionLightSpace;
} vs_out;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;
uniform mat4 uLightSpaceMatrix;

void main() {
    vs_out.FragPosition           = vec3(uModel * vec4(aPosition, 1.0));
    vs_out.Normal                 = transpose(inverse(mat3(uModel))) * aNormal;
    vs_out.TexCoords              = aTexCoords;
    vs_out.FragPositionLightSpace = uLightSpaceMatrix * vec4(vs_out.FragPosition, 1.0);

    gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0);
}
