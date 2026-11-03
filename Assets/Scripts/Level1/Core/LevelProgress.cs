using UnityEngine;

namespace FearExposure.Level1
{
    /// <summary>
    /// Tiny save system (PlayerPrefs). Tracks which levels are unlocked and the best Courage score.
    /// Therapist approval unlocks every level regardless of score.
    /// </summary>
    public static class LevelProgress
    {
        private const string UnlockedPrefix = "FE_LevelUnlocked_";
        private const string BestKey = "FE_Level1_BestCourage";
        private const string TherapistKey = "FE_TherapistApproved";

        public static bool TherapistApproved
        {
            get { return PlayerPrefs.GetInt(TherapistKey, 0) == 1; }
            set
            {
                PlayerPrefs.SetInt(TherapistKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool IsLevelUnlocked(int level)
        {
            if (level <= 1) return true;
            return TherapistApproved || PlayerPrefs.GetInt(UnlockedPrefix + level, 0) == 1;
        }

        public static void UnlockLevel(int level)
        {
            if (level <= 1) return;
            PlayerPrefs.SetInt(UnlockedPrefix + level, 1);
            PlayerPrefs.Save();
        }

        public static int BestCourage { get { return PlayerPrefs.GetInt(BestKey, 0); } }

        public static void RecordLevel1(int points)
        {
            if (points <= BestCourage) return;
            PlayerPrefs.SetInt(BestKey, points);
            PlayerPrefs.Save();
        }

        /// <summary>Testing helper: wipes everything this game saved.</summary>
        public static void ResetAll()
        {
            PlayerPrefs.DeleteKey(UnlockedPrefix + 2);
            PlayerPrefs.DeleteKey(UnlockedPrefix + 3);
            PlayerPrefs.DeleteKey(BestKey);
            PlayerPrefs.DeleteKey(TherapistKey);
            PlayerPrefs.Save();
        }
    }
}
