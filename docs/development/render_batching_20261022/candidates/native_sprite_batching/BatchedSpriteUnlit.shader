// Sprite batching uses the legacy per-renderer color/flip interface. Sampling
// and straight-alpha blending retain the URP unlit fragment semantics.
Shader "CoreGuardian/BatchedSpriteUnlit"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [MaterialToggle] _ZWrite("ZWrite", Float) = 0
        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="True"}
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite [_ZWrite]
        Pass
        {
            HLSLPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment UnlitFragment
            #pragma multi_compile_instancing
            #include "UnitySprites.cginc"
            fixed4 UnlitFragment(v2f input):SV_Target
            {return SampleSpriteTexture(input.texcoord)*input.color;}
            ENDHLSL
        }
    }
}
