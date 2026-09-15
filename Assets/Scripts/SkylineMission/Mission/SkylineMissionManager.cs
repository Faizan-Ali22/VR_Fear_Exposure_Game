using System.Collections;
using UnityEngine;
using SkylineMission.Mechanics;
using SkylineMission.UI;

namespace SkylineMission.Mission
{
    public enum MissionPhase
    {
        Inactive,
        Introduction,
        PlankTraversal,
        PlatformJumping,
        DroneRescue,
        GlassBridgeCrossing,
        Complete
    }

    public class SkylineMissionManager : MonoBehaviour
    {
        public static SkylineMissionManager Instance { get; private set; }

        [Header("System References")]
        [SerializeField] private BalanceSystem balanceSystem;
        [SerializeField] private FallDetector fallDetector;
        [SerializeField] private ScreenFader screenFader;
        
        [Header("Task References")]
        [SerializeField] private Tasks.PlankTraversal plankTask;
        [SerializeField] private Tasks.PlatformJumping platformTask;
        [SerializeField] private Tasks.DroneRescue droneTask;
        [SerializeField] private Tasks.GlassBridgeCrossing glassTask;

        public MissionPhase CurrentPhase { get; private set; }
        
        public int FallCount => fallDetector != null ? fallDetector.FallCount : 0;
        public float ElapsedTime { get; private set; }

        public event System.Action<MissionPhase> OnPhaseChanged;

        private static readonly string[] PhaseObjectives = new string[]
        {
            "", // Inactive
            "Walk carefully across the wooden plank",
            "Jump across the broken platforms",
            "Rescue the drone from the ledge",
            "Cross the glass bridge to the finish",
            "Mission Complete!"
        };

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            if (balanceSystem == null) Debug.LogError("SkylineMissionManager: BalanceSystem is null!");
            if (fallDetector == null) Debug.LogError("SkylineMissionManager: FallDetector is null!");
            if (screenFader == null) Debug.LogError("SkylineMissionManager: ScreenFader is null!");
            if (plankTask == null) Debug.LogError("SkylineMissionManager: PlankTraversal task is null!");
            if (platformTask == null) Debug.LogError("SkylineMissionManager: PlatformJumping task is null!");
            if (droneTask == null) Debug.LogError("SkylineMissionManager: DroneRescue task is null!");
            if (glassTask == null) Debug.LogError("SkylineMissionManager: GlassBridgeCrossing task is null!");
        }

        private void Start()
        {
            if (fallDetector != null)
            {
                fallDetector.OnPlayerFell += HandlePlayerFell;
            }

            if (plankTask != null) plankTask.OnTaskCompleted += CompleteCurrentPhase;
            if (platformTask != null) platformTask.OnTaskCompleted += CompleteCurrentPhase;
            if (droneTask != null) droneTask.OnTaskCompleted += CompleteCurrentPhase;
            if (glassTask != null) glassTask.OnTaskCompleted += CompleteCurrentPhase;

            CurrentPhase = MissionPhase.Inactive;
        }

        private void OnDestroy()
        {
            if (fallDetector != null)
            {
                fallDetector.OnPlayerFell -= HandlePlayerFell;
            }

            if (plankTask != null) plankTask.OnTaskCompleted -= CompleteCurrentPhase;
            if (platformTask != null) platformTask.OnTaskCompleted -= CompleteCurrentPhase;
            if (droneTask != null) droneTask.OnTaskCompleted -= CompleteCurrentPhase;
            if (glassTask != null) glassTask.OnTaskCompleted -= CompleteCurrentPhase;
        }

        private void Update()
        {
            if (CurrentPhase != MissionPhase.Inactive && CurrentPhase != MissionPhase.Complete)
            {
                ElapsedTime += Time.deltaTime;
            }
        }

        public void StartMission()
        {
            StartCoroutine(TransitionToPhase(MissionPhase.Introduction));
        }

        public void CompleteCurrentPhase()
        {
            DeactivateCurrentTask();
            MissionPhase nextPhase = GetNextPhase(CurrentPhase);
            StartCoroutine(TransitionToPhase(nextPhase));
        }

