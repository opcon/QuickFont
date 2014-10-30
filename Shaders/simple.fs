#version 430 core

uniform sampler2D tex_object;

in VS_OUT
{
	vec2 tc;
	vec4 colour;
} fs_in;

out vec4 colour;

void main(void)
{
	colour = texture(tex_object, fs_in.tc.st) + vec4(fs_in.colour.rgb, 0.);
    //colour = vec4(0., 0.5, 0., 1.0);
}