using UnityEngine;
using UnityEngine.XR;

namespace SkylineMission.Mechanics
{
    /// <summary>
    /// Simulates balance instability on narrow surfaces.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class BalanceSystem : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Multiplier for the wobble force")]
        [SerializeField] private float wobbleMultiplier = 0.15f;
        [Tooltip("Distance to raycast down to detect narrow surfaces")]
        [SerializeField] private float raycastDistance = 5f;
        [Tooltip("Smoothing factor for wobble velocity interpolation")]
        [SerializeField] private float smoothing = 8f;

        private CharacterController characterController;
        private Vector3 lastHeadPosition;
        private Vector3 currentWobbleVelocity;

        /// <summary>
        /// Controls the intensity of the balance instability (0-1).
        /// </summary>
        public float BalanceDifficulty { get; set; } = 0.5f;

        /// <summary>
        /// True when the player is standing on a NarrowPath or GlassBridge surface.
        /// </summary>
        public bool IsOnNarrowSurface { get; private set; }

        /// <summary>
        /// Computed movement dampening factor based on difficulty and surface.
        /// </summary>
        public float MovementDampeningFactor => Mathf.Lerp(1f, 0.3f, BalanceDifficulty * (IsOnNarrowSurface ? 1f : 0f));

        private void Awake()
        {
            characterController = GetComponentInParent<CharacterController>();
            if (characterController == null)
            {
                Debug.LogError("BalanceSystem requires a CharacterController in parent or self.");
            }
        }

        private void Update()
        {
            CheckSurface();
            HandleBalance();
        }

        private void CheckSurface()
        {
            IsOnNarrowSurface = false;
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, raycastDistance))
            {
                if (hit.collider.CompareTag("NarrowPath") || hit.collider.CompareTag("GlassBridge"))
                {
                    IsOnNarrowSurface = true;
                }
            }
        }

        private void HandleBalance()
        {
            InputDevice headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (headDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 headPosition))
            {
                if (lastHeadPosition != Vector3.zero && IsOnNarrowSurface)
                {
                    Vector3 headDelta = headPosition - lastHeadPosition;
                    Vector3 headVelocity = headDelta / Time.deltaTime;
                    Vector3 lateralVelocity = new Vector3(headVelocity.x, 0, headVelocity.z);

                    Vector3 targetWobble = lateralVelocity * BalanceDifficulty * wobbleMultiplier;
                    currentWobbleVelocity = Vector3.Lerp(currentWobbleVelocity, targetWobble, Time.deltaTime * smoothing);

                    if (characterController != null && currentWobbleVelocity.magnitude > 0.001f)
                    {
                        characterController.Move(currentWobbleVelocity * Time.deltaTime);
                    }
                }
                lastHeadPosition = headPosition;
            }
        }
    }
}
