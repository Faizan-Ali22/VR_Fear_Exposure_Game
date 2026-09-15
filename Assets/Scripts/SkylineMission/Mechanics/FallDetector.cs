using System.Collections;
using UnityEngine;
using SkylineMission.UI;

namespace SkylineMission.Mechanics
{
    /// <summary>
    /// Detects player falling and handles respawn logic.
    /// </summary>
    public class FallDetector : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Y position threshold for a fall")]
        [SerializeField] private float fallThresholdY = 90f;
        [Tooltip("List of checkpoints to respawn at")]
        [SerializeField] private Transform[] checkpoints;
        [Tooltip("Delay before completing the respawn")]
        [SerializeField] private float respawnDelay = 1.5f;
        [Tooltip("Reference to the screen fader utility")]
        [SerializeField] private ScreenFader screenFader;

        private CharacterController characterController;
        private int activeCheckpointIndex = 0;
        private bool isRespawning = false;

        /// <summary>
        /// Total number of times the player has fallen.
        /// </summary>
        public int FallCount { get; private set; }

        public event System.Action OnPlayerFell;
        public event System.Action OnPlayerRespawned;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            if (characterController == null)
            {
                characterController = FindFirstObjectByType<CharacterController>();
                if (characterController == null)
                {
                    Debug.LogError("FallDetector requires a CharacterController on the XR Origin.");
                }
            }
        }

        private void Update()
        {
            if (!isRespawning && transform.position.y < fallThresholdY)
            {
                StartCoroutine(RespawnCoroutine());
            }
        }

        /// <summary>
        /// Sets the active checkpoint using an index.
        /// </summary>
        public void SetActiveCheckpoint(int index)
        {
            if (index >= 0 && checkpoints != null && index < checkpoints.Length)
            {
                activeCheckpointIndex = index;
            }
        }

        /// <summary>
        /// Sets the active checkpoint using a transform reference.
        /// </summary>
        public void SetActiveCheckpoint(Transform point)
        {
            if (checkpoints == null) return;
            for (int i = 0; i < checkpoints.Length; i++)
            {
                if (checkpoints[i] == point)
                {
                    activeCheckpointIndex = i;
                    break;
                }
            }
        }

        private IEnumerator RespawnCoroutine()
        {
            isRespawning = true;
            FallCount++;
            OnPlayerFell?.Invoke();

            if (screenFader != null)
            {
                yield return screenFader.FadeOut(0.3f);
            }
            else
            {
                yield return new WaitForSeconds(0.3f);
            }

            if (characterController != null)
            {
                characterController.enabled = false;
            }

            if (checkpoints != null && checkpoints.Length > 0 && activeCheckpointIndex < checkpoints.Length)
            {
                transform.position = checkpoints[activeCheckpointIndex].position;
            }
            
            yield return new WaitForSeconds(respawnDelay);

            if (characterController != null)
            {
                characterController.enabled = true;
            }

            if (screenFader != null)
            {
                screenFader.FadeIn(0.5f);
            }

            OnPlayerRespawned?.Invoke();
            isRespawning = false;
        }
    }
}
