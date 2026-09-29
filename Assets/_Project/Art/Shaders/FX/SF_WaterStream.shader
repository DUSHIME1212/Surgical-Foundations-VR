// 11 · Water stream — wobbling, scrolling tap water for the scrub sink (use on a thin cylinder under WaterSocket_*).
Shader "SF/FX/Water Stream"
{
    Properties
    {
        _Color ("Highlight Colour", Color) = (0.92, 0.97, 1, 1)
        _DeepColor ("Body Colour", Color) = (0.55, 0.72, 0.8, 1)
        _Opacity ("Opacity", Range(0, 1)) = 0.55
        _Speed ("Flow Speed", Float) = 3
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        HLSLINCLUDE
        #include "../Include/SFCommon.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _Color, _DeepColor;
            float _Opacity, _Speed;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off ZTest LEqual Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex waterVert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            SFVaryings waterVert(SFAttributes v)
            {
                float3 pos = v.positionOS.xyz;
                float wobble = sin(pos.y * 30 + _Time.y * 28) * 0.12 + sin(pos.y * 71 - _Time.y * 41) * 0.06;
                pos.xz *= 1 + wobble;
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
                float flow = SFFbm(float2(i.uv.x * 6, i.uv.y * 4 + t * _Speed));
                float streaks = SFNoise(float2(i.uv.x * 22, i.uv.y * 2 + t * _Speed * 1.3));
                half rim = SFFresnel(n, v, 1.5);
                half ends = smoothstep(0, 0.08, i.uv.y) * smoothstep(1, 0.92, i.uv.y);
                half a = saturate(_Opacity * (0.3 + rim * 0.6 + streaks * 0.35)) * ends;
                half3 c = lerp(_DeepColor.rgb, _Color.rgb, saturate(flow + rim * 0.5));
                c *= 0.6 + SampleSH(n) * 1.2;
                return half4(MixFog(c, i.fogFactor), a);
            }
            ENDHLSL
        }
    }
}
