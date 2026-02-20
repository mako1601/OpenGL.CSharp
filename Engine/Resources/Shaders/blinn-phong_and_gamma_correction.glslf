#version 460 core

in vec3 vPosition;
in vec3 vNormal;
in vec2 vTexCoords;

out vec4 fColor;

struct Material {
    sampler2D diffuse;
    float shininess;
};

struct Light {
    vec3 position;
};

uniform Material uMaterial;
uniform Light uLight;
uniform vec3 uViewPosition;
uniform float uGamma;
uniform bool uBlinnPhong;

void main() {
    vec3 normal = normalize(vNormal);
    vec3 color = texture(uMaterial.diffuse, vTexCoords).rgb;

    // diffuse
    vec3 lightDirection = normalize(uLight.position - vPosition);
    float diff = max(dot(lightDirection, normal), 0.0);
    vec3 diffuse = diff * color;

    // specular
    vec3 viewDirection = normalize(uViewPosition - vPosition);

    // for phong
    float spec;
    if(uBlinnPhong) {
        vec3 halfwayDirection = normalize(lightDirection + viewDirection);
        spec = pow(max(dot(normal, halfwayDirection), 0.0), uMaterial.shininess);
    } else {
        vec3 reflectDirection = reflect(-lightDirection, normal);
        spec = pow(max(dot(viewDirection, reflectDirection), 0.0), uMaterial.shininess);
    }

    vec3 specular = spec * color;

    // attenuation
    float distance = length(uLight.position - vPosition);
    float attenuation = 1.0 / (distance * distance);

    diffuse  *= attenuation;
    specular *= attenuation;
    color    *= vec3(diffuse + specular);

    // result
    color = pow(color, vec3(1.0 / uGamma));
    fColor = vec4(color, 1.0);
}
