using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace FearExposure.Level1
{
    /// <summary>
    /// A pressable 3D button (elevator call button, floor buttons, duration buttons).
    /// Built on XR Simple Interactable: point a controller ray at it (or reach out with a hand)
    /// and SQUEEZE THE GRIP (XR Device Simulator: hold G). The object also needs a Collider.
    /// Steps switch buttons on and off with SetInteractable().
    /// </summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class WorldButton : MonoBehaviour
    {
        [Header("Visual (optional)")]
        [Tooltip("Mesh whose colour changes. Its material is copied, so the original asset is untouched.")]
        [SerializeField] private Renderer visual;
        [SerializeField] private Color idleColor = new Color(0.25f, 0.85f, 0.45f, 1f);
        [SerializeField] private Color hoverColor = new Color(0.7f, 1f, 0.8f, 1f);
        [SerializeField] private Color pressedColor = Color.white;
        [SerializeField] private Color disabledColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        [Tooltip("Optional part that moves inward when pressed.")]
        [SerializeField] private Transform pressTarget;
        [SerializeField] private Vector3 pressOffset = new Vector3(0f, 0f, 0.008f);

        [Header("Behaviour")]
        [Tooltip("Leave OFF: a step switches the button on at the right moment.")]
        [SerializeField] private bool startInteractable = false;
        [SerializeField] private float cooldownSeconds = 0.6f;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip pressClip;

        public UnityEvent onPressed;
        public event Action Pressed;

        public bool IsInteractable { get; private set; }

        private XRSimpleInteractable _interactable;
        private bool _stateChosen;
        private Material _material;
        private bool _hovered;
        private bool _attention;
        private bool _wasPressed;
        private float _lockUntil;
        private float _pressVisualUntil;
        private Vector3 _restLocalPos;

        private void Awake()
        {
            _interactable = GetComponent<XRSimpleInteractable>();
            _interactable.selectEntered.AddListener(args => HandleSelect());
            _interactable.hoverEntered.AddListener(args => SetHover(true));
            _interactable.hoverExited.AddListener(args => SetHover(false));

            if (visual != null)
            {
                _material = MaterialUtil.Clone(visual.sharedMaterial);
                if (_material != null) visual.sharedMaterial = _material;
            }
            if (pressTarget != null) _restLocalPos = pressTarget.localPosition;
            // If a step already switched this button on/off before Awake ran, keep that choice.
            SetInteractable(_stateChosen ? IsInteractable : startInteractable);
        }

        private void OnDestroy()
        {
            if (_material != null) Destroy(_material);
        }

        /// <summary>Switch the button on (pressable, coloured) or off (greyed out, ignored).</summary>
        public void SetInteractable(bool on)
        {
            _stateChosen = true;
            IsInteractable = on;
            if (_interactable != null) _interactable.enabled = on;
            if (!on)
            {
                _hovered = false;
                _attention = false;
            }
            RefreshVisual();
        }

        /// <summary>Makes the button pulse gently so the player notices it.</summary>
        public void SetAttention(bool on)
        {
            _attention = on;
            if (!on) RefreshVisual();
        }

        /// <summary>Play Mode testing: right-click this component in the Inspector > "Press (test)".</summary>
        [ContextMenu("Press (test)")]
        private void PressForTest()
        {
            HandleSelect();
        }

        private void SetHover(bool hovered)
        {
            _hovered = hovered;
            RefreshVisual();
        }

        private void HandleSelect()
        {
            if (!IsInteractable || Time.time < _lockUntil) return;
            _lockUntil = Time.time + cooldownSeconds;
            _pressVisualUntil = Time.time + 0.2f;

            if (audioSource != null && pressClip != null) audioSource.PlayOneShot(pressClip);
            if (PlayerRig.Instance != null) PlayerRig.Instance.Pulse(0.25f, 0.08f);

            if (Pressed != null) Pressed();
            if (onPressed != null) onPressed.Invoke();
        }

        private void Update()
        {
            bool pressedNow = _pressVisualUntil > Time.time;
            if (pressedNow != _wasPressed)
            {
                _wasPressed = pressedNow;
                if (pressTarget != null)
                    pressTarget.localPosition = pressedNow ? _restLocalPos + pressOffset : _restLocalPos;
                RefreshVisual();
            }

            if (_attention && IsInteractable && !pressedNow && !_hovered && _material != null)
            {
                float k = 0.5f + 0.5f * Mathf.Sin(Time.time * 4f);
                MaterialUtil.SetColor(_material, Color.Lerp(idleColor, hoverColor, k));
            }
        }

        private void RefreshVisual()
        {
            if (_material == null) return;
            Color c;
            if (!IsInteractable) c = disabledColor;
            else if (_pressVisualUntil > Time.time) c = pressedColor;
            else if (_hovered) c = hoverColor;
            else c = idleColor;
            MaterialUtil.SetColor(_material, c);
        }

        public void Validate(ValidationReport report)
        {
            if (GetComponentInChildren<Collider>(true) == null)
                report.Error("Has no Collider. Add a Box Collider so the controller can hit the button.", this);
        }
    }
}
