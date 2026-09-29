// 07 · Guided highlight — additive rim + scanlines + pulse for Guided-mode cues on instruments and targets (FR-19).
Shader "SF/FX/Guided Highlight"
{
    Properties
    {
        [HDR] _Color ("Colour", Color) = (0.36, 0.88, 0.78, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 2
        _Base ("Base Fill", Range(0, 1)) = 0.25
        _PulseSpeed ("Pulse Speed", Float) = 2.5
        _ScanDensity ("Scan Lines (per m)", Float) = 60
        _ScanSpeed ("Scan Speed", Float) = 0.4
        _Opacity ("Opacity", Range(0, 2)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        HLSLINCLUDE
        #include "../Include/SFCommon.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            float _RimPower, _Base, _PulseSpeed, _ScanDensity, _ScanSpeed, _Opacity;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off ZTest LEqual Cull Back

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
                half rim = SFFresnel(n, v, _RimPower);
                half scan = step(0.5, frac(i.positionWS.y * _ScanDensity - t * _ScanSpeed)) * 0.2;
                half pulse = 0.65 + 0.35 * sin(t * _PulseSpeed);
                half a = saturate((rim + _Base + scan) * pulse * _Opacity * _Color.a);
                return half4(MixFog(_Color.rgb, i.fogFactor), a);
            }
            ENDHLSL
        }
    }
}
