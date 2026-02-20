#version 460 core

in vec3 vPosition;
in vec3 vNormal;
in vec2 vTexCoords;

out vec4 fColor;

struct Material {
    sampler2D diffuse;
    sampler2D specular;
    float shininess;
};

struct Light {
    vec3 position;
    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
};

uniform Material uMaterial;
uniform Light uLight[4];
uniform vec3 uViewPosition;

uniform bool uUnlit;
uniform vec3 uColor;

vec3 CalcLight(Light light, vec3 normal, vec3 fragPos, vec3 viewDir) {
    vec3 lightDir = normalize(light.position - fragPos);

    // diffuse shading
    float diff = max(dot(normal, lightDir), 0.0);

    // specular shading
    vec3 reflectDir = reflect(-lightDir, normal);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), uMaterial.shininess);

    // attenuation
    float distance = length(light.position - fragPos);
    float attenuation = 1.0 / (1.0 + 0.09 * distance + 0.032 * (distance * distance));

    // combine results
    vec3 ambient  = light.ambient * vec3(texture(uMaterial.diffuse, vTexCoords));
    vec3 diffuse  = light.diffuse * diff * vec3(texture(uMaterial.diffuse, vTexCoords));
    vec3 specular = light.specular * spec * vec3(texture(uMaterial.specular, vTexCoords));

    ambient  *= attenuation;
    diffuse  *= attenuation;
    specular *= attenuation;

    return ambient + diffuse + specular;
}

void main() {
    vec3 normal = normalize(vNormal);
    vec3 viewDirection = normalize(uViewPosition - vPosition);

    vec3 result = vec3(0.0);
    for(int i = 0; i < 4; i++) {
        result += CalcLight(uLight[i], normal, vPosition, viewDirection);
    }

    // result
    fColor = vec4(result, 1.0);
}
