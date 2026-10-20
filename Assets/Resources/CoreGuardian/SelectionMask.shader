Shader "CoreGuardian/SelectionMask"
{
    Properties { _MainTex("Sprite",2D)="white"{} _MaskAtlas("Mask array",2DArray)=""{} _UseAtlas("Use atlas",Float)=0 _UsePartMatrices("Per-part transforms",Float)=0 }
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
            #pragma target 3.5
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
            TEXTURE2D_ARRAY(_MaskAtlas);SAMPLER(sampler_MaskAtlas);
            float4x4 _MaskVP;
            float _Threshold,_Opacity;
            float _UsePartMatrices;
            float _UseAtlas;
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
                float alpha;
                if(_UseAtlas>.5)alpha=SAMPLE_TEXTURE2D_ARRAY(_MaskAtlas,sampler_MaskAtlas,i.uv,i.atlasRect.x).a;
                else alpha=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).a;
                clip(alpha*_Opacity-_Threshold);return 1;
            }
            ENDHLSL
        }
    }
}
