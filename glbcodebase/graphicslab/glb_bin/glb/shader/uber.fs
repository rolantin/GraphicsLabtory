//----------------------------------------------------
// Declaration: Copyright (c), by i_dovelemon, 2016. All right reserved.
// Author: i_dovelemon[1322600812@qq.com]
// Date: 2016 / 10 / 15
// Brief: This is a uber shader, all the light calculation, texture mapping
// and some sort of that will be implemented in this shader.
//----------------------------------------------------

#extension GL_NV_shadow_samplers_cube : enable

// Constant
const float PI = 3.1415927;

// Input attributes
in vec3 vs_Vertex;

#ifdef GLB_COLOR_IN_VERTEX
in vec3 vs_Color;
#endif

#ifdef GLB_NORMAL_IN_VERTEX
in vec3 vs_Normal;
#endif

#ifdef GLB_TANGENT_IN_VERTEX
in vec3 vs_Tangent;
#endif

#ifdef GLB_BINORMAL_IN_VERTEX
in vec3 vs_Binormal;
#endif

#ifdef GLB_TEXCOORD_IN_VERTEX
in vec2 vs_TexCoord;
#endif

// Output color
out vec4 oColor;

// Uniform
#ifdef GLB_TEXCOORD_IN_VERTEX

#ifdef GLB_ENABLE_ALBEDO_TEX
uniform sampler2D glb_AlbedoTex;
#endif

#ifdef GLB_ENABLE_ROUGHNESS_TEX
uniform sampler2D glb_RoughnessTex;
#endif

#ifdef GLB_ENABLE_METALLIC_TEX
uniform sampler2D glb_MetallicTex;
#endif

#ifdef GLB_ENABLE_ALPHA_TEX
uniform sampler2D glb_AlphaTex;
#endif

#ifdef GLB_ENABLE_NORMAL_TEX
uniform sampler2D glb_NormalTex;

#ifdef GLB_TANGENT_IN_VERTEX
#ifdef GLB_BINORMAL_IN_VERTEX
#ifdef GLB_ENABLE_NORMAL_TEX
	#define GLB_ENABLE_NORMAL_MAPPING
#endif
#endif
#endif

#endif

#ifdef GLB_ENABLE_EMISSION_TEX
uniform sampler2D glb_EmissionTex;
#endif

#ifdef GLB_ENABLE_REFLECT_TEX
uniform samplerCube glb_ReflectTex;
#endif

#endif

#ifdef GLB_ENABLE_SHADOW
uniform sampler2D glb_ShadowTex0;
uniform sampler2D glb_ShadowTex1;
uniform sampler2D glb_ShadowTex2;
uniform sampler2D glb_ShadowTex3;
uniform mat4 glb_ShadowM0;
uniform mat4 glb_ShadowM1;
uniform mat4 glb_ShadowM2;
uniform mat4 glb_ShadowM3;
uniform float glb_ShadowSplit0;
uniform float glb_ShadowSplit1;
uniform float glb_ShadowSplit2;

int calc_current_shadow_index(vec3 vtx, vec3 eye_pos, vec3 look_at, float sp0, float sp1, float sp2) {
	int index = -1;
	vec3 to_vtx = eye_pos - vtx;
	float z_value = dot(to_vtx, look_at);
	if (z_value < sp0) {
		index = 0;
	} else if (sp0 < z_value && z_value < sp1) {
		index = 1;
	} else if (sp1 < z_value && z_value < sp2) {
		index = 2;
	} else if (z_value > sp2) {
		index = 3;
	}

	return index;
}
#endif

#ifdef GLB_ENABLE_AO
uniform float glb_ScreenWidth;
uniform float glb_ScreenHeight;
uniform sampler2D glb_AOMap;
#endif

#ifdef GLB_ENABLE_LIGHTING
	#define GLB_ENABLE_EYE_POS
#endif

#ifdef GLB_ENABLE_REFLECT_TEX
	#define GLB_ENABLE_EYE_POS
#endif

