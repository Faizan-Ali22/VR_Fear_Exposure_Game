using System;
using UnityEngine;

namespace FearExposure.Level1
{
    /// <summary>
    /// Slides one or more door panels open and closed (room door, elevator doors).
    /// Panels sit in their CLOSED position in the scene. For each panel, Open Offsets says how far
    /// (in the panel's parent space) it moves when fully open.
    /// Example, two elevator panels on the local X axis: panel 0 = (-0.5, 0, 0), panel 1 = (0.5, 0, 0).
    /// </summary>
    public class SlidingDoor : MonoBehaviour
    {
        [SerializeField] private Transform[] panels;
        [Tooltip("One entry per panel: local-position offset of that panel when the door is fully open.")]
        [SerializeField] private Vector3[] openOffsets;
        [SerializeField] private float openSeconds = 1.6f;
        [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private bool startOpen;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip openClip;
        [SerializeField] private AudioClip closeClip;

        public event Action Opened;
        public event Action Closed;

        private Vector3[] _closedLocal;
        private float _t;
        private float _target;
        private bool _ready;

        public bool IsOpen { get { return Mathf.Approximately(_t, 1f); } }
        public bool IsClosed { get { return Mathf.Approximately(_t, 0f); } }

        private void Awake()
        {
            Init();
        }

        private void Init()
        {
            if (_ready) return;
            _ready = true;
            int n = panels != null ? panels.Length : 0;
            _closedLocal = new Vector3[n];
            for (int i = 0; i < n; i++)
                if (panels[i] != null) _closedLocal[i] = panels[i].localPosition;
            if (startOpen)
            {
                _t = 1f;
                _target = 1f;
                Apply();
            }
        }

        public void Open()
        {
            Init();
            if (_target >= 1f) return;
            _target = 1f;
            Play(openClip);
        }

        public void Close()
        {
            Init();
            if (_target <= 0f) return;
            _target = 0f;
            Play(closeClip);
        }

        /// <summary>Play Mode testing: right-click this component > "Test Open" / "Test Close" to check the offsets.</summary>
        [ContextMenu("Test Open")]
        private void TestOpen() { Open(); }

        [ContextMenu("Test Close")]
        private void TestClose() { Close(); }

        private void Play(AudioClip clip)
        {
            if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
        }

        private void Update()
        {
            if (Mathf.Approximately(_t, _target)) return;
            _t = Mathf.MoveTowards(_t, _target, Time.deltaTime / Mathf.Max(0.05f, openSeconds));
            Apply();
            if (Mathf.Approximately(_t, 1f)) { if (Opened != null) Opened(); }
            else if (Mathf.Approximately(_t, 0f)) { if (Closed != null) Closed(); }
        }

        private void Apply()
        {
            float k = curve.Evaluate(_t);
            int n = _closedLocal.Length;
            for (int i = 0; i < n; i++)
            {
                if (panels[i] == null) continue;
                Vector3 offset = (openOffsets != null && i < openOffsets.Length) ? openOffsets[i] : Vector3.zero;
                panels[i].localPosition = _closedLocal[i] + offset * k;
            }
        }

        public void Validate(ValidationReport report)
        {
            if (panels == null || panels.Length == 0) { report.Error("Panels is empty.", this); return; }
            for (int i = 0; i < panels.Length; i++)
                if (panels[i] == null) report.Error("Panels[" + i + "] is empty.", this);
            if (openOffsets == null || openOffsets.Length != panels.Length)
                report.Error("Open Offsets must have exactly one entry per panel (" + panels.Length + ").", this);
        }
    }
}
