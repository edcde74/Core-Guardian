Shader "CoreGuardian/BodyHitFlash"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture",2D)="white"{}
        [HideInInspector] _Color("Tint",Color)=(1,1,1,1)
        [HideInInspector] _RendererColor("RendererColor",Color)=(1,1,1,1)
        _HitFlashAmount("Hit Amount",Range(0,1))=0
        _HitFlashTint("Hit Tint",Color)=(1,.945,.831,1)
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
            #pragma vertex HitVertex
            #pragma fragment HitFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE
            struct Attributes {COMMON_2D_INPUTS half4 color:COLOR; UNITY_SKINNED_VERTEX_INPUTS};
            struct Varyings {COMMON_2D_OUTPUTS half4 color:COLOR;};
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _HitFlashTint;
                half _HitFlashAmount;
            CBUFFER_END
            Varyings HitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS=UnityFlipSprite(input.positionOS,unity_SpriteProps.xy);
                Varyings output=CommonUnlitVertex(input);
                output.color=input.color*_Color*unity_SpriteColor;
                return output;
            }
            half4 HitFragment(Varyings input):SV_Target
            {
                half4 color=CommonUnlitFragment(input,input.color);
                color.rgb=lerp(color.rgb,_HitFlashTint.rgb,saturate(_HitFlashAmount));
                return color;
            }
            ENDHLSL
        }
    }
}
