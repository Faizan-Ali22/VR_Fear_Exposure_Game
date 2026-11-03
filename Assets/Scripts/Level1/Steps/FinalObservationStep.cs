using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FearExposure.Level1
{
    /// <summary>
    /// STEP 7 - Final Challenge and Completion (8-10 min).
    /// At the viewing point the player CHOOSES how long to look down at the city (short / medium / long).
    /// The timer only runs while they look down and outward; looking up simply pauses it.
    /// A "finish early" button is always available. The Director shows the results screen afterwards.
    /// </summary>
    public class FinalObservationStep : LevelStep
    {
        [Header("Scene references")]
        [Tooltip("Zone at the railing / viewing point.")]
        [SerializeField] private ZoneVolume viewingZone;
        [Tooltip("Empty object at the railing whose blue arrow points OUT over the city.")]
        [SerializeField] private Transform viewDirection;
        [Tooltip("Parent object of the duration buttons (starts hidden).")]
        [SerializeField] private GameObject durationPicker;
        [Tooltip("One WorldButton per choice, same order as the two lists below.")]
        [SerializeField] private WorldButton[] durationButtons;
        [Tooltip("Always-available 'I'm done' button, shown while the timer runs.")]
        [SerializeField] private WorldButton finishEarlyButton;
        [Tooltip("World canvas with the progress ring (starts hidden).")]
        [SerializeField] private GameObject progressRoot;
        [Tooltip("Image with Image Type = Filled, Fill Method = Radial 360.")]
        [SerializeField] private Image progressFill;
        [SerializeField] private TMP_Text progressLabel;

        [Header("Choices")]
        [SerializeField] private float[] durationSeconds = { 5f, 10f, 20f };
        [SerializeField] private int[] courageRewards = { 15, 25, 40 };

        [Header("Look-down rule")]
        [Tooltip("The head must tilt down at least this many degrees.")]
        [SerializeField] private float minPitchDown = 20f;
        [Tooltip("1 = looking exactly outward, 0 = sideways. The head must face outward at least this much.")]
        [SerializeField, Range(-1f, 1f)] private float minOutwardDot = 0.2f;

        [Header("Messages")]
        [SerializeField, TextArea] private string walkMessage = "For the final challenge, walk to the viewing point at the glass railing.";
        [SerializeField, TextArea] private string chooseMessage = "Choose how long you would like to look down at the city. You can look up to rest at any time - the timer simply pauses.";
        [SerializeField, TextArea] private string lookMessage = "Look down at the city below. Breathe slowly.";
        [SerializeField, TextArea] private string doneMessage = "You did it. Final observation complete.";
        [SerializeField, TextArea] private string earlyMessage = "That is completely fine. You can try a longer look next time.";

        private System.Action[] _handlers;
        private bool _atPoint;
        private bool _counting;
        private bool _completing;
        private bool _earlySubscribed;
        private int _chosen;
        private float _target;
        private float _held;
        private int _lastShownSecond;
        private bool _lastInPosition;

        protected override void OnBegin()
        {
            _atPoint = false;
            _counting = false;
            _completing = false;
            _chosen = -1;
            _held = 0f;
            _handlers = new System.Action[durationButtons.Length];

            durationPicker.SetActive(false);
            progressRoot.SetActive(false);
            if (finishEarlyButton != null) finishEarlyButton.gameObject.SetActive(false);

            Say(walkMessage);
            viewingZone.Entered += OnAtViewingPoint;
        }

        private void OnAtViewingPoint()
        {
            if (_atPoint || !IsRunning) return;
            _atPoint = true;

            durationPicker.SetActive(true);
            for (int i = 0; i < durationButtons.Length; i++)
            {
                int index = i;
                _handlers[i] = () => ChooseDuration(index);
                durationButtons[i].SetInteractable(true);
                durationButtons[i].Pressed += _handlers[i];
            }
            Say(chooseMessage);
        }

        private void ChooseDuration(int index)
        {
            if (_counting || _completing || index < 0 || index >= durationSeconds.Length) return;

            _chosen = index;
            _target = Mathf.Max(1f, durationSeconds[index]);
            _held = 0f;
            _lastShownSecond = -1;
            _lastInPosition = true;
            _counting = true;

            durationPicker.SetActive(false);
            progressRoot.SetActive(true);
            if (finishEarlyButton != null)
            {
                finishEarlyButton.gameObject.SetActive(true);
                finishEarlyButton.SetInteractable(true);
                if (!_earlySubscribed)
                {
                    _earlySubscribed = true;
                    finishEarlyButton.Pressed += OnFinishEarly;
                }
            }
            Say(lookMessage);
            UpdateProgressUi(true);
        }

        private void Update()
        {
            if (!IsRunning || !_counting || _completing) return;
            PlayerRig rig = PlayerRig.Instance;
            if (rig == null || rig.Head == null) return;

            bool inPosition = IsInPosition(rig);
            if (inPosition) _held += Time.deltaTime;
            UpdateProgressUi(inPosition);

            if (_held >= _target) CompleteFull();
        }

        private bool IsInPosition(PlayerRig rig)
        {
            if (!viewingZone.PlayerInside) return false;
            if (rig.HeadPitchDown < minPitchDown) return false;
            if (viewDirection != null)
            {
                Vector3 facing = rig.Head.forward;
                facing.y = 0f;
                Vector3 outward = viewDirection.forward;
                outward.y = 0f;
                if (facing.sqrMagnitude > 0.0001f && outward.sqrMagnitude > 0.0001f)
                {
                    if (Vector3.Dot(facing.normalized, outward.normalized) < minOutwardDot) return false;
                }
            }
            return true;
        }

        private void UpdateProgressUi(bool inPosition)
        {
            float p = Mathf.Clamp01(_held / _target);
            if (progressFill != null) progressFill.fillAmount = p;

            int secondsLeft = Mathf.CeilToInt(_target - _held);
            if (progressLabel != null && (secondsLeft != _lastShownSecond || inPosition != _lastInPosition))
            {
                _lastShownSecond = secondsLeft;
                _lastInPosition = inPosition;
                progressLabel.text = inPosition ? secondsLeft + "s" : "Look down at the city to continue";
            }
        }

        private void CompleteFull()
        {
            if (_completing) return;
            _completing = true;
            Award(courageRewards[_chosen], "Final observation (" + Mathf.RoundToInt(_target) + " seconds)");
            if (PlayerRig.Instance != null) PlayerRig.Instance.Pulse(0.4f, 0.2f);
            Say(doneMessage, 6f);
            StartCoroutine(FinishAfter(3f));
        }

        private void OnFinishEarly()
        {
            if (!_counting || _completing) return;
            _completing = true;
            float p = Mathf.Clamp01(_held / _target);
            int points = Mathf.RoundToInt(courageRewards[_chosen] * p);
            if (points > 0) Award(points, "Final observation (finished early)");
            Say(earlyMessage, 6f);
            StartCoroutine(FinishAfter(2f));
        }

        private IEnumerator FinishAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            Finish();
        }

        protected override void OnFinished()
        {
            if (viewingZone != null) viewingZone.Entered -= OnAtViewingPoint;
            if (_handlers != null && durationButtons != null)
            {
                for (int i = 0; i < durationButtons.Length && i < _handlers.Length; i++)
                {
                    if (durationButtons[i] == null) continue;
                    if (_handlers[i] != null) durationButtons[i].Pressed -= _handlers[i];
                    durationButtons[i].SetInteractable(false);
                }
            }
            if (finishEarlyButton != null)
            {
                if (_earlySubscribed) finishEarlyButton.Pressed -= OnFinishEarly;
                _earlySubscribed = false;
                finishEarlyButton.SetInteractable(false);
            }
            if (durationPicker != null) durationPicker.SetActive(false);
            if (progressRoot != null) progressRoot.SetActive(false);
        }

        public override void Validate(ValidationReport report)
        {
            report.Require(viewingZone, "Viewing Zone", this);
            report.Require(durationPicker, "Duration Picker", this);
            report.Require(progressRoot, "Progress Root", this);
            report.Require(progressFill, "Progress Fill", this);
            report.Require(progressLabel, "Progress Label", this);
            if (viewDirection == null) report.Warn("View Direction is empty - the player will not need to face outward.", this);
            if (finishEarlyButton == null) report.Warn("Finish Early Button is empty - the player cannot stop the timer early.", this);
            else finishEarlyButton.Validate(report);

            if (durationButtons == null || durationButtons.Length == 0)
            {
                report.Error("Duration Buttons is empty.", this);
                return;
            }
            if (durationSeconds == null || durationSeconds.Length != durationButtons.Length)
                report.Error("Duration Seconds must have the same number of entries as Duration Buttons (" + durationButtons.Length + ").", this);
            if (courageRewards == null || courageRewards.Length != durationButtons.Length)
                report.Error("Courage Rewards must have the same number of entries as Duration Buttons (" + durationButtons.Length + ").", this);
            for (int i = 0; i < durationButtons.Length; i++)
            {
                if (durationButtons[i] == null) report.Error("Duration Buttons[" + i + "] is empty.", this);
                else durationButtons[i].Validate(report);
            }
        }
    }
}
