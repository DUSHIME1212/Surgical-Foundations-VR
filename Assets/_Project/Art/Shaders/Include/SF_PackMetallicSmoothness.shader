// Utility (not one of the 20): packs separate Metalness + Roughness maps into URP's MetallicSmoothness layout. Used by ImportedAssetsBuilder via Graphics.Blit.
Shader "Hidden/SF/PackMetallicSmoothness"
{
    Properties
    {
        _MainTex ("Unused", 2D) = "white" {}
        _MetalTex ("Metalness", 2D) = "black" {}
        _RoughTex ("Roughness", 2D) = "white" {}
        _HasMetal ("Has Metal Map", Float) = 1
        _MetalValue ("Metal Value (no map)", Float) = 0
    }
    SubShader
    {
        ZTest Always Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MetalTex); SAMPLER(sampler_MetalTex);
            TEXTURE2D(_RoughTex); SAMPLER(sampler_RoughTex);
            float _HasMetal, _MetalValue;
            struct A { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct V { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            V vert(A a) { V o; o.positionCS = TransformObjectToHClip(a.positionOS.xyz); o.uv = a.uv; return o; }
            float4 frag(V i) : SV_Target
            {
                float m = _HasMetal > 0.5 ? SAMPLE_TEXTURE2D(_MetalTex, sampler_MetalTex, i.uv).r : _MetalValue;
                float r = SAMPLE_TEXTURE2D(_RoughTex, sampler_RoughTex, i.uv).r;
                return float4(m, 0, 0, 1 - r);
            }
            ENDHLSL
        }
    }
}
