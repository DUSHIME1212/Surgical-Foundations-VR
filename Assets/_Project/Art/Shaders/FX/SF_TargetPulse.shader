// 09 · Target pulse — expanding rings from a solid core: the Guided-mode port target-zone pulse (asset list: UI & feedback).
Shader "SF/FX/Target Pulse"
{
    Properties
    {
        [HDR] _Color ("Colour", Color) = (0.36, 0.88, 0.78, 1)
        _Speed ("Speed", Float) = 0.6
        _MaxRadius ("Max Radius", Range(0, 0.5)) = 0.48
        _Width ("Ring Width", Range(0, 0.1)) = 0.025
        _CoreRadius ("Core Radius", Range(0, 0.3)) = 0.1
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        HLSLINCLUDE
        #include "../Include/SFCommon.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            float _Speed, _MaxRadius, _Width, _CoreRadius;
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
                float r = length(i.uv - 0.5);
                float aa = fwidth(r);
                float rings = 0;
                [unroll] for (int k = 0; k < 3; k++)
                {
                    float ph = frac(t * _Speed + k / 3.0);
                    rings += (1 - smoothstep(0, _Width + aa, abs(r - ph * _MaxRadius))) * (1 - ph);
                }
                float core = 1 - smoothstep(_CoreRadius - aa, _CoreRadius + aa, r);
                half a = saturate(rings * 0.8 + core) * _Color.a;
                return half4(_Color.rgb, a);
            }
            ENDHLSL
        }
    }
}
