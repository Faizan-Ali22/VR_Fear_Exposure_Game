using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FearExposure.Level1
{
    /// <summary>
    /// Runs the whole level. Starts the steps one after another, in the order of the Steps list.
    /// Put exactly ONE of these on the [Level1] root object.
    ///
    /// Dev keys (Editor and Development Builds only): F6 = skip the current step.
    /// Right-click this component > "Validate Setup" to check every reference.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    public class Level1Director : MonoBehaviour
    {
        public static Level1Director Current { get; private set; }

        [Header("Required references")]
        [SerializeField] private PlayerRig playerRig;
        [SerializeField] private InstructionPanel instructions;
        [SerializeField] private CourageScore courage;
        [SerializeField] private FadeOverlay fade;

        [Header("Optional references")]
        [SerializeField] private AmbienceController ambience;
        [SerializeField] private CompletionScreen completionScreen;
        [Tooltip("Where the player starts (position + facing). Leave empty to start wherever the XR Origin is.")]
        [SerializeField] private Transform spawnPoint;

        [Header("Flow: the order of this list is the order of the level")]
        [SerializeField] private List<LevelStep> steps = new List<LevelStep>();
        [SerializeField] private bool autoStart = true;
        [SerializeField] private float startDelay = 2f;
        [SerializeField] private float pauseBetweenSteps = 1.5f;
        [SerializeField] private bool hideStepObjectsWhenFinished = true;
        [SerializeField] private bool validateOnStart = true;

        [Header("Events")]
        public UnityEvent onLevelStarted;
        public UnityEvent<int> onStepStarted;
        public UnityEvent onLevelCompleted;

        public PlayerRig Player { get { return playerRig; } }
        public InstructionPanel Instructions { get { return instructions; } }
        public CourageScore Courage { get { return courage; } }
        public FadeOverlay Fade { get { return fade; } }
        public int CurrentIndex { get; private set; }
        public LevelStep CurrentStep { get; private set; }
        public bool LevelComplete { get; private set; }

        private bool _started;
        private bool _setupOk;

        private void Awake()
        {
            Current = this;
            CurrentIndex = -1;
        }

        private void OnDestroy()
        {
            if (courage != null && instructions != null)
                courage.PointsAwarded -= instructions.ShowCourageToast;
            if (Current == this) Current = null;
        }

        private IEnumerator Start()
        {
            if (validateOnStart)
            {
                ValidationReport report = BuildReport();
                PrintReport(report);
                if (report.Errors.Count > 0)
                {
                    Debug.LogError("[Level1] Setup incomplete. Fix the errors above and press Play again. The level will not start.", this);
                    yield break;
                }
            }
            _setupOk = true;

            for (int i = 0; i < steps.Count; i++)
            {
                if (steps[i] != null) steps[i].gameObject.SetActive(false);
            }

            if (courage != null && instructions != null)
                courage.PointsAwarded += instructions.ShowCourageToast;

            if (fade != null) fade.SetAlpha(1f);

            // Give the headset a moment to report its real position before the player is placed.
            yield return new WaitForSeconds(0.6f);
            if (spawnPoint != null && playerRig != null)
                playerRig.PlaceFeetAt(spawnPoint.position, spawnPoint.eulerAngles.y);

            if (onLevelStarted != null) onLevelStarted.Invoke();

            if (autoStart) BeginLevel();
            else if (fade != null) fade.SetAlpha(0f);
        }

        /// <summary>Starts the level from step 0. Called automatically when Auto Start is ticked.</summary>
        public void BeginLevel()
        {
            if (_started || !_setupOk) return;
            _started = true;
            StartCoroutine(IntroRoutine());
        }

        private IEnumerator IntroRoutine()
        {
            yield return new WaitForSeconds(startDelay);
            if (fade != null) yield return fade.StartFade(0f, 1.5f);
            BeginStep(0);
        }

        private void BeginStep(int index)
        {
            if (index >= steps.Count)
            {
                CompleteLevel();
                return;
            }

            LevelStep step = steps[index];
            if (step == null)
            {
                Debug.LogError("[Level1] Steps[" + index + "] is empty - skipping it.", this);
                BeginStep(index + 1);
                return;
            }

            CurrentIndex = index;
            CurrentStep = step;
            step.gameObject.SetActive(true);
            step.Finished += HandleStepFinished;
            if (ambience != null) ambience.Apply(step.AmbiencePreset);
            if (onStepStarted != null) onStepStarted.Invoke(index);
            Debug.Log("[Level1] Step " + (index + 1) + "/" + steps.Count + " started: " + step.StepTitle);
            step.Begin(this);
        }

        private void HandleStepFinished(LevelStep step)
        {
            step.Finished -= HandleStepFinished;
            if (hideStepObjectsWhenFinished) step.gameObject.SetActive(false);
            if (CurrentStep == step) CurrentStep = null;
            // The Director runs the pause, so it keeps working even though the step object is now off.
            StartCoroutine(NextStepAfterPause());
        }

        private IEnumerator NextStepAfterPause()
        {
            yield return new WaitForSeconds(pauseBetweenSteps);
            BeginStep(CurrentIndex + 1);
        }

        private void CompleteLevel()
        {
            LevelComplete = true;
            bool thresholdMet = courage == null || courage.ThresholdMet;
            if (courage != null) LevelProgress.RecordLevel1(courage.Total);
            if (thresholdMet) LevelProgress.UnlockLevel(2);
            if (instructions != null) instructions.Hide();
            if (completionScreen != null) completionScreen.Show(courage);
            Debug.Log("[Level1] Level complete. Courage Points: " + (courage != null ? courage.Total : 0) + (thresholdMet ? " (Level 2 unlocked)" : " (below threshold)"));
            if (onLevelCompleted != null) onLevelCompleted.Invoke();
        }

        /// <summary>Dev tool: finish the running step immediately.</summary>
        [ContextMenu("Skip Current Step (Play Mode)")]
        public void SkipCurrentStep()
        {
            if (CurrentStep != null) CurrentStep.Skip();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f6Key.wasPressedThisFrame) SkipCurrentStep();
#endif
        }
#endif

        // ---------------------------------------------------------------- validation

        public ValidationReport BuildReport()
        {
            ValidationReport report = new ValidationReport();
            report.Require(playerRig, "Player Rig", this);
            report.Require(instructions, "Instructions", this);
            report.Require(courage, "Courage", this);
            report.Require(fade, "Fade", this);

            if (playerRig != null) playerRig.Validate(report);
            if (instructions != null) instructions.Validate(report);
            if (fade != null) fade.Validate(report);
            if (completionScreen != null) completionScreen.Validate(report);
            else report.Warn("No Completion Screen assigned - the level will end without a results screen.", this);

            if (steps.Count == 0) report.Error("The Steps list is empty.", this);
            for (int i = 0; i < steps.Count; i++)
            {
                if (steps[i] == null)
                {
                    report.Error("Steps[" + i + "] is empty.", this);
                    continue;
                }
                for (int j = 0; j < i; j++)
                {
                    if (steps[j] == steps[i]) report.Error("Steps[" + i + "] is a duplicate of Steps[" + j + "].", this);
                }
                steps[i].Validate(report);
            }
            return report;
        }

        private void PrintReport(ValidationReport report)
        {
            for (int i = 0; i < report.Errors.Count; i++) Debug.LogError("[Level1 setup] " + report.Errors[i], this);
            for (int i = 0; i < report.Warnings.Count; i++) Debug.LogWarning("[Level1 setup] " + report.Warnings[i], this);
            if (report.Errors.Count == 0 && report.Warnings.Count == 0)
                Debug.Log("[Level1 setup] All checks passed.", this);
            else if (report.Errors.Count == 0)
                Debug.Log("[Level1 setup] No errors (" + report.Warnings.Count + " warning(s)).", this);
        }

        [ContextMenu("Validate Setup")]
        private void ValidateFromMenu()
        {
            PrintReport(BuildReport());
        }
    }
}
