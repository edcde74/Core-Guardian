Shader "CoreGuardian/SelectionMask"
{
    Properties { _MainTex("Sprite",2D)="white"{} _UsePartMatrices("Per-part transforms",Float)=0 }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline"}
        Pass
        {
            Cull Off ZWrite Off ZTest Always
            Blend One One
            BlendOp Max
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
            float4x4 _MaskVP;
            float _Threshold,_Opacity;
            float _UsePartMatrices;
            float4 _PageSize;
            struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;nointerpolation float4 atlasRect:TEXCOORD1;};
            Varyings Vert(float4 positionOS:POSITION,float2 uv:TEXCOORD0,float4 row0:TEXCOORD1,float4 row1:TEXCOORD2,float4 row2:TEXCOORD3,float4 row3:TEXCOORD4,float4 atlasRect:TEXCOORD5)
            {
                float4 world=mul(unity_ObjectToWorld,positionOS);
                if(_UsePartMatrices>.5)
                {
                    float4x4 part=float4x4(row0,row1,row2,row3);
                    world=mul(part,positionOS);
                }
                Varyings o;o.positionCS=mul(_MaskVP,world);o.uv=uv;o.atlasRect=atlasRect;return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                float2 uv=i.uv;
                // Interpolate original UVs first: interpolating atlas UVs moves boundary samples.
                if(i.atlasRect.z>0)
                {
                    precise float2 texel=uv*i.atlasRect.zw;
                    precise float2 packed=i.atlasRect.xy+texel;
                    uv=packed/_PageSize.xy;
                }
                clip(SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv).a*_Opacity-_Threshold);return 1;
            }
            ENDHLSL
        }
    }
}
