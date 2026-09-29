// 19 · View vignette — head-locked sphere that dims the world (pause, US-ACS-03) and/or tints the periphery (deviation edge vignette).
Shader "SF/FX/View Vignette"
{
    Properties
    {
        _Color ("Colour", Color) = (0.02, 0.04, 0.05, 1)
        _Opacity ("Uniform Opacity", Range(0, 1)) = 0.65
        _EdgeOpacity ("Edge Opacity", Range(0, 1)) = 0.3
        _Inner ("Edge Start", Range(0, 1)) = 0.25
        _Outer ("Edge End", Range(0, 1)) = 0.7
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+100" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        HLSLINCLUDE
        #include "../Include/SFCommon.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            float _Opacity, _EdgeOpacity, _Inner, _Outer;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off ZTest Always Cull Off

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
                float3 dir = normalize(i.positionWS - _WorldSpaceCameraPos);
                float3 fwd = -UNITY_MATRIX_V[2].xyz;
                float ang = 1 - saturate(dot(dir, fwd));
                float vig = smoothstep(_Inner, _Outer, ang);
                half a = saturate(_Opacity + vig * _EdgeOpacity);
                return half4(_Color.rgb, a);
            }
            ENDHLSL
        }
    }
}