#ifdef GLB_ENABLE_EYE_POS
uniform vec3 glb_EyePos;
uniform vec3 glb_LookAt;
#endif

uniform vec3 glb_Material_Emission;

#ifdef GLB_ENABLE_LIGHTING
uniform vec3 glb_Material_Ambient;
uniform vec3 glb_Material_Diffuse;
uniform vec3 glb_Material_Specular;
uniform float glb_Material_Pow;
uniform vec3 glb_Material_Albedo;
uniform float glb_Material_Roughness;
uniform float glb_Material_Metallic;

uniform vec3 glb_GlobalLight_Ambient;

#ifdef GLB_USE_PARALLEL_LIGHT
uniform vec3 glb_ParallelLight_Dir;
uniform vec3 glb_ParallelLight;
#else
	#error No light source specified
#endif

uniform samplerCube glb_DiffusePFCTex;
uniform samplerCube glb_SpecularPFCTex;
uniform sampler2D glb_BRDFPFTTex;

vec3 calc_fresnel(vec3 n, vec3 v, vec3 F0) {
    float ndotv = max(dot(n, v), 0.0);
    return F0 + (vec3(1.0, 1.0, 1.0) - F0) * pow(1.0 - ndotv, 5.0);
}

float calc_NDF_GGX(vec3 n, vec3 h, float roughness) {
    float a = roughness * roughness;
    float a2 = a * a;
    float ndoth = max(dot(n, h), 0.0);
    float ndoth2 = ndoth * ndoth;
    float t = ndoth2 * (a2 - 1.0) + 1.0;
    float t2 = t * t;
    return a2 / (PI * t2);
}

float calc_Geometry_GGX(float costheta, float roughness) {
    float a = roughness;
    float r = a + 1.0;
    float r2 = r * r;
    float k = r2 / 8.0;

    float t = costheta * (1.0 - k) + k;

    return costheta / t;
}

float calc_Geometry_Smith(vec3 n, vec3 v, vec3 l, float roughness) {
    float ndotv = max(dot(n, v), 0.0);
    float ndotl = max(dot(n, l), 0.0);
    float ggx1 = calc_Geometry_GGX(ndotv, roughness);
    float ggx2 = calc_Geometry_GGX(ndotl, roughness);
    return ggx1 * ggx2;
}

vec3 calc_fresnel_roughness(vec3 n, vec3 v, vec3 F0, float roughness) {
    float ndotv = max(dot(n, v), 0.0);
    return F0 + (max(vec3(1.0 - roughness), F0) - F0) * pow(1.0 - ndotv, 5.0);
}

vec3 filtering_cube_map(samplerCube cube, vec3 n) {
    n.yz = -n.yz;
    return texture(cube, n).xyz;
}

vec3 filtering_cube_map_lod(samplerCube cube, vec3 n, float lod) {
    n.yz = -n.yz;
    return textureLod(cube, n, lod).xyz;
}

#endif  // GLB_ENABLE_LIGHTING

