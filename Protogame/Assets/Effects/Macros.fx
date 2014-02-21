// -----------------------------------------------------------------------------
// These are utility macros that are provided to all effects compiled under Protogame.
// They are used to target different shader profiles depending on whether compilation
// is for DX11 or OpenGL.
//
// Some of these macros are from the MonoGame default effects.
//
// This file is included automatically as a literal prefix for all effect code.
// -----------------------------------------------------------------------------

#ifdef SM4
#define PROTOGAME_VERTEX_SHADER vs_4_0_level_9_1
#define PROTOGAME_PIXEL_SHADER ps_4_0_level_9_1
#define PROTOGAME_POSITION SV_Position
#define PROTOGAME_DEPTH DEPTH0
#define PROTOGAME_TARGET(n) COLOR##n
#define PROTOGAME_NORMAL(n) NORMAL##n
#define PROTOGAME_TEXCOORD(n) TEXCOORD##n
#define PROTOGAME_BINORMAL(n) BINORMAL##n
#define PROTOGAME_TANGENT(n) TANGENT##n

#define PROTOGAME_DECLARE_TEXTURE(Name) \
    Texture2D<float4> Name; \
    sampler Name##Sampler

#define PROTOGAME_DECLARE_CUBEMAP(Name) \
    TextureCube<float4> Name; \
    sampler Name##Sampler

#define PROTOGAME_SAMPLE_TEXTURE(Name, texCoord)  Name.Sample(Name##Sampler, texCoord)
#define PROTOGAME_SAMPLE_CUBEMAP(Name, texCoord)  Name.Sample(Name##Sampler, texCoord)

#else
#define PROTOGAME_VERTEX_SHADER vs_2_0
#define PROTOGAME_PIXEL_SHADER ps_2_0
#define PROTOGAME_POSITION POSITION0
#define PROTOGAME_DEPTH DEPTH0
#define PROTOGAME_TARGET(n) COLOR##n
#define PROTOGAME_NORMAL(n) NORMAL##n
#define PROTOGAME_TEXCOORD(n) TEXCOORD##n
#define PROTOGAME_BINORMAL(n) BINORMAL##n
#define PROTOGAME_TANGENT(n) TANGENT##n

#define PROTOGAME_DECLARE_TEXTURE(Name) \
    sampler2D Name

#define PROTOGAME_DECLARE_CUBEMAP(Name) \
    samplerCUBE Name

#define PROTOGAME_SAMPLE_TEXTURE(Name, texCoord)  tex2D(Name, texCoord)
#define PROTOGAME_SAMPLE_CUBEMAP(Name, texCoord)  texCUBE(Name, texCoord)

#endif

