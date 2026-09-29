// 01 · Wet tissue — mottled organ surface with vessels, subsurface wrap and a slowly moving wet sheen.
// Used on: cavity wall, liver, bowel, peritoneum, dissection tissue (seen mostly through the laparoscope).
Shader "SF/Surgical/Wet Tissue"
{
    Properties
    {
        _BaseColor ("Base Colour", Color) = (0.62, 0.2, 0.18, 1)
        _MottleColor ("Mottle Colour", Color) = (0.45, 0.1, 0.1, 1)
        _VeinColor ("Vessel Colour", Color) = (0.32, 0.04, 0.08, 1)
        _SSSColor ("Subsurface Colour", Color) = (0.95, 0.3, 0.22, 1)
        _MottleScale ("Mottle Scale (per m)", Float) = 40
        _VeinScale ("Vessel Scale (per m)", Float) = 18
        _VeinAmount ("Vessel Amount", Range(0, 1)) = 0.5
        _Wetness ("Wetness", Range(0, 1)) = 0.8
        _WetFlow ("Wet Sheen Flow", Float) = 0.04
        _Bump ("Surface Bump", Range(0, 4)) = 1.2
        _SSS ("Subsurface Strength", Range(0, 1)) = 0.4
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
        #include "../Include/SFCommon.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor, _MottleColor, _VeinColor, _SSSColor;
            float _MottleScale, _VeinScale, _VeinAmount, _Wetness, _WetFlow, _Bump, _SSS;
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

                float mottle = SFFbm3(p * _MottleScale);
                float veins = 1 - saturate(abs(SFFbm3(p * _VeinScale + 3.1) - 0.5) * 18); // thin ridges along the 0.5 iso-line
                veins *= _VeinAmount;

                half3 albedo = lerp(_BaseColor.rgb, _MottleColor.rgb, mottle * 0.7);
                albedo = lerp(albedo, _VeinColor.rgb, veins);

                float height = mottle * 0.004 - veins * 0.002;
                n = SFBumpFromHeight(n, i.positionWS, height, _Bump);

                // Wet film: smoothness drifts slowly so highlights crawl like moisture.
                float film = SFNoise3(p * 9 + float3(0, _Time.y * _WetFlow * 10, 0));
                half smoothness = saturate(_Wetness * (0.75 + 0.35 * film));

                half3 c = SFLitPBR(i, n, albedo, 0, smoothness, 1, 0);
                c += SFSubsurface(i, n, _SSSColor.rgb * albedo, 0.6) * _SSS;
                return half4(c, 1);
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