float calc_shadow() {
	float shadow_factor = 1.0;

#ifdef GLB_ENABLE_SHADOW
	vec2 shadow_coord;
	int index = calc_current_shadow_index(vs_Vertex, glb_EyePos, glb_LookAt, glb_ShadowSplit0, glb_ShadowSplit1, glb_ShadowSplit2);
	vec4 light_space_pos;
	float factor;
	if (index == 0) {
		light_space_pos = glb_ShadowM0 * vec4(vs_Vertex, 1.0);
		light_space_pos.xyz /= 2.0f;
		light_space_pos.xyz += 0.5f;
		light_space_pos.xyz /= light_space_pos.w;
		factor = texture2D(glb_ShadowTex0, light_space_pos.xy).z;
	} else if (index == 1) {
		light_space_pos = glb_ShadowM1 * vec4(vs_Vertex, 1.0);
		light_space_pos.xyz /= 2.0f;
		light_space_pos.xyz += 0.5f;
		light_space_pos.xyz /= light_space_pos.w;	
		factor = texture2D(glb_ShadowTex1, light_space_pos.xy).z;
	} else if (index == 2) {
		light_space_pos = glb_ShadowM2 * vec4(vs_Vertex, 1.0);
		light_space_pos.xyz /= 2.0f;
		light_space_pos.xyz += 0.5f;
		light_space_pos.xyz /= light_space_pos.w;				
		factor = texture2D(glb_ShadowTex2, light_space_pos.xy).z;
	} else {
		light_space_pos = glb_ShadowM3 * vec4(vs_Vertex, 1.0);
		light_space_pos.xyz /= 2.0f;
		light_space_pos.xyz += 0.5f;
		light_space_pos.xyz /= light_space_pos.w;
		factor = texture2D(glb_ShadowTex3, light_space_pos.xy).z;
	}
	if (factor < light_space_pos.z) {
		// In shadow
		shadow_factor = 0.0;
	}
	if (light_space_pos.x < 0.0 || 
		light_space_pos.x > 1.0 ||
		light_space_pos.y < 0.0 ||
		light_space_pos.y > 1.0) {
		// Out of shadow
		shadow_factor = 1.0;
	}
	shadow_coord = light_space_pos.xy;
#endif

	return shadow_factor;
}

vec3 calc_normal() {
	vec3 normal = vec3(0.0, 0.0, 0.0);

#ifdef GLB_NORMAL_IN_VERTEX
	normal = normalize(vs_Normal);
#endif

#ifdef GLB_ENABLE_NORMAL_MAPPING
	normal = texture2D(glb_NormalTex, vs_TexCoord).xyz;
	normal -= vec3(0.5, 0.5, 0.5);
	normal *= 2.0;
	normal = normalize(normal);

	mat3 tbn = mat3(normalize(vs_Tangent), normalize(vs_Binormal), vs_Normal);
	normal = tbn * normal;
	normalize(normal);
#endif

	return normal;	
}

vec3 calc_view() {
	vec3 view = vec3(0.0, 0.0, 0.0);

#ifdef GLB_ENABLE_EYE_POS
	view = normalize(glb_EyePos - vs_Vertex);
#endif	

	return view;
}

vec3 calc_light_dir() {
	vec3 light_dir = vec3(0.0, 0.0, 0.0);
#ifdef GLB_USE_PARALLEL_LIGHT
	light_dir = -glb_ParallelLight_Dir;
#endif
	return light_dir;
}

void calc_material(out vec3 albedo, out float roughness, out float metallic, out vec3 emission) {
#ifdef GLB_ENABLE_LIGHTING

#ifdef GLB_ENABLE_ALBEDO_TEX
	albedo = texture(glb_AlbedoTex, vs_TexCoord).xyz;
#else
	albedo = glb_Material_Albedo;
#endif

#ifdef GLB_ENABLE_ROUGHNESS_TEX
	roughness = texture(glb_RoughnessTex, vs_TexCoord).x;
#else
	roughness = glb_Material_Roughness;
#endif

#ifdef GLB_ENABLE_METALLIC_TEX
	metallic = texture(glb_MetallicTex, vs_TexCoord).x;
#else
	metallic = glb_Material_Metallic;
#endif

#ifdef GLB_ENABLE_EMISSION_TEX
	emission = texture(glb_EmissionTex, vs_TexCoord).xyz * glb_Material_Emission;
#else
	emission = glb_Material_Emission;
#endif

#endif
}

vec3 calc_direct_light_color() {
	vec3 light = vec3(0.0, 0.0, 0.0);

#ifdef GLB_ENABLE_LIGHTING
	light = light + glb_GlobalLight_Ambient;

#ifdef GLB_USE_PARALLEL_LIGHT
	light = light + glb_ParallelLight;
#endif

#endif

	return light;
}

