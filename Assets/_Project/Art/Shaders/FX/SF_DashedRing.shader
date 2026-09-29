// 08 · Dashed ring — resolution-independent dashed target ring for port-site markers (Design screen 08). Use on a quad.
Shader "SF/FX/Dashed Ring"
{
    Properties
    {
        _Color ("Colour", Color) = (0.36, 0.88, 0.78, 1)
        _Radius ("Radius", Range(0, 0.5)) = 0.42
        _Thickness ("Thickness", Range(0, 0.2)) = 0.045
        _Dashes ("Dashes", Float) = 18
        _DashFill ("Dash Fill", Range(0, 1)) = 0.55
        _Spin ("Spin (turns/s)", Float) = 0.05
        _DotRadius ("Centre Dot Radius", Range(0, 0.3)) = 0.08
        _Pulse ("Pulse", Range(0, 1)) = 0.3
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        HLSLINCLUDE
        #include "../Include/SFCommon.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            float _Radius, _Thickness, _Dashes, _DashFill, _Spin, _DotRadius, _Pulse;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
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
                float2 c = i.uv - 0.5;
                float r = length(c);
                float aa = fwidth(r) * 1.2;
                float ring = 1 - smoothstep(0, aa, abs(r - _Radius) - _Thickness * 0.5);
                float ang = atan2(c.y, c.x) / 6.2831853 + 0.5 + t * _Spin;
                float phase = frac(ang * _Dashes);
                float w = fwidth(ang * _Dashes) + 1e-4;
                float dash = 1 - smoothstep(_DashFill - w, _DashFill + w, phase);
                float dotMask = (1 - smoothstep(_DotRadius - aa, _DotRadius + aa, r)) * step(1e-4, _DotRadius);
                float pulse = 1 + _Pulse * 0.35 * sin(t * 3);
                half a = saturate(max(ring * dash, dotMask) * _Color.a * pulse);
                return half4(_Color.rgb, a);
            }
            ENDHLSL
        }
    }
}
