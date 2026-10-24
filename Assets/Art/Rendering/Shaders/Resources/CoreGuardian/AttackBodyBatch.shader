Shader "CoreGuardian/AttackBodyBatch"
{
    Properties
    {
        [PerRendererData] _MainTex("Shared carrier",2D)="white"{}
        _BodyTextures("Original RGBA textures",2DArray)=""{}
        [HideInInspector] _Color("Tint",Color)=(1,1,1,1)
        [HideInInspector] _RendererColor("Renderer tint",Color)=(1,1,1,1)
        [HideInInspector] _Flip("Flip",Vector)=(1,1,1,1)
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="True"}
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            TEXTURE2D_ARRAY(_BodyTextures);SAMPLER(sampler_BodyTextures);
            StructuredBuffer<float4> _DisplayColors;
            // Deliberately outside UnityPerMaterial: use native sprite batching, not separate SRP draws.
            half4 _Color;
            struct Attributes {COMMON_2D_INPUTS half4 color:COLOR;};
            struct Varyings {COMMON_2D_OUTPUTS half4 color:COLOR;nointerpolation float layer:TEXCOORD1;};
            Varyings Vert(Attributes input)
            {
                SetUpSpriteInstanceProperties();input.positionOS=UnityFlipSprite(input.positionOS,unity_SpriteProps.xy);
                Varyings o=(Varyings)0;UNITY_SETUP_INSTANCE_ID(input);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS=TransformObjectToHClip(input.positionOS);o.uv=input.uv;o.color=input.color*_Color*unity_SpriteColor;
                uint index=(uint)max(0,floor(o.color.a*255.0+.5)-1);
                o.layer=index%5;o.color=(half4)_DisplayColors[index];return o;
            }
            half4 Frag(Varyings input):SV_Target
            {
                float4 mainTex=input.color*SAMPLE_TEXTURE2D_ARRAY(_BodyTextures,sampler_BodyTextures,input.uv,input.layer);return mainTex;
            }
            ENDHLSL
        }
    }
}
