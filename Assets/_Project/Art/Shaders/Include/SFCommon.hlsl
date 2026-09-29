// Surgical Foundations — shared shader library (URP 17, Quest-friendly, single-pass instanced XR safe).
#ifndef SF_COMMON_INCLUDED
#define SF_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// ───────────── vertex I/O ─────────────

struct SFAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
    float4 tangentOS  : TANGENT;
    float2 uv         : TEXCOORD0;
    float4 color      : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct SFVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv         : TEXCOORD0;
    float3 positionWS : TEXCOORD1;
    float3 normalWS   : TEXCOORD2;
    float3 positionOS : TEXCOORD3;   // object space × object scale (metres, sticks to the object)
    float3 normalOS   : TEXCOORD4;
    float4 color      : TEXCOORD5;
    float  fogFactor  : TEXCOORD6;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

float3 SFObjectScale()
{
    return float3(length(UNITY_MATRIX_M._m00_m10_m20), length(UNITY_MATRIX_M._m01_m11_m21), length(UNITY_MATRIX_M._m02_m12_m22));
}

SFVaryings SFVertFrom(SFAttributes v, float3 positionOS)
{
    SFVaryings o = (SFVaryings)0;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    VertexPositionInputs p = GetVertexPositionInputs(positionOS);
    o.positionCS = p.positionCS;
    o.positionWS = p.positionWS;
    o.normalWS = TransformObjectToWorldNormal(v.normalOS);
    o.positionOS = positionOS * SFObjectScale();
    o.normalOS = v.normalOS;
    o.uv = v.uv;
    o.color = v.color;
    o.fogFactor = ComputeFogFactor(p.positionCS.z);
    return o;
}

SFVaryings SFVert(SFAttributes v) { return SFVertFrom(v, v.positionOS.xyz); }

// ───────────── noise ─────────────

float SFHash21(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

float SFHash31(float3 p)
{
    p = frac(p * 0.3183099 + 0.1);
    p *= 17.0;
    return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
}

float SFNoise(float2 p)
{
    float2 i = floor(p), f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(SFHash21(i), SFHash21(i + float2(1, 0)), u.x),
                lerp(SFHash21(i + float2(0, 1)), SFHash21(i + float2(1, 1)), u.x), u.y);
}

float SFNoise3(float3 p)
{
    float3 i = floor(p), f = frac(p);
    float3 u = f * f * (3.0 - 2.0 * f);
    float n000 = SFHash31(i), n100 = SFHash31(i + float3(1, 0, 0));
    float n010 = SFHash31(i + float3(0, 1, 0)), n110 = SFHash31(i + float3(1, 1, 0));
    float n001 = SFHash31(i + float3(0, 0, 1)), n101 = SFHash31(i + float3(1, 0, 1));
    float n011 = SFHash31(i + float3(0, 1, 1)), n111 = SFHash31(i + float3(1, 1, 1));
    return lerp(lerp(lerp(n000, n100, u.x), lerp(n010, n110, u.x), u.y),
                lerp(lerp(n001, n101, u.x), lerp(n011, n111, u.x), u.y), u.z);
}

float SFFbm(float2 p)
{
    float s = 0, a = 0.5;
    [unroll] for (int k = 0; k < 4; k++) { s += a * SFNoise(p); p = p * 2.03 + 17.1; a *= 0.5; }
    return s;
}

float SFFbm3(float3 p)
{
    float s = 0, a = 0.5;
    [unroll] for (int k = 0; k < 4; k++) { s += a * SFNoise3(p); p = p * 2.01 + 11.7; a *= 0.5; }
    return s;
}

// Cellular noise: x = distance to nearest feature, y = distance to second nearest.
float2 SFVoronoi(float2 p)
{
    float2 i = floor(p), f = frac(p);
    float d1 = 8, d2 = 8;
    [unroll] for (int y = -1; y <= 1; y++)
    [unroll] for (int x = -1; x <= 1; x++)
    {
        float2 g = float2(x, y);
        float2 o = float2(SFHash21(i + g), SFHash21(i + g + 31.7));
        float d = length(g + o - f);
        if (d < d1) { d2 = d1; d1 = d; } else if (d < d2) { d2 = d; }
    }
    return float2(d1, d2);
}

// ───────────── shading helpers ─────────────

half SFFresnel(half3 n, half3 v, half power) { return pow(1.0h - saturate(dot(n, v)), power); }

/// Bump from any scalar height using screen-space derivatives (Mikkelsen surface gradient). No tangents or UVs needed.
float3 SFBumpFromHeight(float3 n, float3 positionWS, float height, float strength)
{
    float3 dpdx = ddx(positionWS), dpdy = ddy(positionWS);
    float3 r1 = cross(dpdy, n), r2 = cross(n, dpdx);
    float det = dot(dpdx, r1);
    float3 grad = sign(det) * (ddx(height) * r1 + ddy(height) * r2);
    return normalize(abs(det) * n - strength * grad);
}

/// Dominant-axis planar coordinates in metres (cheap triplanar for procedural patterns on primitives).
float2 SFPlanarUV(float3 p, float3 n)
{
    float3 a = abs(n);
    return a.x > a.y && a.x > a.z ? p.zy : (a.y > a.z ? p.xz : p.xy);
}

/// Full URP PBR (main + additional lights, shadows, probes, reflection probes, fog).
half3 SFLitPBR(SFVaryings i, float3 normalWS, half3 albedo, half metallic, half smoothness, half occlusion, half3 emission)
{
    InputData d = (InputData)0;
    d.positionWS = i.positionWS;
    d.normalWS = normalize(normalWS);
    d.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.positionWS);
    d.shadowCoord = TransformWorldToShadowCoord(i.positionWS);
    d.fogCoord = i.fogFactor;
    d.bakedGI = SampleSH(d.normalWS);
    d.shadowMask = half4(1, 1, 1, 1);
    d.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);

    SurfaceData s = (SurfaceData)0;
    s.albedo = albedo;
    s.metallic = metallic;
    s.specular = half3(0, 0, 0);
    s.smoothness = smoothness;
    s.occlusion = occlusion;
    s.emission = emission;
    s.alpha = 1;
    s.normalTS = half3(0, 0, 1);
    half4 c = UniversalFragmentPBR(d, s);
    return MixFog(c.rgb, i.fogFactor);
}

/// Wrap-lighting term for subsurface-looking materials (skin, tissue). Adds light that bleeds past the terminator.
half3 SFSubsurface(SFVaryings i, float3 normalWS, half3 tint, half wrap)
{
    Light l = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
    half ndl = dot(normalWS, l.direction);
    half w = saturate((ndl + wrap) / (1 + wrap)) - saturate(ndl);
    return tint * w * l.color * l.distanceAttenuation;
}

#endif
