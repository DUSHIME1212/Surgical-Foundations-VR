// 20 · Hologram — scanline projection with rim light, flicker and occasional glitch (replay ghost, tutorial controller diagram).
Shader "SF/FX/Hologram"
{
    Properties
    {
        [HDR] _Color ("Colour", Color) = (0.36, 0.88, 0.78, 1)
        _Opacity ("Opacity", Range(0, 2)) = 1
        _ScanDensity ("Scan Lines (per m)", Float) = 120
        _ScanSpeed ("Scan Speed", Float) = 4
        _GlitchRate ("Glitch Rate", Float) = 6
        _GlitchAmount ("Glitch Amount (object units)", Float) = 0.05
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        HLSLINCLUDE
        #include "../Include/SFCommon.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            float _Opacity, _ScanDensity, _ScanSpeed, _GlitchRate, _GlitchAmount;
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
            #pragma vertex holoVert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            SFVaryings holoVert(SFAttributes v)
            {
                float3 pos = v.positionOS.xyz;
                float slot = floor(_Time.y * _GlitchRate);
                float glitch = step(0.92, SFHash21(float2(slot, 3.1))) * step(0.5, SFHash21(float2(slot, floor(pos.y * 20))));
                pos.x += glitch * (SFHash21(float2(slot, 7.7)) - 0.5) * _GlitchAmount;
                return SFVertFrom(v, pos);
            }

            half4 frag(SFVaryings i, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 n = normalize(i.normalWS) * IS_FRONT_VFACE(face, 1, -1);
                float3 v = GetWorldSpaceNormalizeViewDir(i.positionWS);
                float3 p = i.positionOS;
                float t = _Time.y;
                half rim = SFFresnel(n, v, 1.8);
                half scan = 0.5 + 0.5 * sin(i.positionWS.y * _ScanDensity * 6.2831 - t * _ScanSpeed);
                half flicker = 0.85 + 0.15 * SFNoise(float2(t * 20, 0));
                half a = saturate((0.15 + rim * 0.9) * (0.55 + 0.45 * scan) * flicker * _Opacity);
                return half4(_Color.rgb, a);
            }
            ENDHLSL
        }
    }
}
