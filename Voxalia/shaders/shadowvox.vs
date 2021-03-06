//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

#version 430 core

layout (location = 0) in vec3 position;
layout (location = 1) in vec3 normal;
layout (location = 2) in vec3 texcoords;
layout (location = 3) in vec4 color;

layout (location = 1) uniform mat4 projection = mat4(1.0);
layout (location = 2) uniform mat4 model_matrix = mat4(1.0);
// ...
layout (location = 5) uniform float should_sqrt = 0.0;

layout (location = 0) out vec4 f_pos;
layout (location = 1) out vec3 f_texcoord;
layout (location = 2) out vec4 f_color;

float fix_sqr(in float inTemp)
{
	return 1.0 - (inTemp * inTemp);
}

void main()
{
	f_pos = projection * model_matrix * vec4(position, 1.0);
	f_texcoord = texcoords;
	if (should_sqrt >= 0.5)
	{
		f_pos /= f_pos.w;
		f_pos.x = sign(f_pos.x) * fix_sqr(1.0 - abs(f_pos.x));
		f_pos.y = sign(f_pos.y) * fix_sqr(1.0 - abs(f_pos.y));
	}
	gl_Position = f_pos;
	f_color = color;
}
