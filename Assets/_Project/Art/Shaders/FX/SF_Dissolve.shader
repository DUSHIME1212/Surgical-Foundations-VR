// 13 · Dissolve — lit surface that burns away along 3D noise with a glowing edge (spawn/despawn, removing a contaminated glove).
Shader "SF/FX/Dissolve"
{
    Properties
    {
        _BaseColor ("Base Colour", Color) = (0.8, 0.82, 0.84, 1)
        _Metallic ("Metallic", Range(0, 1)) = 0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        _Dissolve ("Dissolve", Range(0, 1)) = 0.3
        _NoiseScale ("Noise Scale (per m)", Float) = 20
        [HDR] _EdgeColor ("Edge Colour", Color) = (0.36, 3.5, 3.1, 1)
        _EdgeWidth ("Edge Width", Range(0, 0.2)) = 0.05
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType" = "TransparentCutout" "Queue" = "AlphaTest" "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
        #include "../Include/SFCommon.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor, _EdgeColor;
            float _Metallic, _Smoothness, _Dissolve, _NoiseScale, _EdgeWidth;
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
                float noise = SFFbm3(p * _NoiseScale);
                float d = noise - _Dissolve * 1.05;
                clip(d);
                half3 emission = _EdgeColor.rgb * (1 - smoothstep(0, _EdgeWidth, d)) * step(0.001, _Dissolve);
                return half4(SFLitPBR(i, n, _BaseColor.rgb, _Metallic, _Smoothness, 1, emission), 1);
            }
            ENDHLSL
        }

    }
    FallBack "Universal Render Pipeline/Lit"
}
