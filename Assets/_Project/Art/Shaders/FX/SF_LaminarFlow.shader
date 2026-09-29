// 14 · Laminar airflow — faint downward streaks under the ceiling canopy (visual cue for the sterile field). Disabled on Quest by default.
Shader "SF/FX/Laminar Flow"
{
    Properties
    {
        _Color ("Colour", Color) = (0.8, 0.95, 1, 1)
        _Intensity ("Intensity", Range(0, 0.3)) = 0.05
        _StreakDensity ("Streak Density (per m)", Float) = 9
        _StreakLength ("Streak Length (per m)", Float) = 1.2
        _Speed ("Fall Speed", Float) = 0.35
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        HLSLINCLUDE
        #include "../Include/SFCommon.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            float _Intensity, _StreakDensity, _StreakLength, _Speed;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off ZTest LEqual Cull Off

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
                float s = SFNoise(float2((i.positionWS.x + i.positionWS.z * 0.7) * _StreakDensity, i.positionWS.y * _StreakLength + t * _Speed));
                s = pow(s, 6);
                float h = saturate(i.positionOS.y / max(SFObjectScale().y, 1e-4) + 0.5);
                half a = saturate(s * _Intensity * sin(h * 3.14159) * (1 - abs(dot(n, v)) * 0.5));
                return half4(_Color.rgb, a);
            }
            ENDHLSL
        }
    }
}
