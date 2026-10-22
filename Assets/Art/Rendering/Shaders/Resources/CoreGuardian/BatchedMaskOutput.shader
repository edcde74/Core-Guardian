Shader "CoreGuardian/BatchedMaskOutput"
{
    Properties {_MainTex("GPU mask page",2D)="black"{} _Single("Original quad transform",Float)=0 _Color("Outline",Color)=(1,1,1,1)}
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "DisableBatching"="True"}
        Pass
        {
            Cull Off ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex);
            float _Single;float4 _Region,_Parameters;half4 _Color;
            struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;nointerpolation float4 region:TEXCOORD1;nointerpolation float4 parameters:TEXCOORD2;nointerpolation half4 color:COLOR;};
            Varyings Vert(float4 positionOS:POSITION,float4 color:COLOR,float2 uv:TEXCOORD0,float4 region:TEXCOORD1,float4 parameters:TEXCOORD2,float4 row0:TEXCOORD4,float4 row1:TEXCOORD5,float4 row2:TEXCOORD6,float4 row3:TEXCOORD7)
            {
                float4 world=mul(float4x4(row0,row1,row2,row3),positionOS);
                Varyings o;o.positionCS=TransformWorldToHClip(world.xyz);o.uv=uv;o.region=region;o.parameters=parameters;o.color=color;
                if(_Single>.5){o.positionCS=TransformObjectToHClip(positionOS.xyz);o.region=_Region;o.parameters=_Parameters;o.color=_Color;}return o;
            }
            half Read(float2 uv,float4 region)
            {int2 pixel=clamp((int2)floor(uv*region.zw),int2(0,0),(int2)region.zw-1);return LOAD_TEXTURE2D(_MainTex,(int2)region.xy+pixel).r;}
            half4 Frag(Varyings i):SV_Target
            {
                float2 texel=1/i.region.zw;
                if(i.parameters.w>.5)
                {
                    clip(i.parameters.y-(i.uv.y*i.region.w-i.parameters.z));clip(.5-Read(i.uv,i.region));
                    half a=Read(i.uv+float2(0,1)*texel,i.region)*.55;
                    half b=Read(i.uv+float2(0,2)*texel,i.region)*.21;
                    return half4(.035,.03,.025,max(a,b));
                }
                clip(.5-Read(i.uv,i.region));half edge=0;int width=(int)i.parameters.x;
                [loop] for(int y=-width;y<=width;y++)[loop] for(int x=-width;x<=width;x++)edge=max(edge,Read(i.uv+float2(x,y)*texel,i.region));
                clip(edge-.5);return i.color;
            }
            ENDHLSL
        }
    }
}
