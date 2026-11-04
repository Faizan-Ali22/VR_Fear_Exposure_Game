using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FearExposure.Level1
{
    /// <summary>
    /// Gaze Reticle & Real-Time Timer for VR & XR Simulator testing.
    /// Attached to the Head Camera. Displays:
    /// 1. A central reticle dot showing exact aim point.
    /// 2. A circular progress fill ring and countdown timer when gazing at any armed LookTarget.
    /// 3. Visual feedback (color shifts, pulsing) when gaze is locked on target.
    /// 4. An OnGUI debug readout in the Game view for seamless simulator testing.
    /// </summary>
    public class GazeReticle : MonoBehaviour
    {
        public static GazeReticle Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float reticleDistance = 1.8f;
        [SerializeField] private bool showOnGuiDebug = true;
        [SerializeField] private Color idleColor = new Color(1f, 1f, 1f, 0.35f);
        [SerializeField] private Color activeGazeColor = new Color(0.2f, 1f, 0.5f, 0.95f);
        [SerializeField] private Color completedColor = new Color(0.1f, 1f, 0.3f, 1f);

        private Canvas _canvas;
        private RectTransform _canvasRt;
        private Image _centerDot;
        private Image _progressRing;
        private TMP_Text _timerText;
        private LookTarget _activeTarget;

        private void Awake()
        {
            Instance = this;
            BuildUI();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void BuildUI()
        {
            if (_canvas != null) return;

            // Create local WorldSpace canvas in front of camera
            GameObject canvasGo = new GameObject("GazeReticle_Canvas");
            canvasGo.transform.SetParent(transform, false);
            canvasGo.transform.localPosition = new Vector3(0, 0, reticleDistance);
            canvasGo.transform.localRotation = Quaternion.identity;
            canvasGo.transform.localScale = Vector3.one * 0.0015f;

            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            _canvasRt = canvasGo.GetComponent<RectTransform>();
            _canvasRt.sizeDelta = new Vector2(200, 200);

            // 1. Center Dot
            GameObject dotGo = new GameObject("CenterDot");
            dotGo.transform.SetParent(canvasGo.transform, false);
            _centerDot = dotGo.AddComponent<Image>();
            _centerDot.color = idleColor;
            _centerDot.raycastTarget = false;
            RectTransform dotRt = dotGo.GetComponent<RectTransform>();
            dotRt.sizeDelta = new Vector2(10, 10);

            // 2. Outer Progress Ring (Radial360)
            GameObject ringGo = new GameObject("ProgressRing");
            ringGo.transform.SetParent(canvasGo.transform, false);
            _progressRing = ringGo.AddComponent<Image>();
            _progressRing.color = activeGazeColor;
            _progressRing.type = Image.Type.Filled;
            _progressRing.fillMethod = Image.FillMethod.Radial360;
            _progressRing.fillOrigin = (int)Image.Origin360.Top;
            _progressRing.fillAmount = 0f;
            _progressRing.raycastTarget = false;
            RectTransform ringRt = ringGo.GetComponent<RectTransform>();
            ringRt.sizeDelta = new Vector2(36, 36);

            // 3. Timer Text (e.g. "0.8s / 1.5s")
            GameObject textGo = new GameObject("TimerText");
            textGo.transform.SetParent(canvasGo.transform, false);
            _timerText = textGo.AddComponent<TextMeshProUGUI>();
            _timerText.fontSize = 24;
            _timerText.alignment = TextAlignmentOptions.Center;
            _timerText.color = Color.white;
            _timerText.raycastTarget = false;
            _timerText.text = "";
            RectTransform textRt = textGo.GetComponent<RectTransform>();
            textRt.anchoredPosition = new Vector2(0, -32);
            textRt.sizeDelta = new Vector2(200, 40);
        }

        private void LateUpdate()
        {
            // Find currently armed LookTarget in scene
            FindCurrentLookTarget();

            if (_activeTarget != null && _activeTarget.IsArmed && !_activeTarget.IsCompleted)
            {
                bool gazing = _activeTarget.IsCurrentlyGazed;
                float progress = _activeTarget.Progress;
                float currentSec = _activeTarget.CurrentSeconds;
                float dwellSec = _activeTarget.DwellSeconds;

                // Animate progress ring
                _progressRing.fillAmount = progress;

                if (gazing)
                {
                    _centerDot.color = activeGazeColor;
                    _centerDot.rectTransform.sizeDelta = new Vector2(14, 14);
                    _progressRing.color = activeGazeColor;
                    _timerText.text = $"{currentSec:F1}s / {dwellSec:F1}s";
                    _timerText.color = activeGazeColor;
                }
                else if (progress > 0.01f)
                {
                    _centerDot.color = new Color(1f, 0.8f, 0.2f, 0.7f);
                    _progressRing.color = new Color(1f, 0.8f, 0.2f, 0.5f);
                    _timerText.text = $"{currentSec:F1}s / {dwellSec:F1}s";
                    _timerText.color = new Color(1f, 0.8f, 0.2f, 0.8f);
                }
                else
                {
                    _centerDot.color = idleColor;
                    _centerDot.rectTransform.sizeDelta = new Vector2(8, 8);
                    _progressRing.fillAmount = 0f;
                    _timerText.text = "";
                }
            }
            else if (_activeTarget != null && _activeTarget.IsCompleted)
            {
                _centerDot.color = completedColor;
                _progressRing.fillAmount = 1f;
                _progressRing.color = completedColor;
                _timerText.text = "Target Identified!";
                _timerText.color = completedColor;
            }
            else
            {
                _centerDot.color = idleColor;
                _centerDot.rectTransform.sizeDelta = new Vector2(8, 8);
                _progressRing.fillAmount = 0f;
                _timerText.text = "";
            }
        }

        private void FindCurrentLookTarget()
        {
            // If current target is still armed and valid, keep it
            if (_activeTarget != null && _activeTarget.IsArmed && !_activeTarget.IsCompleted) return;

            // Otherwise scan for any armed LookTarget
            var all = Object.FindObjectsByType<LookTarget>();
            foreach (var lt in all)
            {
                if (lt.gameObject.activeInHierarchy && lt.IsArmed && !lt.IsCompleted)
                {
                    _activeTarget = lt;
                    return;
                }
            }
        }

        private void OnGUI()
        {
            if (!showOnGuiDebug || !Application.isPlaying) return;

            // Display on-screen diagnostic box for testing with XR Device Simulator
            GUI.color = Color.white;
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.alignment = TextAnchor.UpperLeft;
            boxStyle.fontSize = 13;
            boxStyle.normal.textColor = Color.white;

            GUILayout.BeginArea(new Rect(10, Screen.height - 130, 420, 120), boxStyle);
            GUILayout.Label("<b>--- GAZE SIMULATOR DIAGNOSTICS ---</b>");

            if (_activeTarget != null && _activeTarget.IsArmed)
            {
                string targetName = _activeTarget.gameObject.name;
                bool inZone = _activeTarget.IsInGateZone;
                float angle = _activeTarget.CurrentAngle;
                float cone = _activeTarget.ConeAngle;
                bool gazing = _activeTarget.IsCurrentlyGazed;
                float sec = _activeTarget.CurrentSeconds;
                float dwell = _activeTarget.DwellSeconds;
                float pct = _activeTarget.Progress * 100f;

                string zoneStr = inZone ? "<color=#00FF66>INSIDE ZONE (Active)</color>" : "<color=#FF4444>OUTSIDE ZONE (Step onto marker)</color>";
                GUILayout.Label($"Target: <b>{targetName}</b> | Stand Zone: {zoneStr}");

                string gazeStr = gazing ? "<color=#00FF66>LOCKED ON TARGET</color>" : $"<color=#FFAA00>Angle: {angle:F1}° (Cone: {cone:F1}°)</color>";
                GUILayout.Label($"Gaze Status: {gazeStr}");

                string bar = new string('=', Mathf.RoundToInt(pct / 5f)) + new string('-', 20 - Mathf.RoundToInt(pct / 5f));
                GUILayout.Label($"Hold Timer: <b>{sec:F1}s / {dwell:F1}s</b> [{bar}] {pct:F0}%");
            }
            else
            {
                GUILayout.Label("No LookTarget currently armed.");
                GUILayout.Label("Walk into the required trigger zone to arm the gaze target.");
            }

            GUILayout.EndArea();
        }
    }
}
