#version 430 core

layout (binding = 0) uniform sampler2DArray s;
layout (binding = 1) uniform sampler2DArray htex;
layout (binding = 2) uniform sampler2DArray normal_tex;

layout (location = 3) uniform vec4 v_color = vec4(1.0);
layout (location = 4) uniform float specular_power = 200.0 / 1000.0f;
layout (location = 5) uniform float minimum_light = 0.0;
// ...
layout (location = 7) uniform vec4 bw_color = vec4(0.0, 0.0, 0.0, 1.0);
layout (location = 8) uniform vec2 light_clamp = vec2(0.0, 1.0);

in struct vox_out
{
	vec4 position;
	vec3 texcoord;
	vec4 color;
	vec4 tcol;
	mat3 tbn;
} f;

layout (location = 0) out vec4 color;
layout (location = 1) out vec4 position;
layout (location = 2) out vec4 normal;
layout (location = 3) out vec4 renderhint;
layout (location = 4) out vec4 bw;

void main()
{
	vec4 col = texture(s, f.texcoord) * vec4(clamp(f.color.xyz, vec3(light_clamp.x), vec3(light_clamp.y)), f.color.w) * f.tcol;
	if (col.w * v_color.w < 0.99)
	{
		discard;
	}
    float spec = texture(htex, vec3(0, 0, f.texcoord.z)).r;
    // float wavi = texture(htex, vec3(1, 0, f.texcoord.z)).r; // TODO: Implement!?
    float refl = texture(htex, vec3(0, 1, f.texcoord.z)).r;
	vec3 norms = texture(normal_tex, f.texcoord).xyz * 2.0 - 1.0;
	color = col * v_color;
	position = vec4(f.position.xyz, 1.0);
	normal = vec4(normalize(f.tbn * norms), 1.0);
	renderhint = vec4(spec, specular_power, minimum_light, refl);
    // TODO: Maybe take advantage of normal.w and position.w as well?
    bw = bw_color;
}
