using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FearExposure.Level1
{
    /// <summary>
    /// Courage Points: awarded by steps, shown on the results screen,
    /// compared against a threshold to decide whether Level 2 unlocks.
    /// Put on [Level1]/Systems/CourageScore.
    /// </summary>
    public class CourageScore : MonoBehaviour
    {
        [Serializable]
        public struct Entry
        {
            public string reason;
            public int points;
        }

        [Tooltip("Minimum Courage Points needed to unlock Level 2.")]
        [SerializeField] private int requiredPoints = 90;

        public UnityEvent<int> onTotalChanged;
        public UnityEvent onThresholdReached;

        /// <summary>points awarded, reason, new total.</summary>
        public event Action<int, string, int> PointsAwarded;

        private readonly List<Entry> _log = new List<Entry>();

        public int Total { get; private set; }
        public int Required { get { return requiredPoints; } }
        public bool ThresholdMet { get { return Total >= requiredPoints; } }
        public IReadOnlyList<Entry> Log { get { return _log; } }

        public void Award(int points, string reason)
        {
            if (points <= 0) return;
            bool wasMet = ThresholdMet;
            Total += points;
            _log.Add(new Entry { reason = reason, points = points });

            if (PointsAwarded != null) PointsAwarded(points, reason, Total);
            if (onTotalChanged != null) onTotalChanged.Invoke(Total);
            if (!wasMet && ThresholdMet && onThresholdReached != null) onThresholdReached.Invoke();
        }

        public void ResetScore()
        {
            Total = 0;
            _log.Clear();
            if (onTotalChanged != null) onTotalChanged.Invoke(Total);
        }

        [ContextMenu("Reset Saved Progress (PlayerPrefs)")]
        private void ResetSavedProgress()
        {
            LevelProgress.ResetAll();
            Debug.Log("[Level1] Saved progress cleared.");
        }

        [ContextMenu("Therapist Approval ON (unlock all levels)")]
        private void TherapistOn()
        {
            LevelProgress.TherapistApproved = true;
            Debug.Log("[Level1] Therapist approval set: all levels unlocked.");
        }

        [ContextMenu("Therapist Approval OFF")]
        private void TherapistOff()
        {
            LevelProgress.TherapistApproved = false;
            Debug.Log("[Level1] Therapist approval cleared.");
        }
    }
}
