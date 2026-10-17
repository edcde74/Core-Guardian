Shader "Hidden/CoreGuardian/UpgradePreviewOnly"
{
    Properties {_MainTex("Mask",2D)="white"{} _Mode("Scan",Float)=0 _BandY("Band world Y",Float)=0 _BandWidth("Band half width",Float)=.06 _Opacity("Opacity",Float)=1}
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
        Pass
        {
            Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
            float _Mode,_BandY,_BandWidth,_Opacity;
            struct In {float4 positionOS:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
            struct Out {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;float worldY:TEXCOORD1;};
            Out Vert(In i){Out o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.worldY=TransformObjectToWorld(i.positionOS.xyz).y;o.uv=i.uv;o.color=i.color;return o;}
            half4 Frag(Out i):SV_Target
            {
                if(_Mode<.5)return i.color;
                half mask=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).r;
                clip(mask-.5);float distance=abs(i.worldY-_BandY);clip(_BandWidth-distance);
                half lead=step(_BandWidth*.6,distance);
                return half4(lerp(half3(.12,.8,.9),half3(.65,1,1),lead),_Opacity*lerp(.62,.9,lead));
            }
            ENDHLSL
        }
    }
}
