// 02 · Brushed steel — directional micro-scratches in roughness and normal, used on instruments, trocars and stainless equipment.
Shader "SF/Surgical/Brushed Steel"
{
    Properties
    {
        _BaseColor ("Base Colour", Color) = (0.78, 0.8, 0.82, 1)
        _Metallic ("Metallic", Range(0, 1)) = 1
        _Smoothness ("Smoothness", Range(0, 1)) = 0.78
        [Enum(X,0,Y,1,Z,2)] _BrushAxis ("Brush Axis (object)", Float) = 2
        _BrushLength ("Streak Length (per m)", Float) = 6
        _BrushDensity ("Streak Density (per m)", Float) = 900
        _BrushStrength ("Streak Strength", Range(0, 1)) = 0.35
        _Wear ("Edge Wear / Smudges", Range(0, 1)) = 0.25
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
        #include "../Include/SFCommon.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            float _Metallic, _Smoothness, _BrushAxis, _BrushLength, _BrushDensity, _BrushStrength, _Wear;
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
                float along = _BrushAxis < 0.5 ? p.x : (_BrushAxis < 1.5 ? p.y : p.z);
                float across = _BrushAxis < 0.5 ? p.y + p.z * 0.73 : (_BrushAxis < 1.5 ? p.x + p.z * 0.73 : p.x + p.y * 0.73);
                float streak = SFNoise(float2(along * _BrushLength, across * _BrushDensity));
                streak = streak * 0.7 + 0.3 * SFNoise(float2(along * _BrushLength * 3.1, across * _BrushDensity * 0.37));
                float smudge = SFFbm3(p * 25) * _Wear;

                n = SFBumpFromHeight(n, i.positionWS, streak * 0.00015, 1);
                half smoothness = saturate(_Smoothness - (streak - 0.5) * _BrushStrength * 0.5 - smudge * 0.3);
                half3 albedo = _BaseColor.rgb * (0.94 + 0.06 * streak);
                return half4(SFLitPBR(i, n, albedo, _Metallic, smoothness, 1, 0), 1);
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
