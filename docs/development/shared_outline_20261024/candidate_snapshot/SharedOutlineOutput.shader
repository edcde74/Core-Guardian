Shader "CoreGuardian/SharedOutlineOutput"
{
    // Prototype (default OFF): per-body outline output that reads the shared entity-id mask.
    // Same square kernel and colour rule as SelectionOutline; drawn in the body's original sorting slot.
    Properties {_Color("Outline color",Color)=(.06,.065,.075,.9) _Width("Screen pixels",Int)=1 _CGOutlineEntity("Entity id",Float)=0}
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
            #pragma target 3.5
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D_FLOAT(_CGSharedOutlineMask);
            float4 _CGSharedOutlineLayout; // xy: mask pad in pixels, zw: camera pixel size
            float4x4 _CGSharedOutlineVP;   // the exact matrix the mask pass rasterized with
            half4 _Color;
            int _Width;
            float _CGOutlineEntity;
            struct Varyings {float4 positionCS:SV_POSITION;float4 maskCS:TEXCOORD0;};
            Varyings Vert(float4 positionOS:POSITION)
            {
                Varyings o;float3 world=TransformObjectToWorld(positionOS.xyz);
                o.positionCS=TransformWorldToHClip(world);o.maskCS=mul(_CGSharedOutlineVP,float4(world,1));return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                // Re-project with the mask pass matrix so the texel matches its rasterization on every API,
                // independent of how this camera's own target is flipped.
                float2 t=i.maskCS.xy/i.maskCS.w*.5+.5;
                #if UNITY_UV_STARTS_AT_TOP
                t.y=1-t.y;
                #endif
                int2 p=int2(floor(t*(_CGSharedOutlineLayout.zw+2*_CGSharedOutlineLayout.xy)));
                float self=_CGOutlineEntity;
                // Outside this body's own visible silhouette only.
                clip(abs(LOAD_TEXTURE2D(_CGSharedOutlineMask,p).r-self)-.5);
                half edge=0;
                [loop] for(int y=-_Width;y<=_Width;y++)
                [loop] for(int x=-_Width;x<=_Width;x++)
                    edge=max(edge,abs(LOAD_TEXTURE2D(_CGSharedOutlineMask,p+int2(x,y)).r-self)<.5?1:0);
                clip(edge-.5);return _Color;
            }
            ENDHLSL
        }
    }
}
