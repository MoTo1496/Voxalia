#version 430 core

layout (points) in;
layout (triangle_strip, max_vertices = 4) out;

layout (location = 1) uniform mat4 proj_matrix = mat4(1.0);

in struct vox_out
{
	vec2 texcoord;
	vec4 color;
} f[1];

out struct vox_out
{
	vec2 texcoord;
	vec4 color;
} fi;

void main()
{
	fi.color = f[0].color;
	vec3 pos = gl_in[0].gl_Position.xyz;// / gl_in[0].gl_Position.w;
	if (pos.x == 0.0 && pos.y == 0.0 && pos.z == 1.0 || dot(pos, pos) > (50.0 * 50.0))
	{
		return;
	}
	vec3 up = vec3(0.0, 0.0, 1.0);
	vec3 right = cross(up, pos);
	// First Vertex
	gl_Position = proj_matrix * vec4(pos - (right + up) * 0.5, 1.0);
	fi.texcoord = vec2(0.0, 0.0);
	EmitVertex();
	// Second Vertex
	gl_Position = proj_matrix * vec4(pos - (right - up) * 0.5, 1.0);
	fi.texcoord = vec2(0.0, 1.0);
	EmitVertex();
	// Third Vertex
	gl_Position = proj_matrix * vec4(pos + (right + up) * 0.5, 1.0);
	fi.texcoord = vec2(1.0, 0.0);
	EmitVertex();
	// Fourth Vertex
	gl_Position = proj_matrix * vec4(pos + (right - up) * 0.5, 1.0);
	fi.texcoord = vec2(1.0, 1.0);
	EmitVertex();
	EndPrimitive();
}
