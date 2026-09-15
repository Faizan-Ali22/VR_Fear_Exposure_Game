using UnityEngine;
using SkylineMission.Mechanics;

namespace SkylineMission.Mission.Tasks
{
    public class PlankTraversal : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private Transform startTrigger;
        [SerializeField] private Transform endTrigger;
        [SerializeField] private PlatformPhysics plankPlatformPhysics;
        [SerializeField] private GameObject plankObject;
        [SerializeField] private float startRadius = 1.5f;
        [SerializeField] private float completionRadius = 1.5f;

        private bool isActive;
        public bool IsActive 
        { 
            get => isActive; 
            set 
            {
                isActive = value;
                if (!isActive)
                {
                    if (plankPlatformPhysics != null)
                    {
                        plankPlatformPhysics.ResetPlatform();
                    }
                }
            }
        }

        public bool IsComplete { get; private set; }

        public event System.Action OnTaskCompleted;

        private bool playerStartedTraversal = false;
        private Transform playerTransform;

        private void Awake()
        {
            if (startTrigger == null) Debug.LogError("PlankTraversal: StartTrigger is null!");
            if (endTrigger == null) Debug.LogError("PlankTraversal: EndTrigger is null!");
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
                Debug.LogError("PlankTraversal: Could not find object with 'Player' tag.");
            }
        }

        private void Update()
        {
            if (!IsActive || IsComplete || playerTransform == null) return;

            float distToStart = Vector3.Distance(playerTransform.position, startTrigger.position);
            if (!playerStartedTraversal && distToStart <= startRadius)
            {
                playerStartedTraversal = true;
            }

            if (playerStartedTraversal)
            {
                float distToEnd = Vector3.Distance(playerTransform.position, endTrigger.position);
                if (distToEnd <= completionRadius)
                {
                    IsComplete = true;
                    OnTaskCompleted?.Invoke();
                }
            }
        }
    }
}
