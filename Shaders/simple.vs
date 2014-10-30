#version 430 core

uniform mat4 proj_matrix;

in vec3 in_position;
in vec2 in_tc;
in vec4 in_colour;

out VS_OUT
{
	vec2 tc;
	vec4 colour;
} vs_out;

void main(void)
{
	vs_out.tc = in_tc;
	vs_out.colour = in_colour;
	gl_Position = proj_matrix * vec4(in_position, 1.); 
//    gl_Position = vec4(0.,0.,0.,1.);
}