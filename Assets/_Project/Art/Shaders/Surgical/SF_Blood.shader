// 06 · Blood pool — glossy, darkening at the centre, spreading with _Spread (0–1) along a noisy edge. Drive _Spread from script.
Shader "SF/Surgical/Blood Pool"
{
    Properties
    {
        _BaseColor ("Fresh Colour", Color) = (0.55, 0.02, 0.04, 1)
        _DarkColor ("Pooled Colour", Color) = (0.22, 0.0, 0.02, 1)
        _Spread ("Spread", Range(0, 1)) = 0.6
        _EdgeNoise ("Edge Noise", Range(0, 1)) = 0.35
        _Smoothness ("Smoothness", Range(0, 1)) = 0.92
        _Ripple ("Surface Ripple", Range(0, 1)) = 0.3
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType" = "TransparentCutout" "Queue" = "AlphaTest" "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
        #include "../Include/SFCommon.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor, _DarkColor;
            float _Spread, _EdgeNoise, _Smoothness, _Ripple;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex SFVert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP

            half4 frag(SFVaryings i, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 n = normalize(i.normalWS) * IS_FRONT_VFACE(face, 1, -1);
                float3 p = i.positionOS;
                float2 c = i.uv - 0.5;
                float r = length(c) * 2;
                float edge = SFFbm(i.uv * 6 + 3.3) * _EdgeNoise;
                float d = _Spread - (r + edge - _EdgeNoise * 0.5);
                clip(d);
                float depth = saturate(d * 4);
                float ripple = SFNoise(i.uv * 40 + _Time.y * 0.3) * _Ripple;
                n = SFBumpFromHeight(n, i.positionWS, ripple * 0.0005 + depth * 0.002, 1);
                half3 albedo = lerp(_BaseColor.rgb, _DarkColor.rgb, depth);
                return half4(SFLitPBR(i, n, albedo, 0, _Smoothness, 1, 0), 1);
            }
            ENDHLSL
        }

    }
    FallBack "Universal Render Pipeline/Lit"
}
