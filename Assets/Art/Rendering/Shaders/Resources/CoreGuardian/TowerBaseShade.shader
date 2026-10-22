Shader "CoreGuardian/TowerBaseShade"
{
    Properties
    {
        _MainTex("Original base alpha",2D)="white"{}
        _BottomUV("Sprite bottom UV",Float)=0
        _TextureHeight("Source texture height",Float)=64
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
            float _BottomUV,_TextureHeight;
            CBUFFER_END
            struct Attributes {float4 positionOS:POSITION;float2 uv:TEXCOORD0;};
            struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
            Varyings Vert(Attributes i){Varyings o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv;return o;}
            half4 Frag(Varyings i):SV_Target
            {
                half alpha=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).a;
                float row=floor((i.uv.y-_BottomUV)*_TextureHeight);
                half strength=.46*saturate(1-row/5.0);
                return half4(.025,.022,.018,alpha*strength);
            }
            ENDHLSL
        }
    }
}
