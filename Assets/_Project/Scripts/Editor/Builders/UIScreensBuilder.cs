using System.IO;
using SurgicalFoundations.Audio;
using SurgicalFoundations.Scenario;
using SurgicalFoundations.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static SurgicalFoundations.EditorTools.UIKit;

namespace SurgicalFoundations.EditorTools
{
    /// <summary>
    /// Builds the 16 headset screens of the UI flow prototype (Design.pdf) as world-space prefabs, plus the HUD,
    /// subtitle bar and laparoscope monitor overlays. Screen numbers match the prototype.
    /// </summary>
    public static class UIScreensBuilder
    {
        [MenuItem(SFPaths.MenuRoot + "Build/3 · UI Screens", priority = 3)]
        public static void BuildAll()
        {
            Init(EnsureTheme());
            Directory.CreateDirectory(SFPaths.UIScreens);
            Directory.CreateDirectory(SFPaths.UIHud);

            S01_SignIn(); S02_Lobby(); S03_Calibrate(); S04_Tutorial();
            S05_Scrub(); S06_Sterility(); S07_TrayAndDrape();
            S08_PortSites(); S09_TrocarEntry(); S10_BleedBranch();
            S11_Operate(); S12_Drift(); S13_Pause(); S14_Count(); S15_PortRemoval(); S16_Summary();
            HUD(); Subtitles(); ReplayControls();
            MonitorOverlay("UI_Monitor_Access", "LAPAROSCOPE  ·  30°", "TIP IN VIEW", null, false);
            MonitorOverlay("UI_Monitor_Operate", "LAPAROSCOPE  ·  30°  ·  ×1.4", null, "Transfer ring 3 to the right peg — mid-air handover", false);
            MonitorOverlay("UI_Monitor_Drift", "LAPAROSCOPE  ·  30°  ·  ×1.4", null, null, true);
            MonitorOverlay("UI_Monitor_Close", "LAPAROSCOPE  ·  30°", "PORT SITE IN VIEW", null, false);
            AssetDatabase.SaveAssets();
            Debug.Log("[Surgical Foundations] UI screens built in " + SFPaths.UIPrefabs);
        }

