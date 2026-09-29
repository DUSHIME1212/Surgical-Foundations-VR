// 17 · Grid floor — anti-aliased world-space grid with a calm radial glow and slow pulse ring (lobby, matches the Design backdrop).
Shader "SF/Environment/Grid Floor"
{
    Properties
    {
        _BaseColor ("Base", Color) = (0.05, 0.1, 0.105, 1)
        _LineColor ("Lines", Color) = (0.12, 0.24, 0.24, 1)
        [HDR] _GlowColor ("Glow", Color) = (0.36, 0.88, 0.78, 1)
        _Cell ("Cell Size (m)", Float) = 1
        _Major ("Major Every N", Float) = 4
        _FadeRadius ("Fade Radius (m)", Float) = 7
        _GlowRadius ("Glow Radius (m)", Float) = 2.5
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        HLSLINCLUDE
        #include "../Include/SFCommon.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor, _LineColor, _GlowColor;
            float _Cell, _Major, _FadeRadius, _GlowRadius;
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
                float2 w = i.positionWS.xz;
                float2 gp = w / _Cell;
                float2 gr = abs(frac(gp - 0.5) - 0.5) / fwidth(gp);
                float minor = 1 - saturate(min(gr.x, gr.y));
                float2 gm = gp / _Major;
                float2 gmr = abs(frac(gm - 0.5) - 0.5) / fwidth(gm);
                float major = 1 - saturate(min(gmr.x, gmr.y) * 0.6);
                float r = length(w);
                float fade = 1 - smoothstep(_FadeRadius * 0.4, _FadeRadius, r);
                float glow = exp(-r * r / (_GlowRadius * _GlowRadius));
                float pr = frac(t * 0.06) * _FadeRadius;
                float ring = exp(-pow((r - pr) * 2.5, 2)) * (1 - pr / _FadeRadius);
                half3 c = _BaseColor.rgb + _LineColor.rgb * max(minor * 0.6, major) * fade;
                c += _GlowColor.rgb * (glow * 0.08 + ring * max(minor, major) * 0.5);
                return half4(MixFog(c, i.fogFactor), 1);
            }
            ENDHLSL
        }
    }
}
