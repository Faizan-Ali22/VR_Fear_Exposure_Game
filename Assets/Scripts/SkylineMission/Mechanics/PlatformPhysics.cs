using System.Collections;
using UnityEngine;

namespace SkylineMission.Mechanics
{
    /// <summary>
    /// Adds unstable physics to individual platform GameObjects.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class PlatformPhysics : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Amplitude of the wobble in degrees")]
        [SerializeField] private float wobbleAmplitude = 1f;
        [Tooltip("Speed of the wobble")]
        [SerializeField] private float wobbleSpeed = 2f;
        [Tooltip("Can this platform crumble after a delay?")]
        [SerializeField] private bool canCrumble = false;
        [Tooltip("Seconds player must stand before crumbling")]
        [SerializeField] private float crumbleDelay = 4f;
        [Tooltip("Does the platform respawn after falling?")]
        [SerializeField] private bool respawnAfterCrumble = true;
        [Tooltip("Delay before platform respawns")]
        [SerializeField] private float respawnDelay = 3f;
        [Tooltip("Enable wobble effect?")]
        [SerializeField] private bool enableWobble = true;

        /// <summary>
        /// True if the player is currently on this platform.
        /// </summary>
        public bool PlayerIsOnPlatform { get; private set; }

        /// <summary>
        /// Total time the player has been standing on this platform.
        /// </summary>
        public float TimePlayerOnPlatform { get; private set; }

        public event System.Action<PlatformPhysics> OnPlatformCrumbled;

        private Quaternion originalRotation;
        private Vector3 originalPosition;
        private Vector3 originalScale;
        private Rigidbody rb;
        private BoxCollider boxCollider;
        private bool isCrumbled = false;

        private void Awake()
        {
            boxCollider = GetComponent<BoxCollider>();
            boxCollider.isTrigger = true;
            originalRotation = transform.rotation;
            originalPosition = transform.position;
            originalScale = transform.localScale;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && !isCrumbled)
            {
                PlayerIsOnPlatform = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") && !isCrumbled)
            {
                PlayerIsOnPlatform = false;
                TimePlayerOnPlatform = 0f;
                transform.rotation = originalRotation;
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.CompareTag("Player") && !isCrumbled)
            {
                TimePlayerOnPlatform += Time.deltaTime;
            }
        }

        private void Update()
        {
            if (isCrumbled) return;

            if (PlayerIsOnPlatform && enableWobble)
            {
                Vector3 wobbleOffset = new Vector3(
                    Mathf.Sin(Time.time * wobbleSpeed) * wobbleAmplitude, 
                    0, 
                    Mathf.Cos(Time.time * wobbleSpeed * 0.7f) * wobbleAmplitude * 0.5f);
                transform.rotation = originalRotation * Quaternion.Euler(wobbleOffset);
            }

            if (canCrumble && PlayerIsOnPlatform && TimePlayerOnPlatform >= crumbleDelay)
            {
                StartCoroutine(CrumbleCoroutine());
            }
        }

        private IEnumerator CrumbleCoroutine()
        {
            isCrumbled = true;
            PlayerIsOnPlatform = false;
            
            float shakeDuration = 0.5f;
            float elapsed = 0f;
            while (elapsed < shakeDuration)
            {
                elapsed += Time.deltaTime;
                transform.rotation = originalRotation * Quaternion.Euler(Random.Range(-5f, 5f), Random.Range(-5f, 5f), Random.Range(-5f, 5f));
                yield return null;
            }

            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = false;
            OnPlatformCrumbled?.Invoke(this);

            if (respawnAfterCrumble)
            {
                yield return new WaitForSeconds(respawnDelay);
                ResetPlatform();
            }
        }

        /// <summary>
        /// Resets the platform to its original state.
        /// </summary>
        public void ResetPlatform()
        {
            isCrumbled = false;
            TimePlayerOnPlatform = 0f;
            PlayerIsOnPlatform = false;

            if (rb != null)
            {
                Destroy(rb);
            }

            transform.position = originalPosition;
            transform.rotation = originalRotation;
            transform.localScale = originalScale;
            
            if (boxCollider != null)
            {
                boxCollider.enabled = true;
            }
        }
    }
}
