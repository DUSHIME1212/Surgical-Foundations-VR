using System.Collections.Generic;
using SurgicalFoundations.Contracts;
using SurgicalFoundations.Core;
using SurgicalFoundations.Replay;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SurgicalFoundations.UI
{
    /// <summary>
    /// Screen 90: timeline with event markers (click one to jump there, FR-22), play/pause, time, the event being
    /// replayed, and the camera choice. Keyboard: Space play/pause, ←/→ 5 s, 1/2/3 cameras.
    /// </summary>
    public class ReplayControlsBinder : MonoBehaviour
    {
        [SerializeField] UITheme theme;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text heading;
        [SerializeField] RectTransform timeline;
        [SerializeField] RectTransform fill;
        [SerializeField] RectTransform playhead;
        [SerializeField] RectTransform markerTemplate;
        [SerializeField] TMP_Text playLabel;
        [SerializeField] TMP_Text timeLabel;
        [SerializeField] TMP_Text caption;
        [SerializeField] Button[] cameraButtons; // Learner, Laparoscope, Free

        readonly List<GameObject> markers = new List<GameObject>();
        ReplayPlayer player;

        void OnEnable()
        {
            player = ReplayPlayer.Instance != null ? ReplayPlayer.Instance : FindAnyObjectByType<ReplayPlayer>();
            if (player == null) return;
            player.Loaded += OnLoaded;
            player.LoadFailed += OnFailed;
            // The builder leaves preview dots (named "Event") for the UI previews; real markers are made per replay.
            if (markerTemplate != null)
                foreach (Transform child in markerTemplate.parent)
                    if (child.name == "Event") child.gameObject.SetActive(false);
            if (player.File != null) OnLoaded();
            else
            {
                Set(heading, "Session replay");
                Set(title, "Waiting for a replay…");
                Set(caption, "");
            }
        }

        void OnDisable()
        {
            if (player == null) return;
            player.Loaded -= OnLoaded;
            player.LoadFailed -= OnFailed;
        }

        void OnLoaded()
        {
            Set(title, player.Title);
            Set(heading, "Session replay");
            BuildMarkers();
            RefreshCameraButtons();
        }

        void OnFailed(string message)
        {
            Set(title, message);
            Set(caption, "");
        }

        void Update()
        {
            if (player == null || player.File == null) return;
            HandleKeys();

            var k = player.Duration > 0 ? player.Time / player.Duration : 0f;
            if (fill != null) fill.anchorMax = new Vector2(k, fill.anchorMax.y);
            if (playhead != null)
            {
                playhead.anchorMin = new Vector2(k, playhead.anchorMin.y);
                playhead.anchorMax = new Vector2(k, playhead.anchorMax.y);
            }
            Set(playLabel, player.Playing ? "Pause" : "Play");
            Set(timeLabel, $"{SessionManager.FormatClock(player.Time)} / {SessionManager.FormatClock(player.Duration)}");

            var e = player.CurrentEvent();
            if (caption != null)
            {
                caption.text = e == null ? "" : $"{SessionManager.FormatClock(e.sessionTime)} · {Label(e.eventClass)} · {e.message ?? e.code}";
                if (e != null && theme != null) caption.color = ColorFor(e.eventClass);
            }
        }

        void HandleKeys()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.spaceKey.wasPressedThisFrame) TogglePlay();
            if (kb.leftArrowKey.wasPressedThisFrame) player.Skip(-5f);
            if (kb.rightArrowKey.wasPressedThisFrame) player.Skip(5f);
            if (kb.digit1Key.wasPressedThisFrame) ShowLearnerView();
            if (kb.digit2Key.wasPressedThisFrame) ShowLaparoscopeView();
            if (kb.digit3Key.wasPressedThisFrame) ShowFreeView();
        }

        // ───────────── Buttons (wired by UIScreensBuilder) ─────────────

        public void TogglePlay() => player?.TogglePlay();
        public void ShowLearnerView() { player?.SetCamera(ReplayCameraMode.Learner); RefreshCameraButtons(); }
        public void ShowLaparoscopeView() { player?.SetCamera(ReplayCameraMode.Laparoscope); RefreshCameraButtons(); }
        public void ShowFreeView() { player?.SetCamera(ReplayCameraMode.Free); RefreshCameraButtons(); }

        /// <summary>Called by <see cref="TimelineScrubber"/> with 0–1 along the timeline.</summary>
        public void ScrubTo(float normalized)
        {
            if (player == null || player.File == null) return;
            player.Seek(normalized * player.Duration);
        }

        // ───────────── helpers ─────────────

        void BuildMarkers()
        {
            foreach (var m in markers) Destroy(m);
            markers.Clear();
            if (markerTemplate == null || player.Duration <= 0f) return;

            foreach (var e in player.Events)
            {
                // On-protocol and info events would crowd the bar; mark what an instructor wants to jump to.
                if (e.eventClass != EventClass.Deviation && e.eventClass != EventClass.Delayed) continue;
                var marker = Instantiate(markerTemplate, markerTemplate.parent);
                marker.gameObject.SetActive(true);
                marker.name = "Event_" + e.code;
                var x = Mathf.Clamp01(e.sessionTime / player.Duration);
                marker.anchorMin = marker.anchorMax = new Vector2(x, 0.5f);
                var image = marker.GetComponent<Image>();
                if (image != null) { image.color = ColorFor(e.eventClass); image.raycastTarget = true; }
                var button = marker.GetComponent<Button>();
                if (button == null) button = marker.gameObject.AddComponent<Button>();
                var t = e.sessionTime;
                // Start a moment before the event so the lead-up is visible.
                button.onClick.AddListener(() => player.Seek(t - 2f));
                markers.Add(marker.gameObject);
            }
        }

        void RefreshCameraButtons()
        {
            if (cameraButtons == null || theme == null || player == null) return;
            for (int i = 0; i < cameraButtons.Length; i++)
            {
                var b = cameraButtons[i];
                if (b == null) continue;
                var selected = (int)player.CameraMode == i;
                if (b.targetGraphic is Image img) img.color = selected ? theme.accentSoft : theme.card;
                // Chips also have a separate outline image; it carries most of the "selected" look.
                foreach (var outline in b.GetComponentsInChildren<Image>(true))
                    if (outline != b.targetGraphic) outline.color = selected ? theme.accent : theme.cardBorder;
                var label = b.GetComponentInChildren<TMP_Text>();
                if (label != null) label.color = selected ? theme.accent : theme.textPrimary;
                b.interactable = i != (int)ReplayCameraMode.Laparoscope || player.HasLaparoscope;
            }
        }

        Color ColorFor(EventClass c) =>
            theme == null ? Color.white : c == EventClass.Deviation ? theme.danger : c == EventClass.Delayed ? theme.warning : theme.accent;

        static string Label(EventClass c) => c == EventClass.OnProtocol ? "On protocol" : c.ToString();

        static void Set(TMP_Text t, string v) { if (t != null) t.text = v; }
    }
}
