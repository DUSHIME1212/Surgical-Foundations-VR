// 12 · Soap lather — cellular bubbles with thin-film iridescence and a coverage mask (scrub sink, hands during scrub).
Shader "SF/FX/Soap Lather"
{
    Properties
    {
        _Color ("Foam Colour", Color) = (0.96, 0.98, 1, 1)
        _BubbleScale ("Bubbles (per m)", Float) = 180
        _Coverage ("Coverage", Range(0, 1)) = 0.7
        _Iridescence ("Iridescence", Range(0, 1)) = 0.25
        _Drift ("Drift", Float) = 0.01
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        HLSLINCLUDE
        #include "../Include/SFCommon.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            float _BubbleScale, _Coverage, _Iridescence, _Drift;
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
                float2 q = SFPlanarUV(p, i.normalOS);
                float2 vor = SFVoronoi(q * _BubbleScale + t * _Drift * _BubbleScale);
                float wall = 1 - smoothstep(0, 0.08, vor.y - vor.x);
                float mask = 1 - smoothstep(_Coverage - 0.15, _Coverage, SFFbm(q * 12));
                half3 iri = 0.5 + 0.5 * cos(6.2831 * (vor.x * 3 + float3(0, 0.33, 0.67)));
                half3 c = _Color.rgb * (0.75 + SampleSH(n) * 0.8) + iri * _Iridescence * wall;
                half a = mask * saturate(0.3 + wall * 0.7);
                return half4(c, a);
            }
            ENDHLSL
        }
    }
}