        public static UITheme EnsureTheme()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SFPaths.UITheme));
            var theme = AssetDatabase.LoadAssetAtPath<UITheme>(SFPaths.UITheme);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<UITheme>();
                AssetDatabase.CreateAsset(theme, SFPaths.UITheme);
            }
            if (theme.font == null) theme.font = TMP_Settings.defaultFontAsset;
            EditorUtility.SetDirty(theme);
            return theme;
        }

        // ───────────── helpers ─────────────

        static RectTransform Screen(string name, float width, V v, int pad, float spacing, out RectTransform canvas)
        {
            canvas = Canvas(name, width + 80, 1200);
            canvas.gameObject.AddComponent<UIPanel>();
            return RootPanel(canvas, v, width, pad, spacing);
        }

        static GameObject Save(GameObject root, string folder = null)
        {
            folder ??= SFPaths.UIScreens;
            var path = $"{folder}/{root.name}.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static GameObject NewRoot(string name) => new GameObject(name);

        static void Place(Transform t, Transform parent, Vector3 pos, float yaw = 0f)
        {
            t.SetParent(parent, false);
            t.localPosition = pos;
            t.localRotation = Quaternion.Euler(0, yaw, 0);
        }

        static void Advance(Button b, int goTo = -1, StepAdvanceButton.LogKind log = StepAdvanceButton.LogKind.None, string message = null)
        {
            var s = b.gameObject.AddComponent<StepAdvanceButton>();
            Ser.Set(s, "goToIndex", goTo);
            Ser.Set(s, "log", (int)log);
            if (message != null) Ser.Set(s, "message", message);
        }

        static void LogOnEnable(GameObject go, EventClass cls, string code, string message, SoundId cue = SoundId.None)
        {
            var l = go.AddComponent<LogEventOnEnable>();
            Ser.Set(l, "eventClass", (int)cls);
            Ser.Set(l, "code", code);
            Ser.Set(l, "message", message);
            Ser.Set(l, "cue", (int)cue);
        }

        static RectTransform HRow(Transform parent, float spacing = 16) => HStack(parent, spacing);

        static TextMeshProUGUI Flex(TextMeshProUGUI t) { LE(t, -1, -1, 1); return t; }

        // ───────────── 01 Sign in ─────────────

        static void S01_SignIn()
        {
            var p = Screen("UI_01_SignIn", 1100, V.Panel, 56, 30, out var canvas);
            var head = HRow(p, 26);
            Icon(head, Logo, 88, T.accent, "Logo");
            var titles = VStack(head, 6, 0, "Titles");
            LE(titles, -1, -1, 1);
            Text(titles, "Surgical Foundations", TS.Title);
            Text(titles, "Laparoscopic skills · single learner · about 15 minutes", TS.Secondary);

            var cards = HRow(p, 26);
            LE(cards, -1, 340);
            var qrCard = Surface(cards, V.Card, 30, 18, -1, "QRCard");
            qrCard.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.UpperCenter;
            qrCard.GetComponent<VerticalLayoutGroup>().childControlWidth = true;
            LE(qrCard, -1, 340, 1);
            var qrHolder = HStack(qrCard, 0, "QRHolder", TextAnchor.MiddleCenter);
            var qrBg = Rect("QRBackground", qrHolder);
            LE(qrBg, 200, 200, 0);
            Fill(qrBg, UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(SFPaths.Sprites + "/card_r14.png"), new Color(0.91f, 0.94f, 0.93f), 0.75f);
            var qr = Icon(qrBg, QR, 164, T.backdrop, "QR");
            qr.GetComponent<LayoutElement>().ignoreLayout = true;
            var qrt = (RectTransform)qr.transform; qrt.anchorMin = qrt.anchorMax = new Vector2(0.5f, 0.5f); qrt.sizeDelta = new Vector2(164, 164);
            Text(qrCard, "Scan with your phone to sign in with your institution account", TS.Secondary, T.textPrimary, TextAlignmentOptions.Center);

            var codeCard = Surface(cards, V.Card, 34, 16, -1, "CodeCard");
            LE(codeCard, -1, 340, 1);
            Spacer(codeCard, 8);
            Text(codeCard, "Or enter this code at", TS.Eyebrow);
            Text(codeCard, "[yourinstitution]/link", TS.Mono, T.accent);
            Text(codeCard, "4F7-K2Q", TS.MonoLarge);
            Text(codeCard, "Code refreshes every 5 minutes", TS.Secondary);

            var buttons = HRow(p, 22);
            Advance(Button(buttons, "Continue from LMS launch", BV.Primary, 92, -1, 1.7f));
            Advance(Button(buttons, "Enter session code", BV.Secondary, 92, -1, 1f));
            StatusLine(p, "Offline — you can still train. Results will queue on this headset and sync when you reconnect.", T.warning);
            Save(canvas.gameObject);
        }

        // ───────────── 02 Lobby (three panels) ─────────────

        static void S02_Lobby()
        {
            var root = NewRoot("UI_02_Lobby");
            var actions = root.AddComponent<UIActions>();

            // Centre
            var c = Screen("Panel_Main", 1180, V.Panel, 52, 18, out var cc);
            Place(cc, root.transform, Vector3.zero);
            Text(c, "Welcome back, [Learner name]", TS.Secondary);
            Text(c, "Laparoscopic basics", TS.Title);
            Text(c, "Assigned by [Instructor] · Difficulty [Level] · Prep → Access → Operate → Close", TS.Secondary);
            Spacer(c, 6);
            Text(c, "Mode", TS.Eyebrow);
            var modes = HRow(c, 22); LE(modes, -1, 132);
            var mg = Group(modes);
            Segment(modes, mg, "Guided", "Highlights, voice prompts, live flags", true, 132, 1, actions.SetGuided);
            Segment(modes, mg, "Assessment", "No cues · result sent to LMS", false, 132, 1, actions.SetAssessment);
            Text(c, "Posture", TS.Eyebrow);
            var post = HRow(c, 22); LE(post, -1, 76);
            var pg = Group(post);
            Segment(post, pg, "Seated", null, false, 76, 1, actions.SetSeated);
            Segment(post, pg, "Standing", null, true, 76, 1, actions.SetStanding);
            Spacer(c, 6);
            var go = HRow(c, 22);
            Button(go, "Tutorial", BV.Secondary, 92, 250, 0, actions.GoToSkillsLab);
            Button(go, "Start scenario", BV.Primary, 92, -1, 1, actions.StartScenario);

            // Left — recent attempts
            var l = Screen("Panel_RecentAttempts", 680, V.Panel, 44, 10, out var lc);
            Place(lc, root.transform, new Vector3(-1.08f, 0.08f, -0.32f), -32f);
            Text(l, "Recent attempts", TS.Eyebrow);
            Spacer(l, 6);
            string[] modesTxt = { "Guided", "Guided", "Assessment" };
            for (int i = 0; i < 3; i++)
            {
                KV(l, $"[Date] · {modesTxt[i]}", "[score]", T.accent, true, 56);
                Divider(l);
            }
            Text(l, "Last 10 attempts with date, score and duration", TS.Small);

            // Right — settings
            var r = Screen("Panel_Settings", 680, V.Panel, 44, 14, out var rc);
            Place(rc, root.transform, new Vector3(1.08f, 0.04f, -0.32f), 32f);
            Text(r, "Settings", TS.Eyebrow);
            Text(r, "Input", TS.Body);
            var inp = HRow(r, 16); LE(inp, -1, 78);
            var ig = Group(inp);
            Segment(inp, ig, "Controllers", null, true, 78, 1, actions.SetControllers);
            Segment(inp, ig, "Hands", null, false, 78, 1, actions.SetHands);
            Text(r, "Assessment defaults to controllers for precise scoring", TS.Small);
            var sub = HRow(r, 16); LE(sub, -1, 64);
            Flex(Text(sub, "Subtitles", TS.Body));
            Segment(sub, null, "On", null, true, 56, 0, actions.SetSubtitles, true);
            LE(sub.GetChild(sub.childCount - 1), 110, 56, 0);
            Text(r, "Text size", TS.Body);
            var ts = HRow(r, 14); LE(ts, -1, 76);
            var tg = Group(ts);
            Segment(ts, tg, "A", null, false, 76, 0, actions.SetTextSmall, false, 22); LE(ts.GetChild(0), 86, 76, 0);
            Segment(ts, tg, "A", null, true, 76, 0, actions.SetTextMedium, false, 30); LE(ts.GetChild(1), 86, 76, 0);
            Segment(ts, tg, "A", null, false, 76, 0, actions.SetTextLarge, false, 38); LE(ts.GetChild(2), 86, 76, 0);
            Save(root);
        }

        // ───────────── 03 Calibrate ─────────────

        static void S03_Calibrate()
        {
            var root = NewRoot("UI_03_Calibrate");
            var actions = root.AddComponent<UIActions>();
            var p = Screen("Panel", 980, V.Panel, 52, 22, out var canvas);
            Place(canvas, root.transform, Vector3.zero);
            Text(p, "Set up your theatre", TS.Eyebrow);
            Text(p, "Calibrate", TS.Title);

            var s1 = Numbered(p, 1, "Height", true);
            Text(s1, "Stand or sit naturally and look ahead — measured <mspace=0.62em>[cm]</mspace>", TS.Secondary);

            var s2 = Numbered(p, 2, "Dominant hand", true);
            var hands = HRow(s2, 16); LE(hands, -1, 76);
            var hg = Group(hands);
            Segment(hands, hg, "Left", null, false, 76);
            Segment(hands, hg, "Right", null, true, 76);

            var s3 = Numbered(p, 3, "Table height", false);
            Text(s3, "Grab the table edge and move it until your elbows sit at about 90°", TS.Secondary);
            var labels = HRow(s3, 8);
            Text(labels, "Lower", TS.Secondary);
            Flex(Text(labels, "92 cm", TS.Mono, T.textPrimary, TextAlignmentOptions.Center));
            Text(labels, "Higher", TS.Secondary, null, TextAlignmentOptions.Right);
            var fill = Bar(s3, 0.47f, T.accent, -1, null, null, 12);
            var knob = Rect("Knob", fill.parent);
            knob.anchorMin = knob.anchorMax = new Vector2(0.47f, 0.5f);
            knob.sizeDelta = new Vector2(34, 34);
            var ki = knob.gameObject.AddComponent<Image>();
            ki.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SFPaths.Sprites + "/circle.png"); ki.color = T.accent; ki.raycastTarget = false;

            Text(p, "Saved to your profile — you won’t be asked again unless you recalibrate.", TS.Secondary);
            var b = HRow(p, 20);
            Button(b, "Back", BV.Secondary, 92, 200, 0, actions.GoToLobby);
            Advance(Button(b, "Confirm", BV.Primary, 92, -1, 1), -1, StepAdvanceButton.LogKind.OnProtocol, "Calibration saved");
            Save(root);
        }

        // ───────────── 04 Tutorial ─────────────

        static void S04_Tutorial()
        {
            var root = NewRoot("UI_04_Tutorial");
            var actions = root.AddComponent<UIActions>();
            var p = Screen("Panel", 1080, V.Panel, 52, 24, out var canvas);
            Place(canvas, root.transform, Vector3.zero);
            var chipRow = HRow(p);
            Chip(chipRow, "Tutorial · unscored · step 2 of 5", T.accent, T.card, 44, true);
            Text(p, "Pick up the highlighted peg", TS.Title);

            var legend = VStack(p, 14, 0, "Legend");
            (Color c, string t)[] keys =
            {
                (T.warning, "<b>Trigger</b> — close instrument jaws"),
                (T.accent, "<b>Grip</b> — hold the instrument"),
                (T.textSecondary, "<b>Thumbstick</b> — rotate tip / zoom camera"),
                (T.textSecondary, "<b>Menu</b> — pause at any time"),
            };
            for (int i = 0; i < keys.Length; i++)
            {
                var r = HRow(legend, 18);
                var sw = Rect("Swatch", r);
                LE(sw, 22, 22, 0);
                Fill(sw, AssetDatabase.LoadAssetAtPath<Sprite>(SFPaths.Sprites + (i < 2 ? "/card_r14.png" : "/card_r14_outline.png")), keys[i].c, 3f);
                Flex(Text(r, keys[i].t, TS.Body));
            }

            var info = Surface(p, V.Card, 30, 8, -1, "FulcrumNote");
            Text(info, "Instruments pivot at the port: move your hand <b>left</b> and the tip moves <b>right</b>. That’s the fulcrum effect.", TS.Body);

            var prog = HRow(p, 10); LE(prog, -1, 8);
            for (int i = 0; i < 5; i++) { var bar = Bar(prog, i < 2 ? 1f : 0f, T.accent, -1, null, null, 8); }
            var b = HRow(p, 20);
            Button(b, "Skip tutorial", BV.Secondary, 92, 270, 0, actions.StartScenario);
            Button(b, "Next step", BV.Primary, 92, -1, 1, actions.StartScenario);
            Text(p, "Skip appears after your first full tutorial · about 3 minutes", TS.Small);
            Save(root);
        }

        // ───────────── Prep: 05 / 06 / 07 ─────────────

        static void S05_Scrub()
        {
            var p = Screen("UI_05_Scrub", 800, V.Panel, 48, 12, out var canvas);
            Text(p, "Step 1 of 4 · Scrub", TS.Eyebrow);
            Text(p, "Surgical scrub", TS.Heading);
            Spacer(p, 4);
            CheckRow(p, "Remove jewellery, check nails", Row.Done);
            CheckRow(p, "Pre-wash hands and forearms", Row.Done);
            CheckRow(p, "Clean under nails", Row.Done);
            CheckRow(p, "Scrub fingers, hands, forearms", Row.Active, "0:42");
            CheckRow(p, "Rinse, fingertips up", Row.Pending);
            CheckRow(p, "Dry with sterile towel", Row.Pending);
            Text(p, "Steps are detected from hand motion. Skipped or out-of-order steps are logged as deviations. [Sequence to be signed off by SME]", TS.Small);
            Advance(Button(p, "Continue to gown & glove", BV.Secondary, 88), -1, StepAdvanceButton.LogKind.OnProtocol, "Surgical scrub complete");
            Save(canvas.gameObject);
        }

        static void S06_Sterility()
        {
            var root = NewRoot("UI_06_Sterility");
            LogOnEnable(root, EventClass.Deviation, "prep.contamination", "Glove touched non-sterile edge", SoundId.FB_Contamination);

            var p = Screen("Panel_Deviation", 1000, V.Danger, 52, 22, out var dc);
            Place(dc, root.transform, new Vector3(-0.3f, 0, 0));
            var head = HRow(p, 20);
            Icon(head, Warning, 52, T.danger);
            Chip(head, "Deviation · logged 02:29", Color.Lerp(T.danger, Color.black, 0.88f), T.danger, 40);
            Text(p, "Sterility broken", TS.Title);
            Text(p, "Your left glove touched the non-sterile edge of the back table.", TS.Body, T.textSecondary);
            var rec = Surface(p, V.CardDanger, 30, 14, -1, "Recover");
            Text(rec, "Recover", TS.Eyebrow, T.textPrimary);
            string[] steps = { "Step back from the sterile field", "Remove the contaminated glove", "Re-glove with assistance" };
            for (int i = 0; i < steps.Length; i++)
            {
                var r = HRow(rec, 18);
                Text(r, (i + 1).ToString(), TS.Body, T.danger);
                Flex(Text(r, steps[i], TS.Body));
            }
            Text(p, "Your score records both the break and the recovery. In Assessment mode this card is hidden but the event is still logged.", TS.Secondary, T.textPrimary);
            Advance(Button(p, "Start re-glove", BV.Danger, 92), -1, StepAdvanceButton.LogKind.OnProtocol, "Re-gloved · sterility restored");

            var s = Screen("Panel_SterilityStatus", 620, V.Panel, 42, 6, out var sc);
            Place(sc, root.transform, new Vector3(0.62f, 0.12f, -0.06f), 18f);
            Text(s, "Sterility", TS.Eyebrow);
            Spacer(s, 6);
            KV(s, "Right glove", "Sterile", T.accent);
            KV(s, "Left glove", "<b>Contaminated</b>", T.danger);
            KV(s, "Gown", "Sterile", T.accent);
            KV(s, "Instruments", "Sterile", T.accent);
            Save(root);
        }

        static void S07_TrayAndDrape()
        {
            var root = NewRoot("UI_07_TrayAndDrape");
            var p = Screen("Panel_TrayCount", 880, V.Panel, 48, 4, out var tc);
            Place(tc, root.transform, new Vector3(-0.28f, 0, 0));
            Text(p, "Step 3 of 4 · Opening count", TS.Eyebrow);
            var title = HRow(p);
            Flex(Text(title, "Check the instrument tray", TS.Heading));
            Text(title, "7 / 8", TS.Mono, T.textSecondary);
            Spacer(p, 8);
            (string n, string q)[] items = { ("Trocar, 12 mm", "×1"), ("Trocar, 5 mm", "×2"), ("Laparoscope, 30°", "×1"), ("Grasper", "×2"), ("Scissors", "×1") };
            foreach (var (n, q) in items) { CheckRow(p, n, Row.Muted, q, 60); Divider(p); }
            CheckRow(p, "Clip applier — not confirmed (tap to confirm)", Row.Warning, "×1", 64);
            CheckRow(p, "Swabs", Row.Muted, "×5", 60); Divider(p);
            CheckRow(p, "Drape set", Row.Muted, "×1", 60);
            Spacer(p, 10);
            Text(p, "Point at an item on the tray and pull the trigger to confirm it. This becomes the opening count for Close.", TS.Secondary);

            var d = Screen("Panel_Drape", 600, V.Panel, 44, 20, out var dc);
            Place(dc, root.transform, new Vector3(0.55f, 0.06f, -0.05f), 15f);
            Text(d, "Step 4 of 4", TS.Eyebrow);
            Text(d, "Drape and confirm the sterile field", TS.Heading);
            var locked = Surface(d, V.Card, 22, 0, -1, "DrapeLocked");
            Text(locked, "Drape locked — 1 item unchecked", TS.BodyStrong, T.textSecondary, TextAlignmentOptions.Center);
            Text(d, "Emits “Sterile field confirmed” and ends Prep.", TS.Secondary);
            Advance(Button(d, "Confirm sterile field", BV.Primary, 88), -1, StepAdvanceButton.LogKind.OnProtocol, "Sterile field confirmed");
            Save(root);
        }

        // ───────────── Access: 08 / 09 / 10 ─────────────

        static void S08_PortSites()
        {
            var p = Screen("UI_08_PortSites", 820, V.Panel, 48, 14, out var canvas);
            Text(p, "Access · Step 1 of 2", TS.Eyebrow);
            Text(p, "Mark your port sites", TS.Heading);
            (string n, string status, Color c, string sub)[] ports =
            {
                ("Camera · umbilical", "✓ On target", T.accent, "[n] mm from target"),
                ("Left working", "Off target", T.warning, "[n] mm from target · re-mark?"),
                ("Right working", "Pending", T.textSecondary, null),
            };
            foreach (var (n, status, c, sub) in ports)
            {
                var v = VStack(p, 4, 0, "Port");
                var r = HRow(v);
                Flex(Text(r, n, TS.Body));
                Text(r, status, TS.Body, c, TextAlignmentOptions.Right);
                if (sub != null) Text(v, sub, TS.Mono);
                Divider(p);
            }
            Text(p, "Point and pull the trigger to mark. Dashed zones show safe landmarks in Guided mode only.", TS.Secondary);
            Advance(Button(p, "Begin trocar entry", BV.Primary, 88), -1, StepAdvanceButton.LogKind.OnProtocol, "Port sites marked");
            Save(canvas.gameObject);
        }

        static void S09_TrocarEntry()
        {
            var root = NewRoot("UI_09_TrocarEntry");
            var p = Screen("Panel_Entry", 780, V.Panel, 48, 22, out var ec);
            Place(ec, root.transform, Vector3.zero);
            Text(p, "Port 2 of 3 · First pass", TS.Eyebrow);
            Text(p, "Insert the trocar", TS.Heading);
            Gauge(p, "Angle", "<mspace=0.62em>[°]</mspace>  shallow", T.warning, 0f, T.accent, 0.35f, T.warning, new Vector2(0.47f, 0.77f));
            Gauge(p, "Depth", "<mspace=0.62em>[mm]</mspace>", T.accent, 0.55f, T.accent);
            Gauge(p, "Force", "OK", T.accent, 0.4f, T.accent, 0.77f, T.danger);
            KV(p, "Camera view", "✓ Tip visible", T.accent);
            Advance(Button(p, "Port placed · continue", BV.Primary, 88), 3, StepAdvanceButton.LogKind.OnProtocol, "Port placed");
            Advance(Button(p, "Show unsafe-entry branch", BV.Secondary, 80), 2, StepAdvanceButton.LogKind.Deviation, "Trocar entered too deep — vessel injury");

            var f = Screen("Panel_TechniqueFlag", 600, V.Warning, 36, 10, out var fc);
            Place(fc, root.transform, new Vector3(1.22f, 0.18f, -0.08f), 16f);
            LogOnEnable(fc.gameObject, EventClass.Delayed, "access.angle", "Angle too shallow", SoundId.FB_TechniqueFlag);
            Text(f, "Technique flag · <200 ms", TS.Eyebrow, T.warning);
            Text(f, "Angle too shallow", TS.Subheading);
            Text(f, "Raise the trocar toward the target zone before pushing further.", TS.Secondary, T.textPrimary);
            Save(root);
        }

        static void S10_BleedBranch()
        {
            var p = Screen("UI_10_BleedBranch", 980, V.Danger, 52, 18, out var canvas);
            var chipRow = HRow(p);
            Chip(chipRow, "Branch taken · depth over threshold", Color.Lerp(T.danger, Color.black, 0.88f), T.danger, 40);
            Text(p, "Bleeding at the entry site", TS.Title);
            Text(p, "Unsafe entry caused a simulated vessel injury. Manage it to continue.", TS.Body, T.textSecondary);
            (string t, int state)[] rows = { ("Keep the bleed in camera view", 0), ("Apply pressure with the grasper", 1), ("[Next management step — SME to define]", 2) };
            foreach (var (t, state) in rows)
            {
                var r = HStack(p, 20, "Row", TextAnchor.MiddleLeft, 22, 0);
                Fill(r, AssetDatabase.LoadAssetAtPath<Sprite>(SFPaths.Sprites + "/card_r14.png"), T.dangerInset, 0.75f);
                if (state == 1) Outline(r, AssetDatabase.LoadAssetAtPath<Sprite>(SFPaths.Sprites + "/card_r14_outline.png"), T.danger, 0.75f);
                if (state == 0) Icon(r, Check, 28, T.accent);
                else Icon(r, AssetDatabase.LoadAssetAtPath<Sprite>(SFPaths.Sprites + "/circle_outline.png"), 30, state == 1 ? T.danger : T.textMuted);
                Flex(Text(r, t, TS.Body, state == 2 ? T.textSecondary : T.textPrimary));
                LE(r, -1, 70);
            }
            Advance(Button(p, "Bleeding controlled · continue", BV.Danger, 88), 3, StepAdvanceButton.LogKind.Delayed, "Bleeding controlled");
            Text(p, "Time to control and every action are logged for replay.", TS.Secondary, T.textPrimary);
            Save(canvas.gameObject);
        }

        // ───────────── Operate: 11 / 12 ─────────────

        static void S11_Operate()
        {
            var root = NewRoot("UI_11_Operate");
            var t = Screen("Panel_Tasks", 560, V.Panel, 40, 8, out var tc);
            Place(tc, root.transform, new Vector3(-1.02f, 0.1f, -0.1f), -18f);
            Text(t, "Tasks", TS.Eyebrow);
            Spacer(t, 4);
            CheckRow(t, "Grasp & transfer", Row.Muted);
            CheckRow(t, "Peg transfer", Row.Active, "2/6");
            CheckRow(t, "Dissection", Row.Pending);
            CheckRow(t, "Clip & cut", Row.Pending);

            var m = Screen("Panel_ThisTask", 560, V.Panel, 40, 6, out var mc);
            Place(mc, root.transform, new Vector3(1.02f, 0.06f, -0.1f), 18f);
            Text(m, "This task", TS.Eyebrow);
            Spacer(m, 4);
            KV(m, "Time", "[mm:ss]", T.textPrimary, true);
            KV(m, "Path length", "[m]", T.textPrimary, true);
            KV(m, "Economy of motion", "[%]", T.textPrimary, true);
            KV(m, "Drops / errors", "[n]", T.textPrimary, true);
            Text(m, "Live metrics show in Guided mode only.", TS.Small);
            Spacer(m, 6);
            Advance(Button(m, "Next: drift example", BV.Secondary, 84), -1, StepAdvanceButton.LogKind.OnProtocol, "Ring 3 transferred");

            var cam = Canvas("Bar_CameraAssistant", 1300, 110);
            cam.gameObject.AddComponent<UIPanel>();
            Place(cam, root.transform, new Vector3(0, -0.5f, -0.05f));
            var bar = HStack(cam, 16, "Chips", TextAnchor.MiddleCenter);
            Stretch(bar);
            Text(bar, "Camera assistant", TS.Eyebrow);
            Button(bar, "“Zoom in”", BV.Chip, 64);
            Button(bar, "“Pan left”", BV.Chip, 64);
            Button(bar, "“Pan right”", BV.Chip, 64);
            Button(bar, "Hold myself", BV.ChipSelected, 64);
            Save(root);
        }

        static void S12_Drift()
        {
            var root = NewRoot("UI_12_Drift");
            var p = Screen("Panel_EventLog", 580, V.Panel, 40, 10, out var pc);
            Place(pc, root.transform, new Vector3(1.02f, 0.06f, -0.1f), 18f);
            LogOnEnable(pc.gameObject, EventClass.Delayed, "operate.drift", "Drift corrected", SoundId.FB_DriftPing);
            Text(p, "Event log", TS.Eyebrow);
            (string cls, Color c, string msg, string time)[] ev =
            {
                ("Delayed", T.warning, "Drift corrected", "09:12 · Operate"),
                ("On protocol", T.accent, "Ring 2 transferred", "08:58 · Operate"),
                ("Deviation", T.danger, "Excess force on tissue", "08:31 · Operate"),
            };
            foreach (var (cls, c, msg, time) in ev)
            {
                var v = VStack(p, 6, 0, "Event");
                var cr = HRow(v);
                Chip(cr, cls, Color.Lerp(c, Color.black, 0.88f), c, 34);
                Text(v, msg, TS.Body);
                Text(v, time, TS.Mono);
                Divider(p);
            }
            Spacer(p, 6);
            Advance(Button(p, "All tasks done · go to Close", BV.Primary, 88), -1, StepAdvanceButton.LogKind.OnProtocol, "All tasks done");
            Save(root);
        }

        // ───────────── 13 Pause ─────────────

        static void S13_Pause()
        {
            var p = Screen("UI_13_Pause", 980, V.Panel, 52, 20, out var canvas);
            canvas.GetComponent<Canvas>().sortingOrder = 50; // draw over the dimmer sphere
            Text(p, "Operate · 09:12", TS.Eyebrow);
            Text(p, "Paused", TS.Title);
            Text(p, "The timer is stopped and the pause is logged. Take off the headset if you feel unwell.", TS.Body, T.textSecondary);
            Button(p, "Resume", BV.Primary, 92);
            var r1 = HRow(p, 16);
            Button(r1, "Recalibrate", BV.Secondary, 84, -1, 1);
            Button(r1, "Settings", BV.Secondary, 84, -1, 1);
            var r2 = HRow(p, 16);
            Button(r2, "Restart scenario", BV.Secondary, 84, -1, 1);
            Button(r2, "End session", BV.DangerOutline, 84, -1, 1);
            StatusLine(p, "Progress saved on the headset every 5 seconds", T.accent);
            Save(canvas.gameObject);
        }

        // ───────────── Close: 14 / 15 ─────────────

        static void S14_Count()
        {
            var p = Screen("UI_14_Count", 980, V.Panel, 52, 12, out var canvas);
            Text(p, "Close · Step 1 of 2", TS.Eyebrow);
            Text(p, "Instrument & swab count", TS.Heading);
            Spacer(p, 8);
            RectTransform TableRow(string a, string b, string c, string d, Color? cc = null, Color? dc = null, TS style = TS.Body)
            {
                var r = HRow(p, 10);
                Flex(Text(r, a, style));
                LE(Text(r, b, style, null, TextAlignmentOptions.Center), 170);
                LE(Text(r, c, style, cc, TextAlignmentOptions.Center), 170);
                LE(Text(r, d, style == TS.Eyebrow ? style : TS.BodyStrong, dc, TextAlignmentOptions.Right), 220);
                LE(r, -1, style == TS.Eyebrow ? 36 : 60);
                return r;
            }
            TableRow("Item", "Opening", "Now", "Status", null, null, TS.Eyebrow);
            Divider(p);
            TableRow("Trocars", "3", "3", "✓ Match", null, T.accent); Divider(p);
            TableRow("Instruments", "5", "5", "✓ Match", null, T.accent); Divider(p);
            TableRow("Swabs", "5", "4", "1 missing", T.warning, T.warning); Divider(p);
            Spacer(p, 10);
            var w = Surface(p, V.CardWarning, 30, 14, -1, "ClosureBlocked");
            Text(w, "Closure blocked — 1 swab unaccounted for", TS.Subheading);
            Text(w, "Search the operative field on the monitor and the floor around the table. Time spent searching is logged.", TS.Secondary, T.textPrimary);
            Advance(Button(w, "Swab located in the field", BV.Warning, 88), -1, StepAdvanceButton.LogKind.Delayed, "Swab located · search time logged");
            Save(canvas.gameObject);
        }

        static void S15_PortRemoval()
        {
            var p = Screen("UI_15_PortRemoval", 620, V.Panel, 44, 10, out var canvas);
            Text(p, "Close · Step 2 of 2", TS.Eyebrow);
            Text(p, "Remove ports under vision", TS.Heading);
            Spacer(p, 4);
            CheckRow(p, "Right working port", Row.Muted);
            CheckRow(p, "Left working port", Row.Active);
            CheckRow(p, "Camera port (last)", Row.Pending);
            Text(p, "Removing a port without camera view is logged as a deviation.", TS.Small);
            Spacer(p, 6);
            Advance(Button(p, "Closure completed", BV.Primary, 88), -1, StepAdvanceButton.LogKind.OnProtocol, "Closure completed");
            Save(canvas.gameObject);
        }

        // ───────────── 16 Summary ─────────────

        static void S16_Summary()
        {
            var root = NewRoot("UI_16_Summary");
            var actions = root.AddComponent<UIActions>();
            var canvas = Canvas("Panel", 1560, 1000);
            canvas.gameObject.AddComponent<UIPanel>();
            Place(canvas, root.transform, Vector3.zero);
            var p = RootPanel(canvas, V.Panel, 1480, 56, 0);
            var cols = HStack(p, 56, "Columns", TextAnchor.UpperLeft);
            cols.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;

            var left = VStack(cols, 16, 0, "Left");
            LE(left, -1, -1, 1);
            Text(left, "Closure completed · Guided mode · 14:20", TS.Eyebrow);
            Text(left, "Session summary", TS.Heading);
            var score = HRow(left, 26);
            var scoreText = Text(score, "82", TS.Display, null, TextAlignmentOptions.Left, "Score");
            Chip(score, "Pass", T.onAccent, T.accent, 52);
            Text(left, "Overall · pass mark [threshold]", TS.Secondary);
            Spacer(left, 18);
            Text(left, "Stage scores", TS.Eyebrow);
            (string n, float v, Color c)[] stages = { ("Prep", 0.72f, T.accent), ("Access", 0.52f, T.warning), ("Operate", 0.76f, T.accent), ("Close", 0.82f, T.accent) };
            var bars = new UIMotion.Bar[stages.Length];
            for (int i = 0; i < stages.Length; i++)
            {
                var r = HRow(left, 20); LE(r, -1, 50);
                LE(Text(r, stages[i].n, TS.Body), 140);
                var track = Rect("BarHolder", r);
                LE(track, -1, 14, 1);
                var holderLayout = track.gameObject.AddComponent<VerticalLayoutGroup>();
                holderLayout.childControlHeight = true;
                holderLayout.childControlWidth = true; // AddComponent defaults this to false → track would stay 100 px
                holderLayout.childForceExpandWidth = true;
                holderLayout.childAlignment = TextAnchor.MiddleLeft;
                var fill = Bar(track, stages[i].v, stages[i].c, 0.6f, T.textPrimary, null, 14);
                Text(r, "[n]", TS.Mono, stages[i].c == T.warning ? T.warning : T.textSecondary, TextAlignmentOptions.Right);
                bars[i] = new UIMotion.Bar { fill = fill, value = stages[i].v };
            }
            Text(left, "White tick = stage pass mark. Access is below its pass mark.", TS.Secondary);

            var right = VStack(cols, 16, 0, "Right");
            LE(right, -1, -1, 1);
            Text(right, "Top 3 to work on", TS.Eyebrow);
            (string time, string title, string meta, Color c)[] top =
            {
                ("05:24", "Trocar entered too deep — vessel injury", "Deviation · Access", T.danger),
                ("02:29", "Glove touched non-sterile edge", "Deviation · Prep · recovered", T.danger),
                ("09:12", "Left instrument drifted out of view", "Delayed · Operate", T.warning),
            };
            foreach (var (time, title, meta, c) in top)
            {
                var card = Surface(right, V.Card, 26, 0, -1, "Issue");
                var r = HRow(card, 22);
                r.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.UpperLeft;
                Text(r, time, TS.Mono);
                var v = VStack(r, 4, 0, "Text");
                LE(v, -1, -1, 1);
                Text(v, title, TS.BodyStrong);
                Text(v, meta, TS.Secondary, c);
            }
            var b = HRow(right, 18);
            Button(b, "Retry Access stage", BV.Primary, 90, -1, 1, actions.RetryAccess);
            Button(b, "Retry full scenario", BV.Secondary, 90, -1, 1, actions.RetryScenario);
            Button(right, "Back to lobby", BV.Link, 70, -1, -1, actions.BackToLobby);
            StatusLine(right, "Offline — result and replay queued, will sync to your LMS automatically", T.warning);

            var motion = canvas.gameObject.AddComponent<UIMotion>();
            Ser.Set(motion, "scoreLabel", scoreText);
            Ser.Set(motion, "score", 82);
            var so = new SerializedObject(motion);
            var arr = so.FindProperty("bars");
            arr.arraySize = bars.Length;
            for (int i = 0; i < bars.Length; i++)
            {
                arr.GetArrayElementAtIndex(i).FindPropertyRelative("fill").objectReferenceValue = bars[i].fill;
                arr.GetArrayElementAtIndex(i).FindPropertyRelative("value").floatValue = bars[i].value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            Save(root);
        }

        // ───────────── HUD, subtitles, overlays ─────────────

        static void HUD()
        {
            var canvas = Canvas("UI_HUD", 1800, 110);
            var pillSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SFPaths.Sprites + "/pill.png");
            var pillOutline = AssetDatabase.LoadAssetAtPath<Sprite>(SFPaths.Sprites + "/pill_outline.png");
            var circleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SFPaths.Sprites + "/circle.png");
            var circleOutlineSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SFPaths.Sprites + "/circle_outline.png");

            // Stage rail
            var rail = HStack(canvas, 6, "StageRail", TextAnchor.MiddleLeft, 10, 8);
            rail.anchorMin = rail.anchorMax = rail.pivot = new Vector2(0, 0.5f);
            rail.anchoredPosition = Vector2.zero;
            rail.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            Fill(rail, pillSprite, T.panel, 1f);
            Outline(rail, pillOutline, T.panelBorder, 1f);
            var pills = new Image[4]; var labels = new TextMeshProUGUI[4];
            string[] names = { "Prep", "Access", "Operate", "Close" };
            for (int i = 0; i < 4; i++)
            {
                var item = HStack(rail, 0, "Stage_" + names[i], TextAnchor.MiddleCenter, 26, 0);
                pills[i] = Fill(item, pillSprite, T.accent, 64f / 60f);
                labels[i] = Text(item, names[i], TS.Chip, T.textSecondary, TextAlignmentOptions.Center, "Label");
                labels[i].fontSize = 22;
                LE(item, -1, 60);
            }
            var railC = canvas.gameObject.AddComponent<StageRail>();
            Ser.Set(railC, "theme", T);
            Ser.SetArray(railC, "pills", pills);
            Ser.SetArray(railC, "labels", labels);
            railC.Show(ScenarioStage.Prep); // bake the initial look so the prefab reads correctly in the editor

            // Clock, mode badge, pause
            var right = HStack(canvas, 14, "SessionCluster", TextAnchor.MiddleRight);
            right.anchorMin = right.anchorMax = right.pivot = new Vector2(1, 0.5f);
            right.anchoredPosition = Vector2.zero;
            right.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var clockChip = HStack(right, 0, "Clock", TextAnchor.MiddleCenter, 26, 0);
            Fill(clockChip, pillSprite, T.panel, 64f / 72f);
            Outline(clockChip, pillOutline, T.panelBorder, 64f / 72f);
            var clock = Text(clockChip, "<mspace=0.62em>00:00</mspace>", TS.Subheading, null, TextAlignmentOptions.Center, "Time");
            LE(clockChip, -1, 72);
            var badge = HStack(right, 0, "ModeBadge", TextAnchor.MiddleCenter, 26, 0);
            Fill(badge, pillSprite, T.accentChip, 64f / 72f);
            var badgeText = Text(badge, "GUIDED", TS.Chip, T.accent, TextAlignmentOptions.Center, "Label");
            badgeText.fontSize = 22;
            LE(badge, -1, 72);

            var binder = canvas.gameObject.AddComponent<SessionHudBinder>();
            Ser.Set(binder, "clock", clock);
            Ser.Set(binder, "modeBadge", badgeText);

            var pause = Rect("Btn_Pause", right);
            LE(pause, 72, 72, 0);
            var pimg = pause.gameObject.AddComponent<Image>();
            pimg.sprite = circleSprite; pimg.color = T.panel;
            var ring = Rect("Ring", pause); Stretch(ring);
            var ri = ring.gameObject.AddComponent<Image>(); ri.sprite = circleOutlineSprite; ri.color = T.panelBorder; ri.raycastTarget = false;
            var pi = Icon(pause, Pause, 34, T.textPrimary, "Icon");
            pi.GetComponent<LayoutElement>().ignoreLayout = true;
            var pit = (RectTransform)pi.transform; pit.anchorMin = pit.anchorMax = new Vector2(0.5f, 0.5f); pit.sizeDelta = new Vector2(34, 34);
            var pb = pause.gameObject.AddComponent<Button>();
            pb.targetGraphic = pimg;
            pause.gameObject.AddComponent<UIButtonFeedback>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(pb.onClick, binder.TogglePause);

            // Event toast (slides in under the rail)
            var toast = HStack(canvas, 18, "EventToast", TextAnchor.MiddleLeft, 16, 10);
            toast.anchorMin = toast.anchorMax = toast.pivot = new Vector2(0.5f, 0.5f);
            toast.anchoredPosition = new Vector2(0, -150);
            toast.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var cardSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SFPaths.Sprites + "/card_r14.png");
            var cardOutline = AssetDatabase.LoadAssetAtPath<Sprite>(SFPaths.Sprites + "/card_r14_outline.png");
            Fill(toast, cardSprite, T.panel, 0.75f);
            var tOutline = Outline(toast, cardOutline, T.accent, 0.5f);
            var chip = Chip(toast, "On protocol", T.onAccent, T.accent, 40);
            var msg = Text(toast, "Nails cleaned", TS.Body, null, TextAlignmentOptions.Left, "Message");
            msg.textWrappingMode = TextWrappingModes.NoWrap;
            var time = Text(toast, "00:48", TS.Mono, T.textSecondary, TextAlignmentOptions.Left, "Time");
            LE(toast, -1, 68);
            var tg = toast.gameObject.AddComponent<CanvasGroup>();
            var tp = canvas.gameObject.AddComponent<EventToastPresenter>();
            Ser.Set(tp, "theme", T);
            Ser.Set(tp, "group", tg);
            Ser.Set(tp, "outline", tOutline);
            Ser.Set(tp, "chip", chip.GetComponent<Image>());
            Ser.Set(tp, "chipLabel", chip.GetComponentInChildren<TextMeshProUGUI>());
            Ser.Set(tp, "message", msg);
            Ser.Set(tp, "time", time);
            Save(canvas.gameObject, SFPaths.UIHud);
        }

        static void Subtitles()
        {
            var canvas = Canvas("UI_Subtitles", 1400, 110, false);
            var bar = HStack(canvas, 22, "Bar", TextAnchor.MiddleCenter, 34, 16);
            bar.anchorMin = bar.anchorMax = bar.pivot = new Vector2(0.5f, 0.5f);
            var fit = bar.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Fill(bar, AssetDatabase.LoadAssetAtPath<Sprite>(SFPaths.Sprites + "/card_r14.png"), new Color(0.03f, 0.07f, 0.075f, 0.94f), 0.75f);
            Icon(bar, Speaker, 34, T.accent);
            var label = Text(bar, "“Subtitle”", TS.Body, null, TextAlignmentOptions.Center, "Label");
            label.fontSize = 30;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            var group = bar.gameObject.AddComponent<CanvasGroup>();
            var sp = canvas.gameObject.AddComponent<SubtitlePresenter>();
            Ser.Set(sp, "group", group);
            Ser.Set(sp, "label", label);
            Save(canvas.gameObject, SFPaths.UIHud);
        }

        static void ReplayControls()
        {
            var p = Screen("UI_90_ReplayControls", 1400, V.Panel, 44, 18, out var canvas);
            Text(p, "Replay · [Learner name] · [Date]", TS.Eyebrow);
            Text(p, "Session replay", TS.Heading);
            var timeline = Rect("Timeline", p);
            LE(timeline, -1, 40);
            var holder = VStack(timeline, 0, 0, "Holder", TextAnchor.MiddleLeft);
            Stretch(holder);
            var fill = Bar(holder, 0.38f, T.accent, 0.38f, T.textPrimary, null, 12);
            (float at, Color c)[] marks = { (0.17f, T.danger), (0.38f, T.danger), (0.55f, T.accent), (0.64f, T.warning), (0.88f, T.accent) };
            foreach (var (at, c) in marks)
            {
                var m = Rect("Event", fill.parent);
                m.anchorMin = m.anchorMax = new Vector2(at, 0.5f);
                m.sizeDelta = new Vector2(16, 16);
                var i = m.gameObject.AddComponent<Image>();
                i.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SFPaths.Sprites + "/circle.png"); i.color = c; i.raycastTarget = false;
            }
            var r = HRow(p, 16);
            Button(r, "Play", BV.Primary, 76, 180, 0);
            Text(r, "05:24 / 14:20", TS.Mono, T.textPrimary);
            Spacer(r, -1, 1);
            Text(r, "Camera", TS.Eyebrow);
            Button(r, "Learner", BV.ChipSelected, 60);
            Button(r, "Laparoscope", BV.Chip, 60);
            Button(r, "Free", BV.Chip, 60);
            Save(canvas.gameObject);
        }

        /// <summary>Overlay sized to the 0.68 × 0.40 m monitor screen (1 px = 1 mm).</summary>
        static void MonitorOverlay(string name, string label, string chip, string caption, bool drift)
        {
            var canvas = Canvas(name, 680, 400, false);
            var lab = Text(canvas, label, TS.Mono, new Color(0.93f, 0.95f, 0.95f), TextAlignmentOptions.TopLeft, "ScopeLabel");
            lab.fontSize = 18;
            var lrt = (RectTransform)lab.transform;
            lrt.anchorMin = lrt.anchorMax = lrt.pivot = new Vector2(0, 1); lrt.anchoredPosition = new Vector2(18, -14); lrt.sizeDelta = new Vector2(420, 30);
            if (chip != null)
            {
                var c = Chip(canvas, chip, T.accent, new Color(0.04f, 0.18f, 0.16f, 0.95f), 32);
                c.GetComponentInChildren<TextMeshProUGUI>().fontSize = 15;
                c.anchorMin = c.anchorMax = c.pivot = new Vector2(1, 1); c.anchoredPosition = new Vector2(-14, -12);
                c.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            if (caption != null)
            {
                var bar = HStack(canvas, 0, "Caption", TextAnchor.MiddleCenter, 16, 6);
                bar.anchorMin = bar.anchorMax = bar.pivot = new Vector2(0.5f, 0);
                bar.anchoredPosition = new Vector2(0, 16);
                bar.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                Fill(bar, AssetDatabase.LoadAssetAtPath<Sprite>(SFPaths.Sprites + "/card_r14.png"), new Color(0.03f, 0.05f, 0.05f, 0.9f), 1.2f);
                var t = Text(bar, caption, TS.Body, null, TextAlignmentOptions.Center);
                t.fontSize = 17; t.textWrappingMode = TextWrappingModes.NoWrap;
                LE(bar, -1, 36);
            }
            if (drift)
            {
                var frame = Rect("DriftFrame", canvas); Stretch(frame, -10);
                Fill(frame, AssetDatabase.LoadAssetAtPath<Sprite>(SFPaths.Sprites + "/card_r14_outline.png"), T.warning, 0.18f);
                var banner = HStack(canvas, 10, "DriftBanner", TextAnchor.MiddleLeft, 14, 8);
                banner.anchorMin = banner.anchorMax = banner.pivot = new Vector2(0, 0.5f);
                banner.anchoredPosition = new Vector2(16, 0);
                var fit = banner.gameObject.AddComponent<ContentSizeFitter>();
                fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize; fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                var arrow = Icon(banner, Chevron, 30, T.warning, "Arrow");
                arrow.transform.localRotation = Quaternion.Euler(0, 0, 180);
                var box = VStack(banner, 2, 10, "Box");
                Fill(box, AssetDatabase.LoadAssetAtPath<Sprite>(SFPaths.Sprites + "/card_r14.png"), T.warningSurface, 1.2f);
                Outline(box, AssetDatabase.LoadAssetAtPath<Sprite>(SFPaths.Sprites + "/card_r14_outline.png"), T.warning, 1.2f);
                var e = Text(box, "Drift · left instrument", TS.Eyebrow, T.warning); e.fontSize = 13;
                var m = Text(box, "Out of view for <mspace=0.62em>1.4</mspace> s", TS.Body); m.fontSize = 18;
            }
            Save(canvas.gameObject, SFPaths.UIHud);
        }
    }
}
