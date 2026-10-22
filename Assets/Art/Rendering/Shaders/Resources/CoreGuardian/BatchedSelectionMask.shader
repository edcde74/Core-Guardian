Shader "CoreGuardian/BatchedSelectionMask"
{
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline"}
        Pass
        {
            Cull Off ZWrite Off ZTest Always
            Blend One One
            BlendOp Max
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Item {float4 p0,p1,p2,p3,v0,v1,v2,v3,region,alpha;};
            StructuredBuffer<Item> _Items;
            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
            TEXTURE2D_ARRAY(_MaskAtlas);SAMPLER(sampler_MaskAtlas);
            float2 _PageSize;float _UseAtlas;
            struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;nointerpolation float3 alpha:TEXCOORD1;nointerpolation float4 region:TEXCOORD2;};
            Varyings Vert(float4 positionOS:POSITION,float2 uv:TEXCOORD0,float2 index:TEXCOORD1)
            {
                Item data=_Items[(uint)index.x];float4 world=mul(float4x4(data.p0,data.p1,data.p2,data.p3),positionOS);
                float4 clipPosition=mul(float4x4(data.v0,data.v1,data.v2,data.v3),world);
                precise float2 pixel=clipPosition.xy*(data.region.zw*.5)+clipPosition.w*(data.region.zw*.5);
                #if UNITY_UV_STARTS_AT_TOP
                pixel.y=clipPosition.w*data.region.w-pixel.y;
                #endif
                // D3D rasterization uses an 8-bit subpixel grid. Preserve that local grid before
                // placing it on the power-of-two page, so the page offset cannot move an edge.
                pixel=floor(pixel/clipPosition.w*256+.5)/256+data.region.xy;
                clipPosition.xy=(pixel/_PageSize*2-1)*clipPosition.w;
                #if UNITY_UV_STARTS_AT_TOP
                clipPosition.y=-clipPosition.y;
                #endif
                Varyings o;o.positionCS=clipPosition;o.uv=uv;o.alpha=float3(data.alpha.xy,index.y);o.region=data.region;return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                clip(i.positionCS.xy-i.region.xy);clip(i.region.xy+i.region.zw-i.positionCS.xy);
                float alpha;
                if(_UseAtlas>.5)alpha=SAMPLE_TEXTURE2D_ARRAY(_MaskAtlas,sampler_MaskAtlas,i.uv,i.alpha.z).a;
                else alpha=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).a;
                clip(alpha*i.alpha.x-i.alpha.y);return 1;
            }
            ENDHLSL
        }
    }
}
