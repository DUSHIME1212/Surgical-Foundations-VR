using SurgicalFoundations.Audio;
using SurgicalFoundations.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace SurgicalFoundations.EditorTools
{
    /// <summary>
    /// uGUI component kit reproducing the headset UI prototype: glass panels, pills, segmented cards, checklists,
    /// gauges. Units are canvas pixels at 1 px = 1 mm (canvas scale 0.001), laid out with layout groups so text
    /// changes and localisation reflow automatically.
    /// </summary>
    public static class UIKit
    {
        public const float MetersPerPixel = 0.001f;
        const int UILayer = 5;

        public static UITheme T;
        static TMP_FontAsset font;
        static Sprite panel, panelOutline, card, cardOutline, pill, pillOutline, bar, circle, circleOutline;
        public static Sprite Check, Warning, Pause, Speaker, Logo, Chevron, Play, QR, Dashed, Glow, Vignette;

        public enum V { Panel, Danger, Warning, Card, CardActive, CardWarning, CardDanger, Clear }
        public enum TS { Eyebrow, Display, Title, Heading, Subheading, Body, BodyStrong, Secondary, Small, Mono, MonoLarge, Button, Chip }
        public enum BV { Primary, Secondary, Danger, DangerOutline, Warning, Link, Chip, ChipSelected }

        public static void Init(UITheme theme)
        {
            T = theme;
            font = theme.font != null ? theme.font : TMP_Settings.defaultFontAsset;
            Sprite S(string folder, string n) => UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/{n}.png");
            panel = S(SFPaths.Sprites, "panel_r28"); panelOutline = S(SFPaths.Sprites, "panel_r28_outline");
            card = S(SFPaths.Sprites, "card_r14"); cardOutline = S(SFPaths.Sprites, "card_r14_outline");
            pill = S(SFPaths.Sprites, "pill"); pillOutline = S(SFPaths.Sprites, "pill_outline");
            bar = S(SFPaths.Sprites, "bar_r8"); circle = S(SFPaths.Sprites, "circle"); circleOutline = S(SFPaths.Sprites, "circle_outline");
            Dashed = S(SFPaths.Sprites, "circle_dashed"); Glow = S(SFPaths.Sprites, "soft_glow"); Vignette = S(SFPaths.Sprites, "vignette_edge");
            Check = S(SFPaths.Icons, "icon_check"); Warning = S(SFPaths.Icons, "icon_warning"); Pause = S(SFPaths.Icons, "icon_pause");
            Speaker = S(SFPaths.Icons, "icon_speaker"); Logo = S(SFPaths.Icons, "icon_logo"); Chevron = S(SFPaths.Icons, "icon_chevron_right");
            Play = S(SFPaths.Icons, "icon_play"); QR = S(SFPaths.Icons, "qr_placeholder");
            TickSprites = EnsureTickSpriteAsset();
        }

        // ───────────── structure ─────────────

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = UILayer };
            var rt = (RectTransform)go.transform;
            if (parent != null) rt.SetParent(parent, false);
            return rt;
        }

        public static void Stretch(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset); rt.offsetMax = new Vector2(-inset, -inset);
        }

        /// <summary>World-space canvas root with XR raycaster and a CanvasGroup (for UIPanel fades).</summary>
        public static RectTransform Canvas(string name, float width, float height, bool interactive = true)
        {
            var rt = Rect(name, null);
            var c = rt.gameObject.AddComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            var scaler = rt.gameObject.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 2f;
            scaler.referencePixelsPerUnit = 100f;
            if (interactive) rt.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
            rt.gameObject.AddComponent<CanvasGroup>();
            rt.sizeDelta = new Vector2(width, height);
            rt.localScale = Vector3.one * MetersPerPixel;
            return rt;
        }

        public static LayoutElement LE(Component c, float prefW = -1, float prefH = -1, float flexW = -1, float flexH = -1, float minW = -1, float minH = -1)
        {
            // TryGetComponent, not '??': in the editor GetComponent returns a fake-null object that '??' treats as real.
            if (!c.TryGetComponent<LayoutElement>(out var le)) le = c.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = prefW; le.preferredHeight = prefH;
            le.flexibleWidth = flexW; le.flexibleHeight = flexH;
            le.minWidth = minW; le.minHeight = minH >= 0 ? minH : prefH;
            return le;
        }

        public static RectTransform VStack(Transform parent, float spacing = 16, int pad = 0, string name = "VStack", TextAnchor align = TextAnchor.UpperLeft)
        {
            var rt = Rect(name, parent);
            var g = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            g.spacing = spacing; g.padding = new RectOffset(pad, pad, pad, pad); g.childAlignment = align;
            g.childControlWidth = true; g.childControlHeight = true; g.childForceExpandWidth = true; g.childForceExpandHeight = false;
            return rt;
        }

        public static RectTransform HStack(Transform parent, float spacing = 16, string name = "HStack", TextAnchor align = TextAnchor.MiddleLeft, int padH = 0, int padV = 0)
        {
            var rt = Rect(name, parent);
            var g = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            g.spacing = spacing; g.childAlignment = align; g.padding = new RectOffset(padH, padH, padV, padV);
            g.childControlWidth = true; g.childControlHeight = true; g.childForceExpandWidth = false; g.childForceExpandHeight = false;
            return rt;
        }

        public static RectTransform Spacer(Transform parent, float h = -1, float flexW = -1)
        {
            var rt = Rect("Spacer", parent);
            LE(rt, -1, h, flexW);
            return rt;
        }

        // ───────────── surfaces ─────────────

        static (Sprite fill, Sprite outline, float ppu) SpritesFor(V v) => v switch
        {
            V.Panel or V.Danger or V.Warning => (panel, panelOutline, 1f),
            _ => (card, cardOutline, 0.75f)
        };

        public static (Color fill, Color border) ColorsFor(V v) => v switch
        {
            V.Panel => (T.panel, T.panelBorder),
            V.Danger => (T.dangerSurface, T.danger),
            V.Warning => (T.warningSurface, T.warning),
            V.Card => (T.card, T.cardBorder),
            V.CardActive => (T.accentSoft, T.accentSoft),
            V.CardWarning => (T.warningSurface, T.warning),
            V.CardDanger => (T.dangerInset, T.dangerInset),
            _ => (new Color(0, 0, 0, 0), new Color(0, 0, 0, 0))
        };

        public static Image Fill(RectTransform rt, Sprite sprite, Color color, float ppu = 1f, bool raycast = false)
        {
            if (!rt.TryGetComponent<Image>(out var img)) img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite; img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = ppu;
            img.color = color; img.raycastTarget = raycast;
            return img;
        }

        public static Image Outline(RectTransform host, Sprite sprite, Color color, float ppu = 1f)
        {
            var o = Rect("Outline", host);
            Stretch(o);
            o.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var img = Fill(o, sprite, color, ppu);
            o.SetAsFirstSibling();
            return img;
        }

        /// <summary>Surface container with vertical layout; sizes to its content unless a width is given.</summary>
        public static RectTransform Surface(Transform parent, V v, int pad = 48, float spacing = 18, float width = -1, string name = "Panel")
        {
            var rt = VStack(parent, spacing, pad, name);
            var (fs, os, ppu) = SpritesFor(v);
            var (fc, bc) = ColorsFor(v);
            Fill(rt, fs, fc, ppu, v != V.Clear);
            if (v != V.Clear && v != V.CardActive && v != V.CardDanger) Outline(rt, os, bc, ppu);
            if (width > 0) LE(rt, width);
            return rt;
        }

        /// <summary>Top-level panel centred on a canvas and sized to content (CanvasGroup + UIPanel for open/close tweens).</summary>
        public static RectTransform RootPanel(RectTransform canvas, V v, float width, int pad = 52, float spacing = 20, string name = "Panel")
        {
            var rt = Surface(canvas, v, pad, spacing, -1, name);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(width, 100);
            var fit = rt.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return rt;
        }

        // ───────────── text ─────────────

        public static TextMeshProUGUI Text(Transform parent, string s, TS style, Color? color = null, TextAlignmentOptions align = TextAlignmentOptions.TopLeft, string name = null)
        {
            var rt = Rect(name ?? "Text_" + style, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.alignment = align;
            t.richText = true;
            t.color = T.textPrimary;
            switch (style)
            {
                case TS.Eyebrow: t.fontSize = 20; t.fontStyle = FontStyles.Bold | FontStyles.UpperCase; t.characterSpacing = 9; t.color = T.textSecondary; break;
                case TS.Display: t.fontSize = 120; t.fontStyle = FontStyles.Bold; t.color = T.accent; t.characterSpacing = -2; break;
                case TS.Title: t.fontSize = 46; t.fontStyle = FontStyles.Bold; break;
                case TS.Heading: t.fontSize = 36; t.fontStyle = FontStyles.Bold; break;
                case TS.Subheading: t.fontSize = 29; t.fontStyle = FontStyles.Bold; break;
                case TS.Body: t.fontSize = 27; break;
                case TS.BodyStrong: t.fontSize = 27; t.fontStyle = FontStyles.Bold; break;
                case TS.Secondary: t.fontSize = 25; t.color = T.textSecondary; break;
                case TS.Small: t.fontSize = 22; t.color = T.textMuted; t.lineSpacing = 4; break;
                case TS.Mono: t.fontSize = 25; t.color = T.textSecondary; s = $"<mspace=0.62em>{s}</mspace>"; break;
                case TS.MonoLarge: t.fontSize = 84; t.fontStyle = FontStyles.Bold; s = $"<mspace=0.78em>{s}</mspace>"; break;
                case TS.Button: t.fontSize = 28; t.fontStyle = FontStyles.Bold; break;
                case TS.Chip: t.fontSize = 18; t.fontStyle = FontStyles.Bold | FontStyles.UpperCase; t.characterSpacing = 7; break;
            }
            if (color.HasValue) t.color = color.Value;
            t.spriteAsset = TickSprites;
            t.text = s.Replace("✓", Tick);
            if (style == TS.MonoLarge) t.textWrappingMode = TextWrappingModes.NoWrap;
            // TMP reports a flexible width; pin it to 0 so rows only stretch what we mark with Flex()/LE().
            LE(t, -1, -1, 0);
            return t;
        }

        /// <summary>Inline check mark (LiberationSans has no U+2713). Tinted with the text colour.</summary>
        public const string Tick = "<sprite name=\"check\" tint=1>";
        public static TMP_SpriteAsset TickSprites;

        public static TMP_SpriteAsset EnsureTickSpriteAsset()
        {
            const string path = SFPaths.Data + "/UI/TMP_Icons.asset";
            var existing = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(path);
            if (existing != null && existing.spriteCharacterTable.Count > 0) return existing;
            if (existing != null) UnityEditor.AssetDatabase.DeleteAsset(path);

            var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(SFPaths.Icons + "/icon_check.png");
            var asset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
            asset.spriteSheet = tex;
            var glyph = new TMP_SpriteGlyph(0, new UnityEngine.TextCore.GlyphMetrics(96, 96, 0, 82, 104), new UnityEngine.TextCore.GlyphRect(0, 0, 96, 96), 1f, 0);
            asset.spriteGlyphTable.Add(glyph);
            asset.spriteCharacterTable.Add(new TMP_SpriteCharacter(0xE000, glyph) { name = "check" });
            var mat = new Material(Shader.Find("TextMeshPro/Sprite")) { name = "TMP_Icons Material" };
            mat.mainTexture = tex;
            asset.material = mat;
            UnityEditor.AssetDatabase.CreateAsset(asset, path);
            UnityEditor.AssetDatabase.AddObjectToAsset(mat, asset);
            // Stamp the current format version, otherwise UpdateLookupTables runs the legacy upgrade path and wipes the tables.
            Ser.Set(asset, "m_Version", "1.1.0");
            asset.UpdateLookupTables();
            UnityEditor.EditorUtility.SetDirty(asset);
            return asset;
        }

        // ───────────── controls ─────────────

        public static Button Button(Transform parent, string label, BV v, float height = 88, float prefWidth = -1, float flex = -1, UnityAction onClick = null)
        {
            var rt = Rect("Btn_" + Sanitize(label), parent);
            Color fill, text, border = new Color(0, 0, 0, 0);
            Sprite fillSprite = card, outlineSprite = cardOutline;
            float ppu = 0.75f;
            switch (v)
            {
                case BV.Primary: fill = T.accent; text = T.onAccent; break;
                case BV.Danger: fill = T.danger; text = TextOn(T.danger); break;
                case BV.Warning: fill = T.warning; text = TextOn(T.warning); break;
                case BV.DangerOutline: fill = T.card; text = T.danger; border = T.danger; break;
                case BV.Link: fill = new Color(0, 0, 0, 0); text = T.accent; break;
                case BV.Chip: fill = T.card; text = T.textPrimary; border = T.cardBorder; fillSprite = pill; outlineSprite = pillOutline; ppu = 64f / height; break;
                case BV.ChipSelected: fill = T.accentSoft; text = T.accent; border = T.accent; fillSprite = pill; outlineSprite = pillOutline; ppu = 64f / height; break;
                default: fill = T.card; text = T.textPrimary; border = T.cardBorder; break;
            }
            var img = Fill(rt, fillSprite, fill, ppu, true);
            if (border.a > 0f) Outline(rt, outlineSprite, border, ppu);

            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleCenter; h.padding = new RectOffset(28, 28, 0, 0);
            h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = false; h.childForceExpandHeight = false;
            var label_ = Text(rt, label, v == BV.Chip || v == BV.ChipSelected ? TS.Body : TS.Button, text, TextAlignmentOptions.Center, "Label");
            label_.textWrappingMode = TextWrappingModes.NoWrap;

            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var cb = b.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            cb.selectedColor = Color.white;
            cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            cb.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);
            cb.fadeDuration = 0.08f;
            b.colors = cb;
            var nav = b.navigation; nav.mode = Navigation.Mode.None; b.navigation = nav;

            var fb = rt.gameObject.AddComponent<UIButtonFeedback>();
            fb.ClickSound = v == BV.Primary ? SoundId.UI_Confirm : v == BV.Link ? SoundId.UI_Back : SoundId.UI_Click;
            if (onClick != null) UnityEditor.Events.UnityEventTools.AddPersistentListener(b.onClick, onClick);
            LE(rt, prefWidth, height, flex);
            return b;
        }

        static Color TextOn(Color c) => Color.Lerp(c, Color.black, 0.88f);

        public static Toggle Segment(Transform parent, ToggleGroup group, string title, string desc, bool on, float height, float flex = 1,
            UnityAction<bool> onChanged = null, bool pillShape = false, float titleSize = -1)
        {
            var rt = Rect("Seg_" + Sanitize(title), parent);
            float ppu = pillShape ? 64f / height : 0.75f;
            var fill = Fill(rt, pillShape ? pill : card, on ? T.accentSoft : T.card, ppu, true);
            var outline = Outline(rt, pillShape ? pillOutline : cardOutline, on ? T.accent : T.cardBorder, ppu);
            outline.pixelsPerUnitMultiplier = ppu * 0.5f; // thicker selected edge like the prototype

            var content = VStack(rt, 4, 0, "Content", desc == null ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft);
            Stretch(content);
            var vg = content.GetComponent<VerticalLayoutGroup>();
            vg.padding = desc == null ? new RectOffset(16, 16, 0, 0) : new RectOffset(28, 28, 18, 18);
            vg.childForceExpandHeight = false;
            var t = Text(content, title, desc == null ? TS.Body : TS.Subheading, on ? T.accent : T.textPrimary,
                desc == null ? TextAlignmentOptions.Center : TextAlignmentOptions.Left, "Title");
            if (titleSize > 0) t.fontSize = titleSize;
            if (desc != null) Text(content, desc, TS.Secondary, null, TextAlignmentOptions.Left, "Description");

            var tog = rt.gameObject.AddComponent<Toggle>();
            tog.targetGraphic = fill;
            tog.group = group;
            tog.isOn = on;
            var nav = tog.navigation; nav.mode = Navigation.Mode.None; tog.navigation = nav;
            var seg = rt.gameObject.AddComponent<SegmentedOption>();
            Ser.Set(seg, "theme", T);
            Ser.Set(seg, "fill", fill);
            Ser.Set(seg, "outline", outline);
            Ser.SetArray(seg, "labels", new Object[] { t });
            var fb = rt.gameObject.AddComponent<UIButtonFeedback>();
            fb.ClickSound = SoundId.None;
            if (onChanged != null) UnityEditor.Events.UnityEventTools.AddPersistentListener(tog.onValueChanged, onChanged);
            LE(rt, -1, height, flex);
            return tog;
        }

        public static ToggleGroup Group(Transform host)
        {
            var g = host.gameObject.AddComponent<ToggleGroup>();
            g.allowSwitchOff = false;
            return g;
        }

        // ───────────── small pieces ─────────────

        public static RectTransform Chip(Transform parent, string text, Color fg, Color bg, float height = 40, bool outline = false)
        {
            var rt = HStack(parent, 0, "Chip_" + Sanitize(text), TextAnchor.MiddleCenter, 18, 0);
            Fill(rt, pill, bg, 64f / height);
            if (outline) Outline(rt, pillOutline, fg, 64f / height);
            var t = Text(rt, text, TS.Chip, fg, TextAlignmentOptions.Center, "Label");
            t.textWrappingMode = TextWrappingModes.NoWrap;
            LE(rt, -1, height);
            return rt;
        }

        public static Image Icon(Transform parent, Sprite s, float size, Color color, string name = "Icon")
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = s; img.color = color; img.raycastTarget = false; img.preserveAspect = true;
            LE(rt, size, size, 0);
            return img;
        }

        public static Image Divider(Transform parent)
        {
            var rt = Rect("Divider", parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = T.divider; img.raycastTarget = false;
            LE(rt, -1, 2);
            return img;
        }

        public enum Row { Done, Active, Pending, Warning, Muted }

        /// <summary>Checklist row as used on the scrub, count, task and port-removal screens.</summary>
        public static RectTransform CheckRow(Transform parent, string text, Row state, string trailing = null, float height = 66)
        {
            var rt = HStack(parent, 20, "Row_" + Sanitize(text), TextAnchor.MiddleLeft, 18, 0);
            if (state == Row.Active) Fill(rt, card, T.accentSoft, 0.75f);
            if (state == Row.Warning) { Fill(rt, card, T.warningSurface, 0.75f); Outline(rt, cardOutline, T.warning, 0.75f); }
            switch (state)
            {
                case Row.Done: Icon(rt, circle, 34, T.accent, "Dot"); Icon(rt.GetChild(rt.childCount - 1), Check, 22, T.onAccent, "Check"); break;
                case Row.Muted: Icon(rt, Check, 28, T.accent, "Check"); break;
                case Row.Active: Icon(rt, circleOutline, 34, T.accent, "Ring"); break;
                case Row.Warning: Text(rt, "!", TS.BodyStrong, T.warning, TextAlignmentOptions.Center, "Bang").GetComponent<RectTransform>(); break;
                default: Icon(rt, circleOutline, 34, T.textMuted, "Ring"); break;
            }
            // Centre the tick inside the filled dot.
            if (state == Row.Done)
            {
                var dot = rt.Find("Dot");
                var tick = (RectTransform)dot.Find("Check");
                tick.GetComponent<LayoutElement>().ignoreLayout = true;
                tick.anchorMin = tick.anchorMax = new Vector2(0.5f, 0.5f); tick.sizeDelta = new Vector2(22, 22); tick.anchoredPosition = Vector2.zero;
            }
            var label = Text(rt, text, TS.Body, state == Row.Warning ? T.warning : state == Row.Muted ? T.textSecondary : T.textPrimary, TextAlignmentOptions.Left, "Label");
            LE(label, -1, -1, 1);
            if (trailing != null)
                Text(rt, trailing, TS.Mono, state == Row.Active ? T.accent : state == Row.Warning ? T.warning : T.textSecondary, TextAlignmentOptions.Right, "Trailing");
            LE(rt, -1, height);
            return rt;
        }

        /// <summary>Checklist row driven at runtime by a <see cref="SurgicalFoundations.UI.ChecklistRow"/>; built in its pending state.</summary>
        public static SurgicalFoundations.UI.ChecklistRow LiveCheckRow(Transform parent, string text, float height = 66)
        {
            var rt = HStack(parent, 20, "Row_" + Sanitize(text), TextAnchor.MiddleLeft, 18, 0);
            var background = Fill(rt, card, T.accentSoft, 0.75f);
            background.enabled = false;

            // Ring and filled dot share one slot so the label doesn't shift when the state changes.
            var slot = Rect("State", rt);
            LE(slot, 34, 34, 0);
            var ring = Icon(slot, circleOutline, 34, T.textMuted, "Ring");
            var dot = Icon(slot, circle, 34, T.accent, "Dot");
            var tick = Icon(dot.transform, Check, 22, T.onAccent, "Check");
            foreach (var img in new[] { ring, dot, tick })
            {
                var r = (RectTransform)img.transform;
                var le = img.GetComponent<LayoutElement>();
                if (le != null) le.ignoreLayout = true;
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                r.sizeDelta = img == tick ? new Vector2(22, 22) : new Vector2(34, 34);
                r.anchoredPosition = Vector2.zero;
            }
            dot.gameObject.SetActive(false);

            var label = Text(rt, text, TS.Body, T.textPrimary, TextAlignmentOptions.Left, "Label");
            LE(label, -1, -1, 1);
            var trailing = Text(rt, "", TS.Mono, T.textSecondary, TextAlignmentOptions.Right, "Trailing");
            LE(trailing, -1, -1, -1, -1, 110);
            LE(rt, -1, height);

            var row = rt.gameObject.AddComponent<SurgicalFoundations.UI.ChecklistRow>();
            Ser.Set(row, "theme", T);
            Ser.Set(row, "background", background);
            Ser.Set(row, "ring", ring);
            Ser.Set(row, "dot", dot);
            Ser.Set(row, "label", label);
            Ser.Set(row, "trailing", trailing);
            return row;
        }

        public static RectTransform KV(Transform parent, string key, string value, Color? valueColor = null, bool mono = false, float height = 52)
        {
            var rt = HStack(parent, 12, "KV_" + Sanitize(key));
            var k = Text(rt, key, TS.Body, null, TextAlignmentOptions.Left, "Key");
            LE(k, -1, -1, 1);
            Text(rt, value, mono ? TS.Mono : TS.Body, valueColor ?? T.textPrimary, TextAlignmentOptions.Right, "Value");
            LE(rt, -1, height);
            return rt;
        }

        /// <summary>Labelled bar gauge (entry angle/depth/force, stage scores). Returns the fill rect.</summary>
        public static RectTransform Gauge(Transform parent, string label, string value, Color valueColor, float fill, Color fillColor,
            float marker = -1, Color? markerColor = null, Vector2? zone = null, bool labelRow = true, float barHeight = 14)
        {
            var root = VStack(parent, 12, 0, "Gauge_" + Sanitize(label));
            if (labelRow)
            {
                var row = HStack(root, 8, "Labels");
                var l = Text(row, label, TS.Body, null, TextAlignmentOptions.Left, "Label");
                LE(l, -1, -1, 1);
                Text(row, value, TS.Body, valueColor, TextAlignmentOptions.Right, "Value");
            }
            return Bar(root, fill, fillColor, marker, markerColor, zone, barHeight);
        }

        public static RectTransform Bar(Transform parent, float fill, Color fillColor, float marker = -1, Color? markerColor = null, Vector2? zone = null, float h = 14)
        {
            var track = Rect("Track", parent);
            Fill(track, bar, T.track, 16f / h);
            LE(track, -1, h, 1);
            if (zone.HasValue)
            {
                var z = Rect("Zone", track);
                z.anchorMin = new Vector2(zone.Value.x, 0); z.anchorMax = new Vector2(zone.Value.y, 1);
                z.offsetMin = z.offsetMax = Vector2.zero;
                Fill(z, bar, new Color(T.accent.r, T.accent.g, T.accent.b, 0.28f), 16f / h);
            }
            var f = Rect("Fill", track);
            f.anchorMin = Vector2.zero; f.anchorMax = new Vector2(fill, 1);
            f.offsetMin = f.offsetMax = Vector2.zero;
            Fill(f, bar, fillColor, 16f / h);
            if (marker >= 0f)
            {
                var m = Rect("Marker", track);
                m.anchorMin = new Vector2(marker, -0.35f); m.anchorMax = new Vector2(marker, 1.35f);
                m.sizeDelta = new Vector2(4, 0); m.anchoredPosition = Vector2.zero;
                var img = m.gameObject.AddComponent<Image>();
                img.color = markerColor ?? T.textPrimary; img.raycastTarget = false;
            }
            return f;
        }

        /// <summary>Numbered step (calibration screen): filled circle for active/done, ring for upcoming.</summary>
        public static RectTransform Numbered(Transform parent, int n, string title, bool filled)
        {
            var row = HStack(parent, 22, "Step_" + n, TextAnchor.UpperLeft);
            var num = Rect("Number", row);
            LE(num, 50, 50, 0);
            var img = num.gameObject.AddComponent<Image>();
            img.sprite = filled ? circle : circleOutline; img.color = T.accent; img.raycastTarget = false;
            var nt = Text(num, n.ToString(), TS.BodyStrong, filled ? T.onAccent : T.accent, TextAlignmentOptions.Center, "N");
            Stretch((RectTransform)nt.transform);
            var body = VStack(row, 10, 0, "Body");
            LE(body, -1, -1, 1);
            Text(body, title, TS.Subheading, null, TextAlignmentOptions.Left, "Title");
            return body;
        }

        public static RectTransform StatusLine(Transform parent, string text, Color dot)
        {
            var row = HStack(parent, 16, "Status");
            Icon(row, circle, 16, dot, "Dot");
            var t = Text(row, text, TS.Secondary, null, TextAlignmentOptions.Left);
            LE(t, -1, -1, 1);
            return row;
        }

        public static string Sanitize(string s)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var ch in s) if (char.IsLetterOrDigit(ch)) sb.Append(ch);
            return sb.Length > 28 ? sb.ToString(0, 28) : sb.ToString();
        }
    }
}
