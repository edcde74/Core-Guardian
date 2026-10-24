Shader "CoreGuardian/SharedOutlineOutput"
{
    // Per-body outline output that reads the shared entity-id mask, drawn by a SpriteRenderer in the body's original
    // sorting slot. Entity ids 1..255 arrive as the renderer alpha byte (exact in vertex colour and in unity_SpriteColor;
    // linear colour-space conversion leaves alpha untouched), so every output shares one material and sprite batches.
    // _CGOutlineEntity > 0 (property block) overrides it for ids beyond 255.
    // Same square kernel and colour rule as SelectionOutline.
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
            struct Varyings {float4 positionCS:SV_POSITION;float4 maskCS:TEXCOORD0;nointerpolation float entity:TEXCOORD1;};
            Varyings Vert(float4 positionOS:POSITION,float4 color:COLOR)
            {
                Varyings o;float3 world=TransformObjectToWorld(positionOS.xyz);
                o.positionCS=TransformWorldToHClip(world);o.maskCS=mul(_CGSharedOutlineVP,float4(world,1));
                o.entity=_CGOutlineEntity>0?_CGOutlineEntity:round(color.a*unity_SpriteColor.a*255);return o;
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
                float self=i.entity;
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
