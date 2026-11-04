using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Filtering;

namespace FearExposure.Level1
{
    /// <summary>
    /// A pressable 3D button (elevator call button, floor buttons, duration buttons).
    /// Supports multi-modal activation:
    ///   1. Poking (XR Poke Interactor / XRPokeFilter as in Lab Manual 9).
    ///   2. Direct physical hand slamming or touching (triggers, collisions, proximity).
    ///   3. Grabbing / Select (Grip button / G key).
    ///   4. Point & Click / Activate (Trigger button / Left Click in simulator).
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

        [Header("Touch & Proximity Settings")]
        [Tooltip("Distance in metres from hand to button centre to trigger a physical press.")]
        [SerializeField] private float touchRadius = 0.22f;

        public UnityEvent onPressed;
        public event Action Pressed;

        public bool IsInteractable { get; private set; }

        private XRSimpleInteractable _interactable;
        private XRPokeFilter _pokeFilter;
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
            if (_interactable != null)
            {
                // Multi-modal listeners: both Select (grip/poke) and Activate (trigger/click)
                _interactable.selectEntered.AddListener(args => HandleSelect());
                _interactable.activated.AddListener(args => HandleSelect());
                _interactable.hoverEntered.AddListener(args => SetHover(true));
                _interactable.hoverExited.AddListener(args => SetHover(false));

                // Auto-link InteractionManager if unassigned
                if (_interactable.interactionManager == null)
                {
                    var mgr = FindAnyObjectByType<XRInteractionManager>();
                    if (mgr != null) _interactable.interactionManager = mgr;
                }

                // Ensure colliders are registered
                var col = GetComponent<Collider>();
                if (col != null && !_interactable.colliders.Contains(col))
                {
                    _interactable.colliders.Add(col);
                }
            }

            // Auto-configure XRPokeFilter for Lab Manual 9 Poke Interactors
            _pokeFilter = GetComponent<XRPokeFilter>();
            if (_pokeFilter == null)
            {
                _pokeFilter = gameObject.AddComponent<XRPokeFilter>();
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

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
            if (_pokeFilter != null) _pokeFilter.enabled = on;
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
        public void PressForTest()
        {
            HandleSelect();
        }

        private void SetHover(bool hovered)
        {
            _hovered = hovered;
            RefreshVisual();
        }

        public void HandleSelect()
        {
            if (!IsInteractable || Time.time < _lockUntil) return;
            _lockUntil = Time.time + cooldownSeconds;
            _pressVisualUntil = Time.time + 0.25f;

            if (audioSource != null && pressClip != null)
            {
                audioSource.PlayOneShot(pressClip);
            }
            if (PlayerRig.Instance != null)
            {
                PlayerRig.Instance.Pulse(0.35f, 0.12f);
            }

            if (Pressed != null) Pressed();
            if (onPressed != null) onPressed.Invoke();
        }

        // Direct physical collision / trigger detection (hand slamming / body collision)
        private void OnTriggerEnter(Collider other)
        {
            TryHandlePhysicalContact(other.gameObject);
        }

        private void OnCollisionEnter(Collision collision)
        {
            TryHandlePhysicalContact(collision.gameObject);
        }

        private void TryHandlePhysicalContact(GameObject hitObj)
        {
            if (!IsInteractable || Time.time < _lockUntil) return;
            if (hitObj == null) return;

            // Check if collider belongs to player rig or hands
            if (hitObj.GetComponentInParent<PlayerRig>() != null ||
                hitObj.GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor>() != null ||
                hitObj.name.Contains("Hand") || hitObj.name.Contains("Controller") || hitObj.name.Contains("Poke"))
            {
                HandleSelect();
            }
        }

        private void Update()
        {
            // Continuous proximity touch detection for hand slamming in VR or simulator
            if (IsInteractable && Time.time >= _lockUntil)
            {
                PlayerRig rig = PlayerRig.Instance;
                if (rig != null && rig.IsWithinReach(transform.position, 0.35f, touchRadius))
                {
                    HandleSelect();
                }
            }

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
