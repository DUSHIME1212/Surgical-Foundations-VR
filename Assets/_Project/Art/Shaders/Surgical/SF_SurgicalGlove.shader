// 04 · Surgical glove — latex sheen with a contamination state that spreads as a coral stain and pulses (FR-07).
Shader "SF/Surgical/Surgical Glove"
{
    Properties
    {
        _BaseColor ("Base Colour", Color) = (0.85, 0.78, 0.62, 1)
        _Smoothness ("Smoothness", Range(0, 1)) = 0.6
        _ContamColor ("Contamination Colour", Color) = (1, 0.54, 0.47, 1)
        _Contamination ("Contamination", Range(0, 1)) = 0
        _PulseSpeed ("Pulse Speed", Float) = 3
        _StainScale ("Stain Scale (per m)", Float) = 30
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
        #include "../Include/SFCommon.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor, _ContamColor;
            float _Smoothness, _Contamination, _PulseSpeed, _StainScale;
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
                float stain = SFFbm3(p * _StainScale);
                float mask = smoothstep(1 - _Contamination, 1 - _Contamination + 0.08, stain * 0.6 + _Contamination * 0.55);
                mask *= step(0.001, _Contamination);
                float micro = SFNoise3(p * 400);
                n = SFBumpFromHeight(n, i.positionWS, micro * 0.00008, 1);
                half3 albedo = lerp(_BaseColor.rgb, _ContamColor.rgb * 0.8, mask);
                half pulse = (0.5 + 0.5 * sin(_Time.y * _PulseSpeed)) * mask;
                half3 emission = _ContamColor.rgb * pulse * 0.6;
                return half4(SFLitPBR(i, n, albedo, 0, _Smoothness * (1 - mask * 0.3), 1, emission), 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0 Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex SFShadowVert
            #pragma fragment SFShadowFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "../Include/SFPasses.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On ColorMask R Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex SFDepthVert
            #pragma fragment SFDepthFrag
            #pragma multi_compile_instancing
            #include "../Include/SFPasses.hlsl"
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
