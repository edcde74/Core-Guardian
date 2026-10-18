Shader "CoreGuardian/PhotoShadowContact"
{
    Properties { _MainTex("Current animated body union",2D)="black"{} _Band("Lower contact band",Float)=16 }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
        Pass
        {
            Cull Off ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;
            float _Band;
            struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
            Varyings Vert(float4 positionOS:POSITION,float2 uv:TEXCOORD0)
            {Varyings o;o.positionCS=TransformObjectToHClip(positionOS.xyz);o.uv=uv;return o;}
            half4 Frag(Varyings i):SV_Target
            {
                clip(_Band-i.uv.y*_MainTex_TexelSize.w);
                clip(.5-SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).r);
                half a=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv+float2(0,1)*_MainTex_TexelSize.xy).r*.55;
                half b=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv+float2(0,2)*_MainTex_TexelSize.xy).r*.21;
                return half4(.035,.03,.025,max(a,b));
            }
            ENDHLSL
        }
    }
}
