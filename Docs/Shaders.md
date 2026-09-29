# Surgical Foundations — Shader Library

20 hand-written URP 17 HLSL shaders in `Assets/_Project/Art/Shaders/`. All are SRP-Batcher compatible, GPU-instancing and single-pass-instanced XR ready, and use procedural detail (no extra textures) so they stay cheap on Quest 3. Shared code: `Include/SFCommon.hlsl` (noise, bump-from-height, PBR helper) and `Include/SFPasses.hlsl` (shadow/depth passes).

Preview: `Scenes/Tools/91_ShaderGallery` (not in the build) — render in `Docs/ShaderGallery.png`.

| # | Shader | Type | Used on | Key properties |
|---|---|---|---|---|
| 01 | SF/Surgical/Wet Tissue | Lit (PBR + wrap SSS) | Cavity wall, liver, bowel, peritoneum, dissection tissue | Mottle/vessel scale, Wetness, Wet Flow, SSS |
| 02 | SF/Surgical/Brushed Steel | Lit | Instruments, trocars, stainless equipment | Brush Axis, Density, Strength, Wear |
| 03 | SF/Surgical/Surgical Drape | Lit + sheen | Drapes, gowns, scrubs, caps, masks | Weave, Fold scale/strength, Sheen |
| 04 | SF/Surgical/Surgical Glove | Lit | Gloves, hands | **Contamination 0–1** (coral stain + pulse, FR-07) |
| 05 | SF/Surgical/Skin | Lit (PBR + wrap SSS) | Patient, placeholder characters | SSS, Pores, **Prep** (antiseptic tint) |
| 06 | SF/Surgical/Blood Pool | Lit, alpha-clip | Vessel-injury branch at the right port | **Spread 0–1** (animated), Edge noise |
| 07 | SF/FX/Guided Highlight | Additive | Guided-mode cues, port dots | Rim, Pulse, Scanlines |
| 08 | SF/FX/Dashed Ring | Alpha | Port-site target rings (screen 08) | Radius, Dashes, Spin |
| 09 | SF/FX/Target Pulse | Additive | Port target-zone pulse | Speed, Max radius |
| 10 | SF/FX/Ghost Hand | Alpha, depth-primed | Ghost-hand scrub demo above the sink | Opacity, Rim, Scan band |
| 11 | SF/FX/Water Stream | Alpha, vertex wobble | Scrub-sink tap water | Flow speed, Opacity |
| 12 | SF/FX/Soap Lather | Alpha | Scrub-sink trough | Bubble scale, Coverage, Iridescence |
| 13 | SF/FX/Dissolve | Lit, alpha-clip, glow edge | Spawn/despawn, glove removal | **Dissolve 0–1**, Edge colour |
| 14 | SF/FX/Laminar Flow | Additive volume | Under the ceiling canopy (off on Quest) | Intensity, Fall speed |
| 15 | SF/UI/Laparoscope Screen | Unlit | Tower monitor (scope RenderTexture) | Barrel distortion, Scope field, Fringe, Grain |
| 16 | SF/UI/Vitals Monitor | Unlit, procedural | Anaesthesia + patient monitors | Heart rate, Resp rate, Sweep |
| 17 | SF/Environment/Grid Floor | Unlit, procedural | Lobby floor | Cell size, Glow, Fade radius |
| 18 | SF/Environment/Cove Glow | Unlit HDR | Lobby cove strips | Intensity, Breathing |
| 19 | SF/FX/View Vignette | Alpha, head-locked | Pause dimmer, deviation edge flash | Uniform / Edge opacity |
| 20 | SF/FX/Hologram | Additive, glitch | Replay ghost head | Scanlines, Glitch rate |

Utility (not counted): `Hidden/SF/PackMetallicSmoothness` packs separate metalness + roughness maps into URP's MetallicSmoothness layout for the imported models.

## Driving shader values at runtime
- `ShaderPropertyAnimator` animates any float (Once / Loop / PingPong) via a MaterialPropertyBlock — e.g. blood `_Spread`, `_Dissolve`, glove `_Contamination`.
- `DeviationVignette` flashes shader 19's edge on every Deviation event (Guided mode).
- `LaparoscopeFeed` feeds the scope camera into shader 15 (`_BaseMap`) and fades it in via `_EmissionColor`.

## Performance notes (Quest 3)
- Lit shaders compute detail procedurally (a few noise octaves); they are meant for small/close surfaces. Don't put 01/03/05 on full-screen geometry.
- Transparent effects are small except **14 Laminar Flow**, which is disabled on Android by `DisableOnMobileXR`.
- Custom lit shaders sample light probes (not lightmaps) — they're for dynamic objects. Static rooms stay on URP Lit so they keep baked lightmaps. `FallBack "Universal Render Pipeline/Lit"` supplies the Meta pass, so they still contribute albedo to bakes.
