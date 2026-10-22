Shader "CoreGuardian/TowerGlow"
{
    Properties
    {
        _MainTex("Original sprite",2D)="white"{}
        _UVRect("Original UV bounds",Vector)=(0,0,1,1)
        _Role("Tower role",Float)=0
        _Clock("Idle clock",Float)=0
        _Age("Actual shot age",Float)=100
        _PulseScale("Attack-rate pulse scale",Float)=1
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
            float4 _MainTex_TexelSize,_UVRect;
            float _Role,_Clock,_Age,_PulseScale;
            CBUFFER_END
            struct Attributes {float4 positionOS:POSITION;float2 uv:TEXCOORD0;};
            struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
            Varyings Vert(Attributes i){Varyings o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv;return o;}
            half Mask(float2 uv)
            {
                half inside=step(_UVRect.x,uv.x)*step(_UVRect.y,uv.y)*step(uv.x,_UVRect.z)*step(uv.y,_UVRect.w);
                half4 c=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv);
                return inside*c.a*step(.08,c.b)*step(.015,c.g)*step(.08,c.b-c.r)*step(.008,c.g-c.r);
            }
            half Pulse(float age,float delay,float duration){return step(delay,age)*saturate(1-(age-delay)/duration);}
            half4 Frag(Varyings i):SV_Target
            {
                float2 p=(i.uv-_UVRect.xy)/(_UVRect.zw-_UVRect.xy);
                float age=_Age/max(.1,_PulseScale);
                half pulse=Pulse(age,0,.16);
                half strength=.06+pulse*.95;
                if(_Role>.5&&_Role<1.5)strength=.07+Pulse(age,abs(p.x-.5)*.2,.14)*1.05;
                if(_Role>1.5)strength=.13+.06*sin(_Clock*3.14159*1.4)+pulse*.65;
                float2 d=_MainTex_TexelSize.xy*1.5;
                half center=Mask(i.uv);
                half halo=(Mask(i.uv+float2(d.x,0))+Mask(i.uv-float2(d.x,0))+Mask(i.uv+float2(0,d.y))+Mask(i.uv-float2(0,d.y)))*.25;
                half3 tint=lerp(half3(.06,.52,1),half3(.4,.9,1),pulse);
                return half4(tint*(center+halo*(_Role>.5&&_Role<1.5?.18:.08))*strength,0);
            }
            ENDHLSL
        }
    }
}
