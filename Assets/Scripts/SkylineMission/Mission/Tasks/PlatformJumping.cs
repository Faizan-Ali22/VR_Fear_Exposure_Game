using UnityEngine;
using SkylineMission.Mechanics;

namespace SkylineMission.Mission.Tasks
{
    public class PlatformJumping : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private PlatformPhysics[] platforms;
        [SerializeField] private Transform finalPlatformTrigger;
        [SerializeField] private float completionRadius = 2f;
        [SerializeField] private int[] timedPlatformIndices;

        private bool isActive;
        public bool IsActive 
        { 
            get => isActive; 
            set 
            {
                isActive = value;
                if (isActive)
                {
                    EnablePlatforms();
                }
                else
                {
                    DisablePlatforms();
                }
            }
        }

        public bool IsComplete { get; private set; }
        public int CurrentPlatformIndex { get; private set; }

        public event System.Action OnTaskCompleted;

        private Transform playerTransform;

        private void Awake()
        {
            if (platforms == null || platforms.Length == 0) Debug.LogError("PlatformJumping: Platforms array is empty!");
            if (finalPlatformTrigger == null) Debug.LogError("PlatformJumping: FinalPlatformTrigger is null!");
        }

        private void Start()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                Debug.LogError("PlatformJumping: Could not find object with 'Player' tag.");
            }
            CurrentPlatformIndex = -1;
        }

        private void Update()
        {
            if (!IsActive || IsComplete || playerTransform == null) return;

            UpdateCurrentPlatform();

            float distToGoal = Vector3.Distance(playerTransform.position, finalPlatformTrigger.position);
            if (distToGoal <= completionRadius)
            {
                IsComplete = true;
                OnTaskCompleted?.Invoke();
            }
        }

        private void UpdateCurrentPlatform()
        {
            for (int i = 0; i < platforms.Length; i++)
            {
                if (platforms[i] != null && platforms[i].PlayerIsOnPlatform)
                {
                    CurrentPlatformIndex = i;
                    break;
                }
            }
        }

        private void EnablePlatforms()
        {
            foreach (var platform in platforms)
            {
                if (platform != null)
                {
                    platform.enabled = true;
                    platform.ResetPlatform();
                }
            }
        }

        private void DisablePlatforms()
        {
            foreach (var platform in platforms)
            {
                if (platform != null)
                {
                    platform.ResetPlatform();
                    platform.enabled = false;
                }
            }
        }
    }
}
