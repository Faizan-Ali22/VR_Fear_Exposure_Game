using System;
using System.Collections;
using UnityEngine;

namespace FearExposure.Level1
{
    /// <summary>
    /// STEP 6 - Main Task: Find the Target (6-8 min).
    /// A mission marker floats over a distant building. The player collects the bonus pickup on the way,
    /// then looks out from the observation area to find three colored rooftop objects. Each object
    /// found ticks one objective. The step ends when all three are found.
    /// </summary>
    public class FindTargetsStep : LevelStep
    {
        [Serializable]
        public class TargetEntry
        {
            public string label = "object";
            public Color color = Color.white;
            [Tooltip("LookTarget on the colored rooftop object. Set its Gate Zone to the observation area zone.")]
            public LookTarget look;
            [Tooltip("Optional marker that lights up as a hint when the player is stuck.")]
            public MarkerVisual hintMarker;
        }

        [Header("Scene references")]
        [Tooltip("DiamondMarker floating above the distant mission building (optional).")]
        [SerializeField] private MarkerVisual missionMarker;
        [SerializeField] private ObjectiveListPanel objectivePanel;
        [Tooltip("Optional bonus pickup placed on the way to the observation area.")]
        [SerializeField] private Collectible bonusCollectible;

        [Header("Targets (exactly as many as the objective panel has rows)")]
        [SerializeField]
        private TargetEntry[] targets = new TargetEntry[]
        {
            new TargetEntry { label = "red object", color = new Color(1f, 0.25f, 0.2f, 1f) },
            new TargetEntry { label = "green object", color = new Color(0.25f, 0.9f, 0.3f, 1f) },
            new TargetEntry { label = "blue object", color = new Color(0.3f, 0.5f, 1f, 1f) }
        };

        [Header("Reward and hints")]
        [SerializeField] private int rewardPerTarget = 15;
        [SerializeField] private float hintAfterSeconds = 45f;

        [Header("Messages")]
        [SerializeField, TextArea] private string missionMessage = "Mission: find three colored objects on the rooftops across the city. The floating marker shows the area to search. Use the observation area to look out.";
        [SerializeField, TextArea] private string allFoundMessage = "All three found. Excellent work.";

        private bool[] _done;
        private bool[] _hinted;
        private int _found;
        private float _lastProgressTime;

        protected override void OnBegin()
        {
            _found = 0;
            _done = new bool[targets.Length];
            _hinted = new bool[targets.Length];
            _lastProgressTime = Time.time;

            if (missionMarker != null) missionMarker.Show();
            objectivePanel.gameObject.SetActive(true);
            objectivePanel.HideAllRows();
            objectivePanel.SetHeader("Find the colored objects");

            for (int i = 0; i < targets.Length; i++)
            {
                TargetEntry t = targets[i];
                objectivePanel.Configure(i, "Find the " + t.label, t.color);
                if (t.hintMarker != null) t.hintMarker.Hide();
                t.look.ResetTarget();
                t.look.Completed += OnTargetFound;
                t.look.Arm();
            }

            if (bonusCollectible != null) bonusCollectible.ResetCollectible();
            Say(missionMessage);
        }

        private void OnTargetFound(LookTarget look)
        {
            int index = IndexOf(look);
            if (index < 0 || _done[index]) return;

            _done[index] = true;
            _found++;
            _lastProgressTime = Time.time;

            objectivePanel.MarkDone(index);
            if (targets[index].hintMarker != null) targets[index].hintMarker.Hide();
            Award(rewardPerTarget, "Found the " + targets[index].label);
            if (PlayerRig.Instance != null) PlayerRig.Instance.Pulse(0.3f, 0.12f);

            if (_found >= targets.Length)
            {
                Say(allFoundMessage, 6f);
                StartCoroutine(FinishAfter(3f));
            }
            else
            {
                int remaining = targets.Length - _found;
                Say("Found the " + targets[index].label + ". " + remaining + " to go.", 6f);
            }
        }

        private void Update()
        {
            if (!IsRunning || _found >= targets.Length) return;
            if (Time.time - _lastProgressTime < hintAfterSeconds) return;

            for (int i = 0; i < targets.Length; i++)
            {
                if (_done[i] || _hinted[i]) continue;
                _hinted[i] = true;
                _lastProgressTime = Time.time;
                if (targets[i].hintMarker != null)
                {
                    targets[i].hintMarker.Show();
                    Say("Hint: the " + targets[i].label + " is near the glowing marker I just lit.", 8f);
                }
                else
                {
                    Say("Hint: keep scanning the rooftops. The " + targets[i].label + " is somewhere across the city.", 8f);
                }
                return;
            }
        }

        private IEnumerator FinishAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            Finish();
        }

        private int IndexOf(LookTarget look)
        {
            for (int i = 0; i < targets.Length; i++)
                if (targets[i].look == look) return i;
            return -1;
        }

        protected override void OnFinished()
        {
            if (targets != null)
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    if (targets[i] == null) continue;
                    if (targets[i].look != null)
                    {
                        targets[i].look.Completed -= OnTargetFound;
                        targets[i].look.Disarm();
                    }
                    if (targets[i].hintMarker != null) targets[i].hintMarker.Hide();
                }
            }
            if (missionMarker != null) missionMarker.Hide();
            if (objectivePanel != null) objectivePanel.gameObject.SetActive(false);
        }

        public override void Validate(ValidationReport report)
        {
            report.Require(objectivePanel, "Objective Panel", this);
            if (missionMarker != null) missionMarker.Validate(report);
            else report.Warn("Mission Marker is empty - the player gets no marker over the distant building.", this);
            if (bonusCollectible == null) report.Warn("Bonus Collectible is empty - there is no pickup on the way.", this);

            if (targets == null || targets.Length == 0)
            {
                report.Error("Targets is empty.", this);
                return;
            }
            if (objectivePanel != null) objectivePanel.Validate(report, targets.Length);
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null) { report.Error("Targets[" + i + "] is empty.", this); continue; }
                report.Require(targets[i].look, "Targets[" + i + "].Look", this);
                if (targets[i].hintMarker != null) targets[i].hintMarker.Validate(report);
            }
        }
    }
}
