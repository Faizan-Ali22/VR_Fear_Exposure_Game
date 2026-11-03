using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FearExposure.Level1
{
    /// <summary>
    /// The results board at the end of the level: Courage Points, a short breakdown,
    /// and whether Level 2 is unlocked. Has Replay and Continue buttons.
    /// "Root" is the World Space Canvas object, which should start switched OFF.
    /// The canvas needs a Tracked Device Graphic Raycaster, and the scene needs an
    /// EventSystem with an XR UI Input Module, so the buttons can be clicked with the controller ray.
    /// </summary>
    public class CompletionScreen : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text pointsText;
        [SerializeField] private TMP_Text breakdownText;
        [SerializeField] private TMP_Text unlockText;
        [SerializeField] private Button replayButton;
        [SerializeField] private Button continueButton;
        [Tooltip("Scene to load when Continue is pressed (it must be in the Build Profiles Scene List). Empty = do nothing.")]
        [SerializeField] private string nextSceneName = "";
        [SerializeField] private float distance = 2.2f;

        public UnityEvent onContinue;

        private readonly StringBuilder _text = new StringBuilder(256);
        private bool _wired;

        private void Awake()
        {
            WireButtons();
            if (root != null) root.SetActive(false);
        }

        private void WireButtons()
        {
            if (_wired) return;
            _wired = true;
            if (replayButton != null) replayButton.onClick.AddListener(Replay);
            if (continueButton != null) continueButton.onClick.AddListener(Continue);
        }

        public void Show(CourageScore score)
        {
            WireButtons();
            if (root == null) return;

            PlayerRig rig = PlayerRig.Instance;
            root.SetActive(true);
            if (rig != null && rig.Head != null)
            {
                Vector3 f = rig.Head.forward;
                f.y = 0f;
                if (f.sqrMagnitude < 0.0001f) f = Vector3.forward;
                else f.Normalize();
                Vector3 pos = rig.Head.position + f * distance;
                pos.y = rig.Head.position.y - 0.1f;
                root.transform.position = pos;
                Vector3 look = pos - rig.Head.position;
                look.y = 0f;
                if (look.sqrMagnitude > 0.0001f) root.transform.rotation = Quaternion.LookRotation(look);
            }

            int total = score != null ? score.Total : 0;
            int required = score != null ? score.Required : 0;
            bool met = score == null || score.ThresholdMet;

            if (titleText != null) titleText.text = "Level 1 Complete";
            if (pointsText != null) pointsText.text = total + " Courage Points";

            if (breakdownText != null)
            {
                _text.Length = 0;
                if (score != null)
                {
                    int shown = Mathf.Min(score.Log.Count, 12);
                    for (int i = 0; i < shown; i++)
                    {
                        CourageScore.Entry e = score.Log[i];
                        _text.Append('+').Append(e.points).Append("   ").Append(e.reason).Append('\n');
                    }
                    if (score.Log.Count > shown) _text.Append("...");
                }
                breakdownText.text = _text.ToString();
            }

            if (unlockText != null)
            {
                if (LevelProgress.TherapistApproved) unlockText.text = "Level 2 unlocked (therapist approval).";
                else if (met) unlockText.text = "Level 2 unlocked!";
                else unlockText.text = "Level 2 needs " + required + " Courage Points. You have " + total + ". Replay Level 1 any time to earn more.";
            }

            if (continueButton != null) continueButton.interactable = LevelProgress.IsLevelUnlocked(2);
        }

        public void Hide()
        {
            if (root != null) root.SetActive(false);
        }

        private void Replay()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.buildIndex < 0)
            {
                Debug.LogError("[Level1] Replay needs this scene in File > Build Profiles > Scene List. Add it and try again.");
                return;
            }
            SceneManager.LoadScene(scene.buildIndex);
        }

        private void Continue()
        {
            if (onContinue != null) onContinue.Invoke();
            if (string.IsNullOrEmpty(nextSceneName))
            {
                Debug.Log("[Level1] Continue pressed. Set 'Next Scene Name' on CompletionScreen to load Level 2.");
                return;
            }
            if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
            {
                Debug.LogError("[Level1] Scene '" + nextSceneName + "' is not in File > Build Profiles > Scene List.");
                return;
            }
            SceneManager.LoadScene(nextSceneName);
        }

        public void Validate(ValidationReport report)
        {
            report.Require(root, "Root", this);
            report.Require(pointsText, "Points Text", this);
            if (replayButton == null || continueButton == null)
                report.Warn("Replay/Continue buttons are not both assigned.", this);
        }
    }
}
