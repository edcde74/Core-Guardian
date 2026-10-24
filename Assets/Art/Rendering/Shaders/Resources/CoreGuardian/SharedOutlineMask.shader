Shader "CoreGuardian/SharedOutlineMask"
{
    // Writes the owning emphasis entity id of each visible body pixel into one screen-size mask.
    // Depth (rank in the visual back-to-front sort) keeps the front-most body regardless of draw order.
    Properties { _MainTex("Sprite",2D)="white"{} }
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
    float4x4 _CGSharedOutlineVP;
    float _CGSharedOutlineThreshold,_CGMaskEntity,_CGMaskDepth;
    struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;nointerpolation float2 data:TEXCOORD1;};
    float4 ToMaskClip(float4 world,float depth)
    {
        float4 clip=mul(_CGSharedOutlineVP,world);
        // ShaderLab "ZTest Greater" and the depth clear value are flipped by Unity on reversed-Z APIs (D3D/Vulkan/Metal),
        // so the written value is flipped there too: in every case the larger rank (front) wins.
        #if UNITY_REVERSED_Z
        depth=1-depth;
        #endif
        // Raw depth in [0,1] after the viewport transform on every API (GL clip z is [-1,1]).
        #if defined(SHADER_API_GLCORE)||defined(SHADER_API_GLES3)
        clip.z=(depth*2-1)*clip.w;
        #else
        clip.z=depth*clip.w;
        #endif
        return clip;
    }
    float4 MaskFrag(Varyings i):SV_Target
    {
        // Same per-pixel rule as SelectionMask: sprite alpha x renderer alpha >= threshold.
        clip(SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).a*i.data.y-_CGSharedOutlineThreshold);
        return float4(i.data.x,0,0,1);
    }
    ENDHLSL
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline"}
        // Pass 0: batched parts. Per-part matrix rows, entity, depth and alpha come from _CGMaskParts.
        Pass
        {
            Cull Off ZWrite On ZTest Greater
            Blend Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment MaskFrag
            #pragma target 4.5
            struct Part {float4 r0,r1,r2,r3,data;};
            StructuredBuffer<Part> _CGMaskParts;
            Varyings Vert(float3 positionOS:POSITION,float2 uv:TEXCOORD0,float2 index:TEXCOORD1)
            {
                Part p=_CGMaskParts[(uint)index.x];
                float4 world=mul(float4x4(p.r0,p.r1,p.r2,p.r3),float4(positionOS,1));
                Varyings o;o.positionCS=ToMaskClip(world,p.data.y);o.uv=uv;o.data=float2(p.data.x,p.data.z);return o;
            }
            ENDHLSL
        }
        // Pass 1: renderer fallback (non-simple draw modes, no compute support). Entity/depth are globals.
        Pass
        {
            Cull Off ZWrite On ZTest Greater
            Blend Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment MaskFrag
            #pragma target 3.5
            Varyings Vert(float4 positionOS:POSITION,float2 uv:TEXCOORD0,float4 color:COLOR)
            {
                // SpriteRenderer flip is applied by the sprite shader, not by the transform.
                float3 local=float3(positionOS.xy*unity_SpriteProps.xy,positionOS.z);
                Varyings o;o.positionCS=ToMaskClip(mul(unity_ObjectToWorld,float4(local,1)),_CGMaskDepth);o.uv=uv;
                // Renderer alpha may arrive as vertex colour or as unity_SpriteColor depending on the draw path.
                o.data=float2(_CGMaskEntity,color.a*unity_SpriteColor.a);return o;
            }
            ENDHLSL
        }
    }
}
