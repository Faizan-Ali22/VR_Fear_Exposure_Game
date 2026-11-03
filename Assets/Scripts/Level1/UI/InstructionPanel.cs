using TMPro;
using UnityEngine;

namespace FearExposure.Level1
{
    /// <summary>
    /// The on-screen guide voice: shows one short instruction at a time, fades it in and out,
    /// optionally plays a voice clip, and shows small "+10 Courage" toasts.
    /// Put on a World Space Canvas root (next to FollowPanel). A Canvas Group is added automatically.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class InstructionPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text bodyText;
        [Tooltip("Optional small line for '+10 Courage' messages.")]
        [SerializeField] private TMP_Text toastText;
        [Tooltip("Canvas Group on the toast text object (needed if Toast Text is used).")]
        [SerializeField] private CanvasGroup toastGroup;
        [Tooltip("Optional 2D AudioSource for voice-over clips.")]
        [SerializeField] private AudioSource voiceSource;
        [SerializeField] private float fadeSeconds = 0.35f;
        [SerializeField] private float toastSeconds = 2.5f;

        private CanvasGroup _bodyGroup;
        private float _bodyTarget;
        private float _hideAt = -1f;
        private float _toastTarget;
        private float _toastHideAt = -1f;

        private void Awake()
        {
            _bodyGroup = GetComponent<CanvasGroup>();
            _bodyGroup.alpha = 0f;
            _bodyGroup.interactable = false;
            _bodyGroup.blocksRaycasts = false;
            if (toastGroup != null)
            {
                toastGroup.alpha = 0f;
                toastGroup.interactable = false;
                toastGroup.blocksRaycasts = false;
            }
        }

        /// <summary>Show a message. seconds = 0 keeps it up until the next message or Hide().</summary>
        public void Show(string message, float seconds = 0f, AudioClip voice = null)
        {
            if (bodyText != null) bodyText.text = message;
            _bodyTarget = 1f;
            _hideAt = seconds > 0f ? Time.time + seconds : -1f;
            if (voice != null && voiceSource != null)
            {
                voiceSource.Stop();
                voiceSource.clip = voice;
                voiceSource.Play();
            }
        }

        public void Hide()
        {
            _bodyTarget = 0f;
            _hideAt = -1f;
        }

        /// <summary>Hooked to CourageScore.PointsAwarded by Level1Director.</summary>
        public void ShowCourageToast(int points, string reason, int total)
        {
            if (toastText == null) return;
            toastText.text = "+" + points + " Courage  -  " + reason;
            _toastTarget = 1f;
            _toastHideAt = Time.time + toastSeconds;
        }

        private void Update()
        {
            float step = Time.deltaTime / Mathf.Max(0.01f, fadeSeconds);
            if (_hideAt > 0f && Time.time >= _hideAt)
            {
                _hideAt = -1f;
                _bodyTarget = 0f;
            }
            if (_toastHideAt > 0f && Time.time >= _toastHideAt)
            {
                _toastHideAt = -1f;
                _toastTarget = 0f;
            }
            if (_bodyGroup != null) _bodyGroup.alpha = Mathf.MoveTowards(_bodyGroup.alpha, _bodyTarget, step);
            if (toastGroup != null) toastGroup.alpha = Mathf.MoveTowards(toastGroup.alpha, _toastTarget, step);
        }

        public void Validate(ValidationReport report)
        {
            report.Require(bodyText, "Body Text", this);
            if (toastText != null && toastGroup == null)
                report.Warn("Toast Text is set but Toast Group is empty - toasts will not fade.", this);
        }
    }
}
