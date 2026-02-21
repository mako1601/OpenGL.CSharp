#version 460 core

out vec4 fColor;

in VS_OUT {
    vec3 FragPosition;
    vec3 Normal;
    vec2 TexCoords;
} fs_in;

uniform sampler2D uDiffuseTexture;
uniform sampler2DArray uShadowMapArray;

uniform vec3 uLightDirection;
uniform vec3 uViewPosition;
uniform mat4 uCascadeView;

uniform int uCascadeCount;
uniform mat4 uLightSpaceMatrices[4];
uniform float uCascadeSplits[4];
uniform int uShowCascadeColors;

vec3 CascadeDebugColor(int cascadeIndex) {
    switch(cascadeIndex) {
        case 0: return vec3(1.0, 0.25, 0.15);
        case 1: return vec3(1.0, 0.75, 0.15);
        case 2: return vec3(0.25, 0.95, 0.25);
        default: return vec3(0.25, 0.6, 1.0);
    }
}

int SelectCascadeIndex(float viewDepth) {
    for(int i = 0; i < uCascadeCount; ++i) {
        if(viewDepth < uCascadeSplits[i]) {
            return i;
        }
    }

    return uCascadeCount - 1;
}

float ShadowCalculation(int cascadeIndex, vec3 normal, vec3 lightDir) {
    vec4 fragPositionLightSpace = uLightSpaceMatrices[cascadeIndex] * vec4(fs_in.FragPosition, 1.0);
    vec3 projCoords = fragPositionLightSpace.xyz / fragPositionLightSpace.w * 0.5 + 0.5;

    if(projCoords.z > 1.0 || projCoords.z < 0.0 ||
        projCoords.x < 0.0 || projCoords.x > 1.0 ||
        projCoords.y < 0.0 || projCoords.y > 1.0) {
        return 0.0;
    }

    float closestDepth = texture(uShadowMapArray, vec3(projCoords.xy, float(cascadeIndex))).r;
    float currentDepth = projCoords.z;
    float bias = max(0.0008 * (1.0 - dot(normal, lightDir)), 0.00002);

    return currentDepth - bias > closestDepth ? 1.0 : 0.0;
}

void main() {
    vec3 color = texture(uDiffuseTexture, fs_in.TexCoords).rgb;
    vec3 normal = normalize(fs_in.Normal);
    vec3 lightColor = vec3(1.0, 0.96, 0.88);

    vec3 ambient = 0.38 * lightColor;

    vec3 lightDir = normalize(-uLightDirection);
    float diff = max(dot(lightDir, normal), 0.0);
    vec3 diffuse = diff * lightColor;

    vec3 viewDir = normalize(uViewPosition - fs_in.FragPosition);
    vec3 halfwayDir = normalize(lightDir + viewDir);
    float spec = pow(max(dot(normal, halfwayDir), 0.0), 64.0);
    vec3 specular = spec * lightColor;

    float viewDepth = abs((uCascadeView * vec4(fs_in.FragPosition, 1.0)).z);
    int cascadeIndex = SelectCascadeIndex(viewDepth);
    float shadow = ShadowCalculation(cascadeIndex, normal, lightDir);

    vec3 lighting = (ambient + (1.0 - shadow) * (diffuse + specular)) * color;
    if(uShowCascadeColors != 0) {
        vec3 cascadeColor = CascadeDebugColor(cascadeIndex);
        lighting = mix(lighting, cascadeColor, 0.65);
    }

    fColor = vec4(lighting, 1.0);
}
