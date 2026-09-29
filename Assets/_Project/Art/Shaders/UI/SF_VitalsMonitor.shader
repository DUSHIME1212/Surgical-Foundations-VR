// 16 · Vitals monitor — procedural ECG / SpO2 pleth / respiration traces with a sweep eraser (anaesthesia machine screen).
Shader "SF/UI/Vitals Monitor"
{
    Properties
    {
        _Background ("Background", Color) = (0.015, 0.035, 0.035, 1)
        _GridColor ("Grid", Color) = (0.05, 0.12, 0.11, 1)
        [HDR] _EcgColor ("ECG", Color) = (0.23, 1.2, 0.63, 1)
        [HDR] _PlethColor ("Pleth", Color) = (0.25, 0.9, 1.2, 1)
        [HDR] _RespColor ("Resp", Color) = (1.2, 1.0, 0.3, 1)
        _HeartRate ("Heart Rate (bpm)", Float) = 72
        _RespRate ("Resp Rate (/min)", Float) = 14
        _SweepSpeed ("Sweep (screens/s)", Float) = 0.2
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        HLSLINCLUDE
        #include "../Include/SFCommon.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _Background, _GridColor, _EcgColor, _PlethColor, _RespColor;
            float _HeartRate, _RespRate, _SweepSpeed;
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
                float2 uv = i.uv;
                const float traceW = 0.74;
                float xn = uv.x / traceW;
                float sweeps = t * _SweepSpeed;
                float head = frac(sweeps);
                float s = (floor(sweeps) - (xn > head ? 1 : 0) + xn) / _SweepSpeed; // signal time shown at this x

                float hb = frac(s * _HeartRate / 60.0);
                #define G(x, m, w) exp(-pow(((x) - (m)) / (w), 2))
                float ecg = 0.1 * G(hb, 0.18, 0.025) - 0.12 * G(hb, 0.30, 0.008) + 1.0 * G(hb, 0.33, 0.011) - 0.25 * G(hb, 0.36, 0.01) + 0.28 * G(hb, 0.56, 0.04);
                float pb = frac(s * _HeartRate / 60.0 - 0.1);
                float pleth = smoothstep(0, 0.14, pb) * (1 - smoothstep(0.14, 0.95, pb)) + 0.18 * G(pb, 0.42, 0.05);
                float resp = 0.5 + 0.5 * sin(s * 6.2831 * _RespRate / 60.0);

                half3 col = _Background.rgb;
                float2 g = abs(frac(uv * float2(24, 12)) - 0.5);
                col = lerp(col, _GridColor.rgb, (1 - smoothstep(0.0, 0.03, min(g.x, g.y))) * 0.6);

                float inTrace = step(uv.x, traceW) * (1 - step(0, xn - head) * step(xn - head, 0.03));
                float3 bands = float3(0.68, 0.38, 0.08);
                float3 heights = float3(0.24, 0.22, 0.2);
                float3 vals = float3(ecg * 0.8 + 0.15, pleth, resp);
                half3 cols[3] = { _EcgColor.rgb, _PlethColor.rgb, _RespColor.rgb };
                [unroll] for (int k = 0; k < 3; k++)
                {
                    float d = uv.y - (bands[k] + vals[k] * heights[k]);
                    float aa = fwidth(d) + 0.0015;
                    float trace = saturate(1 - abs(d) / (aa * 1.6)) * inTrace;
                    col = lerp(col, cols[k], trace);
                    // numeric block placeholder on the right
                    float2 box = abs(uv - float2(0.87, bands[k] + heights[k] * 0.5)) - float2(0.1, heights[k] * 0.42);
                    float boxMask = 1 - step(0, max(box.x, box.y));
                    col = lerp(col, cols[k] * 0.22, boxMask);
                    float2 bar = abs(uv - float2(0.87, bands[k] + heights[k] * 0.5)) - float2(0.06, 0.02);
                    col = lerp(col, cols[k], (1 - step(0, max(bar.x, bar.y))) * 0.9);
                }
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
