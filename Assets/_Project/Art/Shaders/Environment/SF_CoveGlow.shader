// 18 · Cove glow — soft emissive light strip with a slow breathing pulse (lobby cove lighting, equipment status LEDs).
Shader "SF/Environment/Cove Glow"
{
    Properties
    {
        [HDR] _Color ("Colour", Color) = (0.36, 0.88, 0.78, 1)
        _Intensity ("Intensity", Float) = 2
        _Breath ("Breathing Amount", Range(0, 1)) = 0.15
        _BreathSpeed ("Breathing Speed (Hz)", Float) = 0.12
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        HLSLINCLUDE
        #include "../Include/SFCommon.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            float _Intensity, _Breath, _BreathSpeed;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            Blend One Zero
            ZWrite On ZTest LEqual Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex SFVert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            half4 frag(SFVaryings i, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 n = normalize(i.normalWS) * IS_FRONT_VFACE(face, 1, -1);
                float3 v = GetWorldSpaceNormalizeViewDir(i.positionWS);
                float3 p = i.positionOS;
                float t = _Time.y;
                float profile = 1 - abs(i.uv.y - 0.5) * 2;
                float breathe = 1 + _Breath * sin(t * _BreathSpeed * 6.2831);
                half3 c = _Color.rgb * _Intensity * (0.55 + 0.45 * profile) * breathe;
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
