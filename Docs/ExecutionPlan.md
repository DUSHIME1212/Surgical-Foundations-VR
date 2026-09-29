# Surgical Foundations VR — Execution Plan

Source: *Production Asset List* (Sep 28, 2026, @DUSHIME). Scope: 9 scenes, 41 models (38 MVP, 3 R2), ~70 animations, .NET backend.
Current project state (Sep 29, 2026): Unity 6000.6.3f1, URP 17.6, XRI 3.6, XR Hands 1.9, OpenXR + Meta OpenXR. Only the VR template `SampleScene` exists; no project code yet. Version control is Unity Version Control (Plastic).

**Assumptions** (adjust once team size is known): 2-week sprints; tracks run in parallel — **ENG** (Unity engineering), **ART** (modelling/materials), **ANIM** (animation/VFX), **UI**, **BE** (backend + dashboard). Nine sprints to MVP, two to R2, one to R3.

---

## Progress (Sep 29, 2026)
Pulled forward from Sprints 1–3 as a grey-box foundation (see `Docs/ProjectStructure.md`):
- ✅ `Assets/_Project` structure, runtime + editor assemblies, import rules
- ✅ All 41 asset-list models as real-scale primitive placeholders with gameplay pivots (swap-ready)
- ✅ 9 scenes: persistent bootstrap + additive stages, fade transitions, spawn points, build settings
- ✅ Lighting pipeline: baked rooms, mixed surgical heads, probes, reflection probes, post profiles
- ✅ All 16 prototype screens + HUD, toasts, subtitles, pause, monitor overlays; click-through flow Lobby → Prep → Access → Operate → Close → Summary
- ✅ 63 original placeholder sounds + SoundBank + AudioManager (ducking, subtitles, haptic UI ticks)
- ⏳ Not yet: detection/scoring logic, pose recorder, sync queue, backend, Obi tissue, real art/VO

## 0. Decisions and fixes before Sprint 1

| # | Item | Owner | Why it blocks |
|---|---|---|---|
| D1 | Update FR-14 in System Requirements: tearing + bleeding become "should" (R2) | PO | Asset list already treats them as R2; docs disagree |
| D2 | Book SME sessions to sign off the scrub sequence, gowning steps and vessel-injury steps | PO | Blocks all ghost-hand demos and assistant gowning clips (Sprint 5) |
| D3 | Buy Obi Softbody; confirm it supports Unity 6000.6 + URP 17 + Android/Quest | ENG lead | Dissection tissue spike in Sprint 1 |
| D4 | Shared C# contracts library must target **netstandard2.1** (Unity can't load a net10.0 assembly); the API on .NET 10 references the same project | BE + ENG | NFR-07 "event format can't drift" depends on it |
| D5 | Map prototype screens 13 and 16 — not listed in the scene table (presumably Pause and Summary, which are UI prefabs) | UX | Keeps the screen → scene trace complete |
| D6 | Get motion reference for the scrub nurse gowning assist (~12 keyframed clips) | ANIM | Mocap/video shoot needs lead time |
| D7 | Decide on a laparoscope RenderTexture resolution and refresh rate; the cavity (40k tris) renders through a second camera | ENG | Second camera roughly doubles cost in Operate; affects the 400k / 72 fps budget |

---

## 1. Milestones

| Milestone | End of | Exit criteria |
|---|---|---|
| **M0 Foundations** | Sprint 1 | Bootstrap + additive loading works on Quest 3; Obi tissue profiled; API skeleton deployed; contracts library shared |
| **M1 Vertical slice** | Sprint 3 | Lobby → SkillsLab → OR_Base → Operate with greybox; a grasper moves on the fulcrum; peg-transfer task scores on-device; events sync to API |
| **M2 All stages playable (greybox)** | Sprint 5 | Prep, Access, Operate, Close playable end to end with scoring, summary and replay upload |
| **M3 Content complete** | Sprint 7 | All 38 MVP models final, all MVP animations in, replay viewer working in WebGL |
| **M4 MVP release** | Sprint 9 | 72 fps on Quest 3 in every scene, load < 15 s, offline sync proven, xAPI/SCORM integration tested with a real LRS/LMS |
| **M5 R2** | Sprint 11 | Branches, tissue tear + bleed, camera assistant, anaesthetist, users/cohorts, cohort analytics, replay camera views |
| **M6 R3** | Sprint 12 | Haptics, live spectating |

