using UnityEngine;

namespace SkylineMission.Mechanics
{
    /// <summary>
    /// Applies dynamic wind forces to the player.
    /// </summary>
    public class WindForceSystem : MonoBehaviour
    {
        public static WindForceSystem Instance { get; private set; }

        [Header("Settings")]
        [Tooltip("Maximum wind force applied")]
        [SerializeField] private float maxWindForce = 0.8f;
        [Tooltip("Speed at which the wind direction changes")]
        [SerializeField] private float directionChangeSpeed = 0.1f;
        [Tooltip("Frequency of random gusts")]
        [SerializeField] private float gustFrequency = 0.3f;
        [Tooltip("Cooldown between automatic gusts")]
        [SerializeField] private float gustCooldown = 5f;
        [Tooltip("Audio source for wind sound")]
        [SerializeField] private AudioSource windAudioSource;

        private CharacterController characterController;
        private float gustStrengthExtra;

        /// <summary>
        /// Master intensity of the wind (0-1).
        /// </summary>
        public float WindIntensity { get; set; } = 0.5f;

        /// <summary>
        /// Current normalized wind direction.
        /// </summary>
        public Vector3 CurrentWindDirection { get; private set; }

        /// <summary>
        /// Actual force magnitude applied this frame.
        /// </summary>
        public float CurrentWindStrength { get; private set; }

        public event System.Action<Vector3, float> OnGust;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            characterController = FindFirstObjectByType<CharacterController>();
            if (characterController == null)
            {
                Debug.LogError("WindForceSystem could not find a CharacterController.");
            }
        }

        private void FixedUpdate()
        {
            UpdateWind();
            ApplyWind();
        }

        private void UpdateWind()
        {
            float angle = Mathf.PerlinNoise(Time.time * directionChangeSpeed, 0) * 360f;
            CurrentWindDirection = Quaternion.Euler(0, angle, 0) * Vector3.forward;

            float baseStrength = Mathf.PerlinNoise(Time.time * gustFrequency, 100f);
            CurrentWindStrength = (baseStrength * maxWindForce + gustStrengthExtra) * WindIntensity;

            if (windAudioSource != null)
            {
                windAudioSource.volume = (CurrentWindStrength / maxWindForce) * WindIntensity;
            }
        }

        private void ApplyWind()
        {
            if (characterController != null && characterController.enabled)
            {
                Vector3 windForce = CurrentWindDirection * CurrentWindStrength;
                characterController.Move(windForce * Time.fixedDeltaTime);
            }
        }

        /// <summary>
        /// Triggers a temporary gust of wind.
        /// </summary>
        public void TriggerGust(float strength, float duration)
        {
            StartCoroutine(GustCoroutine(strength, duration));
            OnGust?.Invoke(CurrentWindDirection, strength);
        }

        private System.Collections.IEnumerator GustCoroutine(float strength, float duration)
        {
            float elapsed = 0f;
            float halfDuration = duration / 2f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                if (elapsed < halfDuration)
                {
                    gustStrengthExtra = Mathf.Lerp(0, strength, elapsed / halfDuration);
                }
                else
                {
                    gustStrengthExtra = Mathf.Lerp(strength, 0, (elapsed - halfDuration) / halfDuration);
                }
                yield return null;
            }
            gustStrengthExtra = 0f;
        }
    }
}
