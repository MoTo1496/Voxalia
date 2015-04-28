#version 430 core

layout (location = 0) in vec3 position;
layout (location = 1) in vec3 normal;
layout (location = 2) in vec2 texcoord;
layout (location = 3) in vec4 color;
layout (location = 4) in vec4 Weights;
layout (location = 5) in vec4 BoneID;

const int MAX_BONES = 50;

layout (location = 1) uniform mat4 projection;
layout (location = 2) uniform mat4 model_matrix;
// ...
layout (location = 6) uniform mat4 boneTrans[MAX_BONES];

layout (location = 0) out vec4 f_pos;

void main()
{
	vec4 pos1 = vec4(position, 1.0);
	pos1 += (boneTrans[int(BoneID[0])] * vec4(position, 0.0)) * Weights[0];
	pos1 += (boneTrans[int(BoneID[1])] * vec4(position, 0.0)) * Weights[1];
	pos1 += (boneTrans[int(BoneID[2])] * vec4(position, 0.0)) * Weights[2];
	pos1 += (boneTrans[int(BoneID[3])] * vec4(position, 0.0)) * Weights[3];
	vec4 positiony = projection * model_matrix * vec4(pos1.xyz, 1.0);
	f_pos = positiony;
	gl_Position = positiony;
}
