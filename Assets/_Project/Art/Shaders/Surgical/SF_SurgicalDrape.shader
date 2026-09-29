// 03 · Surgical drape / gown fabric — non-woven weave, soft folds and a cloth sheen at grazing angles.
Shader "SF/Surgical/Surgical Drape"
{
    Properties
    {
        _BaseColor ("Base Colour", Color) = (0.18, 0.44, 0.45, 1)
        _SheenColor ("Sheen Colour", Color) = (0.55, 0.8, 0.8, 1)
        _SheenPower ("Sheen Power", Range(1, 8)) = 4
        _WeaveScale ("Weave (threads per m)", Float) = 450
        _WeaveStrength ("Weave Strength", Range(0, 1)) = 0.35
        _FoldScale ("Fold Scale (per m)", Float) = 3
        _FoldStrength ("Fold Strength", Range(0, 4)) = 1.5
        _Smoothness ("Smoothness", Range(0, 1)) = 0.12
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
        #include "../Include/SFCommon.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor, _SheenColor;
            float _SheenPower, _WeaveScale, _WeaveStrength, _FoldScale, _FoldStrength, _Smoothness;
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
                float2 uv = SFPlanarUV(p, i.normalOS) * _WeaveScale;
                float weave = (sin(uv.x * 6.2831) * sin(uv.y * 6.2831)) * 0.5 + 0.5;
                float fibres = SFNoise(uv * 0.37);
                float folds = SFFbm3(p * _FoldScale);

                n = SFBumpFromHeight(n, i.positionWS, folds * 0.03 + weave * 0.0002, _FoldStrength);
                half3 v = GetWorldSpaceNormalizeViewDir(i.positionWS);
                half3 albedo = _BaseColor.rgb * (1 - _WeaveStrength * 0.25 * (weave * 0.6 + fibres * 0.4)) * (0.85 + 0.3 * folds);
                half3 sheen = _SheenColor.rgb * SFFresnel(n, v, _SheenPower) * 0.35;
                half3 c = SFLitPBR(i, n, albedo, 0, _Smoothness, lerp(0.7, 1, folds), 0);
                return half4(c + sheen * SampleSH(n) * 2, 1);
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