**Critical path:** Bootstrap/scene loading → instrument rig (fulcrum) → Operate stage → scoring/event log → replay recording → replay viewer. Soft-body tissue and the scrub-nurse character are the two highest content risks.

---

## 2. Sprint plan

### Sprint 1 — Foundations and spikes (M0)
**ENG**
- Create folder structure (`Assets/_Project/{Scenes,Scripts,Prefabs,Art,Animation,UI,Settings}`) and assembly definitions (`Core`, `Gameplay`, `UI`, `Contracts`).
- Create all 9 scenes as empties; add to Build Settings; remove `SampleScene` from the build.
- `00_Bootstrap`: XR Origin (from template), `SessionManager`, `SceneLoader` (single + additive, fade-to-black transition, NFR-05), stub `ScenarioStateMachine`, `EventLogger` skeleton.
- **Spike:** Obi Softbody dissection tissue on Quest 3 — 10k-tri mesh + collision proxy, measure frame time (NFR-01).
- **Spike:** laparoscope camera → RenderTexture on monitor mesh, measure cost at 2–3 resolutions (D7).
- Quest 3 build pipeline and a perf-capture routine (OVR Metrics / Unity Profiler over ADB).

**BE**
- Solution: `Api` (ASP.NET Core, .NET 10), `Contracts` (netstandard2.1), `Worker` (BackgroundService), PostgreSQL + EF Core migrations, S3-compatible storage (MinIO locally).
- Contracts v0: `Session`, `ProtocolEvent` (client UUID), `ScoringConfig`, `ReplayManifest`.
- Docker compose for local dev; CI build.

**ART**
- Style guide, texel density, material library (URP Lit / Simple Lit), naming and pivot conventions (tip pivot + port-contact pivot for every instrument, separate jaw meshes).
- Greybox: OR room, OR table, tower + monitor, patient body, grasper.

**UI**
- UI kit in world-space uGUI/UI Toolkit: panel, button, toast, gauge, checklist row; panel open/close tween (150–250 ms), laser pointer + haptic tick.

### Sprint 2 — Core systems
**ENG**
- `EventLogger`: ≥ 30 Hz pose recorder (head, hands, instrument tips), protocol events, local persistence.
- `SyncQueue`: offline-first, batched upload, retries, dedupe by event UUID (FR-25, NFR-09).
- Instrument rig: fulcrum pivot at port (FR-10), shaft rotation on thumbstick, jaw open/close from trigger 0–1, ratchet click.
- Hand grip poses ×6 (XR Hands skeleton) — placeholder poses from the XRI hands sample.
- `01_Lobby`: sign-in panel (device code / QR), settings (mode, posture, input), attempts history stub.

**BE**
- Auth: `POST /auth/device-code`, `POST /auth/token` (FR-01). Sessions: `POST /sessions`, `/complete` (FR-04, FR-18). Events: batched `POST /sessions/:id/events` (FR-25).
- `GET /config/scoring/:version` with seeded config v1 (FR-18).

**ART**
- Final: grasper, Maryland dissector, scissors, trocar 12 mm + 5 mm, laparoscope 30°. Greybox: abdominal cavity, peg board + rings.

### Sprint 3 — Vertical slice (M1)
**ENG**
- `02_SkillsLab`: calibration (table height, seated-mode reposition, US-ACS-01), tutorial on peg board, controller-diagram panel.
- `10_OR_Base` + `13_Stage_Operate`: laparoscope camera → monitor, 30° rotation with cable follow, monitor power-on fade, drift detection + edge arrow (FR-13).
- On-device scoring engine reading cached `ScoringConfig` (FR-17, FR-18); peg-transfer task.
- Pause (world dims, timer freezes, US-ACS-03), subtitles (US-ACS-04), localization setup.