        public void SkipToPhase(MissionPhase phase)
        {
            DeactivateCurrentTask();
            StartCoroutine(TransitionToPhase(phase));
        }

        private void DeactivateCurrentTask()
        {
            switch (CurrentPhase)
            {
                case MissionPhase.PlankTraversal:
                    if (plankTask != null) plankTask.IsActive = false;
                    break;
                case MissionPhase.PlatformJumping:
                    if (platformTask != null) platformTask.IsActive = false;
                    break;
                case MissionPhase.DroneRescue:
                    if (droneTask != null) droneTask.IsActive = false;
                    break;
                case MissionPhase.GlassBridgeCrossing:
                    if (glassTask != null) glassTask.IsActive = false;
                    break;
            }
        }

        private MissionPhase GetNextPhase(MissionPhase current)
        {
            switch (current)
            {
                case MissionPhase.Inactive: return MissionPhase.Introduction;
                case MissionPhase.Introduction: return MissionPhase.PlankTraversal;
                case MissionPhase.PlankTraversal: return MissionPhase.PlatformJumping;
                case MissionPhase.PlatformJumping: return MissionPhase.DroneRescue;
                case MissionPhase.DroneRescue: return MissionPhase.GlassBridgeCrossing;
                case MissionPhase.GlassBridgeCrossing: return MissionPhase.Complete;
                default: return MissionPhase.Complete;
            }
        }

        private IEnumerator TransitionToPhase(MissionPhase phase)
        {
            if (screenFader != null)
            {
                yield return screenFader.FadeOut(0.5f);
            }

            ConfigurePhase(phase);

            if (screenFader != null)
            {
                yield return screenFader.FadeIn(0.5f);
            }

            if (phase == MissionPhase.Introduction)
            {
                yield return new WaitForSeconds(3f);
                CompleteCurrentPhase();
            }
        }

        private void ConfigurePhase(MissionPhase phase)
        {
            CurrentPhase = phase;
            OnPhaseChanged?.Invoke(phase);

            switch (phase)
            {
                case MissionPhase.Inactive:
                    if (WindForceSystem.Instance != null) WindForceSystem.Instance.WindIntensity = 0f;
                    if (balanceSystem != null) balanceSystem.BalanceDifficulty = 0f;
                    break;

                case MissionPhase.Introduction:
                    // Display objective maybe
                    break;

                case MissionPhase.PlankTraversal:
                    if (WindForceSystem.Instance != null) WindForceSystem.Instance.WindIntensity = 0.2f;
                    if (balanceSystem != null) balanceSystem.BalanceDifficulty = 0.4f;
                    if (plankTask != null) plankTask.IsActive = true;
                    if (fallDetector != null) fallDetector.SetActiveCheckpoint(0);
                    break;

                case MissionPhase.PlatformJumping:
                    if (WindForceSystem.Instance != null) WindForceSystem.Instance.WindIntensity = 0.4f;
                    if (balanceSystem != null) balanceSystem.BalanceDifficulty = 0.5f;
                    if (platformTask != null) platformTask.IsActive = true;
                    if (fallDetector != null) fallDetector.SetActiveCheckpoint(1);
                    break;

                case MissionPhase.DroneRescue:
                    if (WindForceSystem.Instance != null) WindForceSystem.Instance.WindIntensity = 0.5f;
                    if (balanceSystem != null) balanceSystem.BalanceDifficulty = 0.7f;
                    if (droneTask != null) droneTask.IsActive = true;
                    if (fallDetector != null) fallDetector.SetActiveCheckpoint(2);
                    break;

                case MissionPhase.GlassBridgeCrossing:
                    if (WindForceSystem.Instance != null) WindForceSystem.Instance.WindIntensity = 0.8f;
                    if (balanceSystem != null) balanceSystem.BalanceDifficulty = 0.6f;
                    if (glassTask != null) glassTask.IsActive = true;
                    if (fallDetector != null) fallDetector.SetActiveCheckpoint(3);
                    break;

                case MissionPhase.Complete:
                    if (WindForceSystem.Instance != null) WindForceSystem.Instance.WindIntensity = 0f;
                    break;
            }
        }

        private void HandlePlayerFell()
        {
            // Optional handling per task
        }
    }
}
