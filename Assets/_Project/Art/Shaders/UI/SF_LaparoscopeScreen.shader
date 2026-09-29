// 15 · Laparoscope monitor — scope optics on the camera RenderTexture: barrel distortion, circular field, chromatic fringe, grain.
// Keeps _BaseMap/_BaseColor/_EmissionMap/_EmissionColor so LaparoscopeFeed can drive it with a MaterialPropertyBlock.
Shader "SF/UI/Laparoscope Screen"
{
    Properties
    {
        _BaseMap ("Camera Feed", 2D) = "white" {}
        _BaseColor ("Tint", Color) = (1, 1, 1, 1)
        _EmissionMap ("Emission (unused)", 2D) = "white" {}
        [HDR] _EmissionColor ("Brightness (power-on)", Color) = (0, 0, 0, 1)
        _Distortion ("Barrel Distortion", Range(0, 0.5)) = 0.15
        _Aspect ("Screen Aspect", Float) = 1.7
        _ScopeRadius ("Scope Field Radius", Range(0.5, 2)) = 1.25
        _ScopeSoftness ("Field Edge Softness", Range(0, 0.2)) = 0.04
        _Fringe ("Chromatic Fringe", Range(0, 0.02)) = 0.004
        _Vignette ("Vignette", Range(0, 1)) = 0.35
        _Grain ("Grain", Range(0, 0.2)) = 0.035
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        HLSLINCLUDE
        #include "../Include/SFCommon.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor, _EmissionColor;
            float _Distortion, _Aspect, _ScopeRadius, _ScopeSoftness, _Fringe, _Vignette, _Grain;
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
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            half4 frag(SFVaryings i, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 n = normalize(i.normalWS) * IS_FRONT_VFACE(face, 1, -1);
                float3 v = GetWorldSpaceNormalizeViewDir(i.positionWS);
                float3 p = i.positionOS;
                float t = _Time.y;
                float2 c = i.uv - 0.5;
                float2 ca = c * float2(_Aspect, 1);
                float r2 = dot(ca, ca);
                float2 uv = 0.5 + c * (1 + _Distortion * r2);
                half3 col;
                col.r = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv + c * _Fringe).r;
                col.g = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).g;
                col.b = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv - c * _Fringe).b;
                float circ = length(ca) * 2;
                float field = 1 - smoothstep(_ScopeRadius - _ScopeSoftness, _ScopeRadius + _ScopeSoftness, circ);
                float vig = 1 - _Vignette * pow(saturate(circ / _ScopeRadius), 3);
                float grain = (SFHash21(i.uv * 1024 + frac(t * 17.3) * 91.7) - 0.5) * _Grain;
                col = (col * vig + grain) * field * _BaseColor.rgb * _EmissionColor.rgb;
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
