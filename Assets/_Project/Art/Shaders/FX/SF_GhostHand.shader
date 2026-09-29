// 10 · Ghost hand — soft translucent demo hands for Guided mode (scrub steps, trocar angle). Depth-primed so it never self-overlaps.
Shader "SF/FX/Ghost Hand"
{
    Properties
    {
        _Color ("Colour", Color) = (0.78, 1, 0.94, 1)
        _Opacity ("Opacity", Range(0, 1)) = 0.55
        _RimPower ("Rim Power", Range(0.5, 6)) = 2.2
        _BandSpeed ("Scan Band Speed", Float) = 0.25
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        HLSLINCLUDE
        #include "../Include/SFCommon.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            float _Opacity, _RimPower, _BandSpeed;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "DepthPrime"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            ZWrite On ColorMask 0 Cull Back
            HLSLPROGRAM
            #pragma vertex SFVert
            #pragma fragment primeFrag
            #pragma multi_compile_instancing
            half4 primeFrag(SFVaryings i) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
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
                float band = exp(-pow((frac(i.positionWS.y * 0.8 - t * _BandSpeed) - 0.5) * 12, 2)) * 0.35;
                half a = saturate(_Opacity * (0.15 + rim * 0.85 + band));
                half3 c = _Color.rgb * (0.8 + band + rim * 0.5);
                return half4(c, a);
            }
            ENDHLSL
        }
    }
}