**BE**
- Replays: `POST /replays` (pre-signed URL), `GET /replays/:id` (FR-21, NFR-16).
- Dashboard: React app with TypeScript client generated by NSwag from OpenAPI.

**ART / ANIM**
- Final: OR room, OR table, ceiling lights, tower, peg board + rings, transfer objects, controller diagram, skills lab room, lobby room.
- Surgeon hands (L/R) rigged to XR Hands skeleton with bare / gloved / contaminated materials.

**Gate:** first profiling pass on Quest 3 — confirm triangle budgets from the asset list, adjust.

### Sprint 4 — Prep and Access stages
**ENG**
- `11_Stage_Prep`: scrub sink with sensor tap, 6-step scrub tracking, gown/glove station, back table sterile vs. non-sterile colliders (FR-07), contamination detection + pulse + vignette, instrument tray XR sockets for tray check.
- `12_Stage_Access`: port-site targets (Guided mode decals), scalpel incision, trocar entry through 4 wall layers with depth + resistance (FR-08), entry gauges with threshold flash, obturator withdrawal + valve flap.
- Guided mode cues: highlight rings, target-zone pulse, event toasts by class (FR-16, FR-19).

**ART**
- Final: scrub alcove, sink, back table, Mayo stand, instrument tray, gown, gloves + packet, drape set, abdominal wall layers, scalpel, port-site markers.

**ANIM**
- Cloth setups: gown, drape, swab. Glove snap blend shape. Tap water + soap lather VFX.
- Scrub nurse: rig, idle loop, retarget pipeline for Mecanim humanoid.

### Sprint 5 — Close stage, summary, replay (M2)
**ENG**
- `13_Stage_Operate`: dissection task with Obi tissue (deform only, FR-14 MVP), clip-and-cut task with clip applier fire.
- `14_Stage_Close`: seeded hideable swab spawns (US-CLS-05), count checklist (FR-15), kick bucket drop target, port removal, needle holder + spline suture.
- Summary panel: score count-up, stage bars (FR-20). Replay file writer + upload through `SyncQueue`.

**BE**
- xAPI worker with retry/backoff to LRS (FR-24). SCORM bridge `GET /scorm/launch`, `POST /scorm/result`. SSO callback (OIDC first, SAML after).

**ART**
- Final: abdominal cavity (40k), dissection tissue + proxy, clip-and-cut structure, clip, clip applier, needle holder + suture, swab (cloth + static), kick bucket, anaesthesia machine, patient body.

**ANIM** — *needs D2 sign-off*
- Ghost-hand demos: scrub ×6, closed-glove, trocar insertion, port removal.
- Scrub nurse gowning clips: open pack, hold gown, tie gown, present gloves, alert gesture.

### Sprint 6 — Replay viewer and polish
**ENG**
- `90_ReplayViewer` WebGL build: stripped theatre, pose playback interpolated from ≥ 30 Hz samples, timeline seek, click event to jump (FR-22), learner / laparoscope / free cameras (camera views R2 — ship learner cam in MVP).
- Embed the WebGL viewer in the React dashboard.
- Stage rail + checklist ticks, count mismatch → match animation, all remaining UI tweens.

**ANIM**
- Scrub nurse: pass/receive instrument (2), count point + nod. Final hand grip poses ×6.

**ART** — remaining MVP models, LODs, lightmap bake for OR_Base.

### Sprint 7 — Content complete (M3)
- All 38 MVP models and all MVP animations integrated.
- Full playthrough in Guided and unguided modes; seeded runs reproducible (FR-04).
- Occlusion culling, texture compression (ASTC), GPU instancing for clips, static batching.

