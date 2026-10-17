Shader "CoreGuardian/EnvironmentContrast"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture",2D)="white"{}
        [HideInInspector] _Color("Tint",Color)=(1,1,1,1)
        [HideInInspector] _RendererColor("RendererColor",Color)=(1,1,1,1)
        _DetailContrast("Detail Contrast",Range(0,1))=1
        _LuminancePivot("Luminance Pivot",Range(0,1))=.18
        _InteriorOnly("Preserve Outer Ruins",Float)=0
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #pragma vertex EnvironmentVertex
            #pragma fragment EnvironmentFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE
            struct Attributes {COMMON_2D_INPUTS half4 color:COLOR; UNITY_SKINNED_VERTEX_INPUTS};
            struct Varyings {COMMON_2D_OUTPUTS half4 color:COLOR;};
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _DetailContrast;
                half _LuminancePivot;
                half _InteriorOnly;
            CBUFFER_END
            Varyings EnvironmentVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS=UnityFlipSprite(input.positionOS,unity_SpriteProps.xy);
                Varyings output=CommonUnlitVertex(input);
                output.color=input.color*_Color*unity_SpriteColor;
                return output;
            }
            half4 EnvironmentFragment(Varyings input):SV_Target
            {
                half4 color=CommonUnlitFragment(input,input.color);
                // Compress luminance only: preserve hue, alpha, sampling and sprite silhouette.
                half luminance=dot(color.rgb,half3(.2126,.7152,.0722));
                half target=lerp(_LuminancePivot,luminance,_DetailContrast);
                half3 compressed=color.rgb*(target/max(luminance,.0001h));
                // The courtyard texture includes the outer ruins; fade the adjustment before those edges.
                half2 edge=min(input.uv,1-input.uv);
                half interior=smoothstep(.025h,.065h,edge.x)*smoothstep(.025h,.085h,edge.y);
                color.rgb=lerp(color.rgb,compressed,lerp(1,interior,saturate(_InteriorOnly)));
                return color;
            }
            ENDHLSL
        }
    }
}
