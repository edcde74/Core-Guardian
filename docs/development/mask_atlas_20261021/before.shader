Shader "CoreGuardian/SelectionMask"
{
    Properties { _MainTex("Sprite",2D)="white"{} }
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
            struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
            Varyings Vert(float4 positionOS:POSITION,float2 uv:TEXCOORD0)
            {Varyings o;o.positionCS=mul(_MaskVP,mul(unity_ObjectToWorld,positionOS));o.uv=uv;return o;}
            half4 Frag(Varyings i):SV_Target
            {clip(SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).a*_Opacity-_Threshold);return 1;}
            ENDHLSL
        }
    }
}