### Sprint 8 — Performance and integration hardening
- Hit 72 fps in every scene on Quest 3 (NFR-01), stage load < 15 s (NFR-03).
- Offline test: complete a full session in airplane mode, reconnect, verify no duplicate events (FR-25, NFR-09).
- xAPI against a real LRS; SCORM package in a real LMS; SSO against a test IdP.
- Deploy: Linux containers in cloud; document the Windows Server on-prem path.

### Sprint 9 — MVP release (M4)
- Bug bash, SME clinical review of the full procedure, accessibility pass (seated mode, subtitles).
- Quest build packaging (Meta Horizon store or enterprise sideload), release notes, runbooks.

### Sprints 10–11 — R2 (M5)
- Vessel-injury branch: epigastric vessels model, bleed + pooling VFX (US-ACC-05).
- Tissue tearing above force threshold with bleed (US-OP-06).
- Camera assistant: hands model, laparoscope hold idle, 4 blended pan/zoom clips, voice/button commands (US-OP-03).
- Anaesthetist character with idle + monitor-check clips.
- Backend: users, cohorts, assignments with roles (FR-02); cohort stats endpoint (US-INS-07). Replay viewer extra camera views.

### Sprint 12 — R3 (M6)
- Haptics beyond UI ticks (instrument contact, tissue resistance).
- Live spectating of an in-progress session from the dashboard.

---

## 3. Asset tracker (by production sprint)

**Models (41)** — ✓ = MVP, R2 = release 2

| Sprint | Models |
|---|---|
| 2 | Grasper, Maryland dissector, scissors, trocar 12 mm, trocar 5 mm, laparoscope 30° |
| 3 | OR room, OR table, ceiling lights, lap tower + monitor, peg board + rings, transfer objects, controller diagram, skills lab room, lobby room, surgeon hands |
| 4 | Scrub alcove, scrub sink, back table, Mayo stand, instrument tray, gown, gloves + packet, drape set, abdominal wall layers, scalpel, port-site markers, scrub nurse (rig) |
| 5 | Abdominal cavity, dissection tissue, clip-and-cut structure, surgical clip, clip applier, needle holder + suture, swab, kick bucket, anaesthesia machine, patient body |
| 10–11 | Epigastric vessels (R2), anaesthetist (R2), camera assistant hands (R2) |

Worst-case in-view triangles (OR + Prep/Operate + characters) sum to about 260k LOD0 against the 400k budget — but the cavity is drawn again by the laparoscope camera, so profile Operate specifically.

**Animations (~70)**

| Type | Count (approx.) | Sprints |
|---|---|---|
| Keyframed — scrub nurse (MVP) | 9 clips | 4–6 |
| Keyframed — ghost-hand demos + grip poses (MVP) | 15 | 5–6 |
| Keyframed — R2 characters | 7 | 10–11 |
| Procedural instruments/environment | ~15 | 2–6 |
| Simulation / VFX | ~13 (bleeding R2) | 4–5, 10–11 |
| UI tweens | ~14 | 1–6 |

---

## 4. Risks

| Risk | Impact | Mitigation |
|---|---|---|
| Obi Softbody too expensive on Quest 3 | Dissection task fails 72 fps | Sprint 1 spike; fallback to vertex-shader deformation on a proxy |
| Second camera (laparoscope) cost | Operate stage misses budget | Lower RT resolution, render at half rate, aggressive culling on cavity layer |
| SME sign-off late | Ghost-hand demos + gowning clips slip | Book D2 now; build demos last; use placeholder text cues until then |
| 41 models built in-house | ART is the bottleneck | Greybox first, final art gated per sprint; environment + equipment are the largest share, start them earliest |
| Contracts drift between Unity and API | Sync breaks silently | netstandard2.1 shared library (D4) + contract tests in CI |
| Scene reloads drop XR rig or sync queue | Lost data mid-session | Everything persistent lives in `00_Bootstrap`; test stage swaps in every sprint |

---

## 5. Definition of done (per item)
- Runs on Quest 3 at 72 fps in its scene; meets its triangle budget.
- Traced to its FR/US ID in the tracker.
- Emits the correct protocol events and survives offline → online sync.
- No animation moves the player camera (NFR-05).
