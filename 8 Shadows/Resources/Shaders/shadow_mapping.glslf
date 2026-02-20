#version 460 core

out vec4 fColor;

in VS_OUT {
    vec3 FragPosition;
    vec3 Normal;
    vec2 TexCoords;
    vec4 FragPositionLightSpace;
} fs_in;

uniform sampler2D uDiffuseTexture;
uniform sampler2D uShadowMap;

uniform vec3 uLightPosition;
uniform vec3 uViewPosition;

float ShadowCalculation(vec4 fragPositionLightSpace) {
    vec3 projCoords = fragPositionLightSpace.xyz / fragPositionLightSpace.w * 0.5 + 0.5;

    if(projCoords.z > 1.0 || projCoords.z < 0.0 ||
        projCoords.x < 0.0 || projCoords.x > 1.0 ||
        projCoords.y < 0.0 || projCoords.y > 1.0) {
        return 0.0;
    }

    float closestDepth = texture(uShadowMap, projCoords.xy).r;
    float currentDepth = projCoords.z;
    vec3 normal = normalize(fs_in.Normal);
    vec3 lightDir = normalize(uLightPosition - fs_in.FragPosition);
    // float bias = max(0.0015 * (1.0 - dot(normal, lightDir)), 0.00005);
    // bias = min(bias, 0.0015);
    float bias = max(0.0008 * (1.0 - dot(normal, lightDir)), 0.00002);
    // bias = min(bias, 0.0008);
    // float receiver = length(vec2(dFdx(currentDepth), dFdy(currentDepth)));
    // bias += receiver * 0.5;

    float shadow = currentDepth - bias > closestDepth ? 1.0 : 0.0;

    return shadow;
}

void main() {
    vec3 color = texture(uDiffuseTexture, fs_in.TexCoords).rgb;
    vec3 normal = normalize(fs_in.Normal);
    vec3 lightColor = vec3(1.0, 0.96, 0.88);

    // ambient
    vec3 ambient = 0.38 * lightColor;

    // diffuse
    vec3 lightDir = normalize(uLightPosition - fs_in.FragPosition);
    float diff = max(dot(lightDir, normal), 0.0);
    vec3 diffuse = diff * lightColor;

    // specular
    vec3 viewDir = normalize(uViewPosition - fs_in.FragPosition);
    float spec = 0.0;
    vec3 halfwayDir = normalize(lightDir + viewDir);
    spec = pow(max(dot(normal, halfwayDir), 0.0), 64.0);
    vec3 specular = spec * lightColor;

    // calculate shadow
    float shadow = ShadowCalculation(fs_in.FragPositionLightSpace);
    vec3 lighting = (ambient + (1.0 - shadow) * (diffuse + specular)) * color;

    // result
    fColor = vec4(lighting, 1.0);
}
