struct VS_IN
{
	float2 pos : POSITION;
	float2 tex : TEXCOORD;
	float4 col : COLOR;
};

struct PS_IN
{
	float4 pos : SV_POSITION;
	float4 tex : TEXCOORD;
	float4 col : COLOR;
};

cbuffer VS_CONSTANT_BUFFER : register(b0)
{
	float fWidth;
	float fHeight;
};
PS_IN VS(VS_IN input)
{
	PS_IN output = (PS_IN)0;

	output.pos.x = (input.pos.x / fWidth) * 2 - 1;
	output.pos.y = 1 - (input.pos.y / fHeight) * 2;
	output.pos.z = 0.5;
	output.pos.w = 1;
	output.tex.xy = input.tex;
	output.col = input.col;

	return output;
}

Texture2D faceTexture : register(t0);
SamplerState textureSampler : register(s0);

float4 PS(PS_IN input) : SV_Target
{
	return input.col * faceTexture.Sample(textureSampler, input.tex.xy);
}