vec3 calc_direct_lighting(vec3 n, vec3 v, vec3 l, vec3 h, vec3 albedo, float roughness, float metalic, vec3 light) {
	vec3 result = vec3(0.0, 0.0, 0.0);

#ifdef GLB_ENABLE_LIGHTING
    vec3 F0 = mix(vec3(0.04, 0.04, 0.04), albedo, metalic);
    vec3 F = calc_fresnel(h, v, F0);

    vec3 T = vec3(1.0, 1.0, 1.0) - F;
    vec3 kD = T * (1.0 - metalic);

    float D = calc_NDF_GGX(n, h, roughness);

    float G = calc_Geometry_Smith(n, v, l, roughness);

    vec3 Diffuse = kD * albedo * vec3(1.0 / PI, 1.0 / PI, 1.0 / PI);
    float t = 4.0 * max(dot(n, v), 0.0) * max(dot(n, l), 0.0) + 0.001;
    vec3 Specular = D * F * G * vec3(1.0 / t, 1.0 / t, 1.0 / t);

    float ndotl = max(dot(n, l), 0.0);
	result = (Diffuse + Specular) * light * (ndotl);
#endif
return result;
}

vec3 calc_ibl_lighting(vec3 n, vec3 v, vec3 albedo, float roughness, float metalic) {
	vec3 result = vec3(0.0, 0.0, 0.0);	

#ifdef GLB_ENABLE_LIGHTING
    vec3 F0 = mix(vec3(0.04, 0.04, 0.04), albedo, metalic);
    vec3 F = calc_fresnel_roughness(n, v, F0, roughness);

    // Diffuse part
    vec3 T = vec3(1.0, 1.0, 1.0) - F;
    vec3 kD = T * (1.0 - metalic);

    vec3 irradiance = filtering_cube_map(glb_DiffusePFCTex, n);
    vec3 diffuse = kD * albedo * irradiance;

    // Specular part
    float ndotv = max(0.0, dot(n, v));
    vec3 r = 2.0 * ndotv * n - v;
    vec3 ld = filtering_cube_map_lod(glb_SpecularPFCTex, r, roughness);
    vec2 dfg = textureLod(glb_BRDFPFTTex, vec2(ndotv, roughness), 0.0).xy;
    vec3 specular = ld * (F0 * dfg.x + dfg.y);

    return diffuse + specular;
#endif

	return result;
}

float calc_alpha() {
	float alpha = 1.0;

#ifdef GLB_TEXCOORD_IN_VERTEX
	#ifdef GLB_ENABLE_ALPHA_TEX
		alpha = texture2D(glb_AlphaTex, vs_TexCoord).w;
	#endif
#endif

	return alpha;
}

void main() {
	oColor = vec4(0.0, 0.0, 0.0, 0.0);

	float shadow_factor = calc_shadow();
	vec3 normal = calc_normal();
	vec3 view = calc_view();
	vec3 light = calc_light_dir();	
	vec3 half = normalize(view + light);

	vec3 albedo = vec3(0.0, 0.0, 0.0);
	float roughness = 0.0;
	float metallic = 0.0;
	vec3 emission = vec3(0.0, 0.0, 0.0);
	calc_material(albedo, roughness, metallic, emission);

	vec3 direct_light_color = calc_direct_light_color();

	// vec3 n, vec3 v, vec3 l, vec3 h, vec3 albedo, float roughness, float metalic, vec3 light
	vec3 direct_color = calc_direct_lighting(normal, view, light, half, albedo, roughness, metallic, direct_light_color);

	// vec3 n, vec3 v, vec3 albedo, float roughness, float metalic
	vec3 ibl_color = calc_ibl_lighting(normal, view, albedo, roughness, metallic);

	oColor.xyz = (direct_color + ibl_color) * shadow_factor + emission;

	// TEST code
#ifdef GLB_ENABLE_REFLECT_TEX
    vec3 refl = reflect(-view, normal);
    refl = normalize(refl);
    refl.yz = -refl.yz;
    oColor.xyz = textureCube(glb_ReflectTex, refl).xyz;
#endif

	float alpha = calc_alpha();
	oColor.w = alpha;
}