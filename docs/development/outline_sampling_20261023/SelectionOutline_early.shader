Shader "CoreGuardian/SelectionOutline"
{
    Properties {_MainTex("Union mask",2D)="black"{} _Color("Outline color",Color)=(.8,.67,.39,1) _Width("Screen pixels",Int)=2}
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
            half4 _Color;
            int _Width;
            struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
            Varyings Vert(float4 positionOS:POSITION,float2 uv:TEXCOORD0)
            {Varyings o;o.positionCS=TransformObjectToHClip(positionOS.xyz);o.uv=uv;return o;}
            half4 Frag(Varyings i):SV_Target
            {
                float center=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).r;
                clip(.5-center);
                half edge=max(0,center);
                if(edge>=.5)return _Color;
                [loop] for(int y=-_Width;y<=_Width;y++)
                [loop] for(int x=-_Width;x<=_Width;x++)
                {
                    if(x!=0||y!=0)
                    {
                        // Body masks are point-filtered RTs without mips. Explicit LOD
                        // avoids implicit derivatives after neighboring pixels return.
                        edge=max(edge,SAMPLE_TEXTURE2D_LOD(_MainTex,sampler_MainTex,i.uv+float2(x,y)*_MainTex_TexelSize.xy,0).r);
                        if(edge>=.5)return _Color;
                    }
                }
                clip(edge-.5);return _Color;
            }
            ENDHLSL
        }
    }
}
