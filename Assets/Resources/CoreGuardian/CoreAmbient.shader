Shader "CoreGuardian/CoreAmbient"
{
    Properties
    {
        _MainTex("Source sprite",2D)="white"{}
        _UVRect("Source UV bounds",Vector)=(0,0,1,1)
        _Strength("Local glow strength",Float)=.4
        _Particles("Particle material",Float)=0
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
        Blend One One
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
            float4 _MainTex_TexelSize;
            float4 _UVRect;
            float _Strength;
            float _Particles;
            CBUFFER_END
            struct Attributes {float4 positionOS:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
            struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
            Varyings Vert(Attributes input)
            {
                Varyings output;output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                output.uv=input.uv;output.color=input.color;return output;
            }
            half Cyan(float2 uv)
            {
                half inside=step(_UVRect.x,uv.x)*step(_UVRect.y,uv.y)*step(uv.x,_UVRect.z)*step(uv.y,_UVRect.w);
                half4 c=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv);
                return inside*c.a*step(.15,c.g)*step(.2,c.b)*step(.1,c.g-c.r)*step(.15,c.b-c.r);
            }
            half4 Frag(Varyings input):SV_Target
            {
                if(_Particles>.5)return half4(input.color.rgb*input.color.a,0);
                float2 d=_MainTex_TexelSize.xy*3;
                half center=Cyan(input.uv);
                half halo=(Cyan(input.uv+float2(d.x,0))+Cyan(input.uv-float2(d.x,0))+
                    Cyan(input.uv+float2(0,d.y))+Cyan(input.uv-float2(0,d.y))+
                    Cyan(input.uv+d)+Cyan(input.uv-d)+Cyan(input.uv+float2(d.x,-d.y))+Cyan(input.uv+float2(-d.x,d.y)))*.125;
                return half4(half3(.12,.7,.95)*(center+halo*.3)*_Strength,0);
            }
            ENDHLSL
        }
    }
}
