#version 460 core

out vec4 fColor;

in vec2 vTexCoords;

uniform sampler2DArray uDepthMap;
uniform int uCascadeLayer;
uniform float uNearPlane;
uniform float uFarPlane;

float LinearizeDepth(float depth) {
    float z = depth * 2.0 - 1.0;
    return (2.0 * uNearPlane * uFarPlane) / (uFarPlane + uNearPlane - z * (uFarPlane - uNearPlane));
}

void main() {
    float depthValue = texture(uDepthMap, vec3(vTexCoords, float(uCascadeLayer))).r;
    // fColor = vec4(vec3(LinearizeDepth(depthValue) / uFarPlane), 1.0); // perspective
    fColor = vec4(vec3(depthValue), 1.0); // orthographic
}
