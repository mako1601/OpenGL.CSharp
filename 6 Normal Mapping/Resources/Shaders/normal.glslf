#version 460 core

out vec4 fColor;

in VS_OUT {
    vec3 FragPosition;
    vec2 TexCoords;
    vec3 TangentLightPosition;
    vec3 TangentViewPosition;
    vec3 TangentFragPosition;
} fs_in;

struct Material {
    sampler2D diffuse;
    sampler2D normal;
    float shininess;
};

struct Light {
    vec3 ambient;
    vec3 specular;
};

uniform Material uMaterial;
uniform Light uLight;

void main() {
    // obtain normal from normal map in range [0,1]
    vec3 normal = texture(uMaterial.normal, fs_in.TexCoords).rgb;
    // transform normal vector to range [-1,1]
    normal = normalize(normal * 2.0 - 1.0);  // this normal is in tangent space
   
    // get diffuse color
    vec3 color = texture(uMaterial.diffuse, fs_in.TexCoords).rgb;

    // ambient
    vec3 ambient = uLight.ambient * color;

    // diffuse
    vec3 lightDirection = normalize(fs_in.TangentLightPosition - fs_in.TangentFragPosition);
    float diff = max(dot(lightDirection, normal), 0.0);
    vec3 diffuse = diff * color;

    // specular
    vec3 viewDirection = normalize(fs_in.TangentViewPosition - fs_in.TangentFragPosition);
    vec3 reflectDirection = reflect(-lightDirection, normal);
    vec3 halfwayDirection = normalize(lightDirection + viewDirection);
    vec3 specular = uLight.specular * pow(max(dot(normal, halfwayDirection), 0.0), uMaterial.shininess);

    // result
    fColor = vec4(ambient + diffuse + specular, 1.0);
}