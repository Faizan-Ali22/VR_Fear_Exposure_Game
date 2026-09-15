using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using SkylineMission.Mission;

namespace SkylineMission.UI
{
    public class SkylineHUD : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI objectiveText;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI notificationText;
        [SerializeField] private Canvas hudCanvas;

        [Header("Follow Settings")]
        [SerializeField] private Transform followTarget;
        [SerializeField] private float followDistance = 2.5f;
        [SerializeField] private float followHeight = 0.3f;
        [SerializeField] private float followSpeed = 3f;
        [SerializeField] private float hudScale = 0.002f;

        private Coroutine notificationCoroutine;

        private void Awake()
        {
            if (hudCanvas == null)
            {
                hudCanvas = GetComponentInChildren<Canvas>();
            }

            if (hudCanvas != null)
            {
                hudCanvas.renderMode = RenderMode.WorldSpace;
                hudCanvas.transform.localScale = new Vector3(hudScale, hudScale, hudScale);
            }

            if (followTarget == null && Camera.main != null)
            {
                followTarget = Camera.main.transform;
            }

            if (notificationText != null)
            {
                notificationText.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (SkylineMissionManager.Instance != null)
            {
                SkylineMissionManager.Instance.OnPhaseChanged += HandlePhaseChanged;
            }
        }

        private void OnDisable()
        {
            if (SkylineMissionManager.Instance != null)
            {
                SkylineMissionManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
            }
        }

        private void LateUpdate()
        {
            if (followTarget == null || hudCanvas == null) return;

            Vector3 targetPosition = followTarget.position + followTarget.forward * followDistance + Vector3.up * followHeight;
            
            hudCanvas.transform.position = Vector3.Lerp(hudCanvas.transform.position, targetPosition, Time.deltaTime * followSpeed);
            
            Vector3 lookDirection = hudCanvas.transform.position - followTarget.position;
            if (lookDirection != Vector3.zero)
            {
                hudCanvas.transform.rotation = Quaternion.LookRotation(lookDirection);
            }
        }

        public void SetObjective(string text)
        {
            if (objectiveText != null)
            {
                objectiveText.text = text;
                StartCoroutine(AnimateObjectiveText());
            }
        }

        private IEnumerator AnimateObjectiveText()
        {
            float duration = 0.3f;
            float elapsed = 0f;
            Color color = objectiveText.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                float scale = Mathf.Lerp(0.8f, 1.0f, t);
                objectiveText.transform.localScale = new Vector3(scale, scale, scale);
                
                color.a = Mathf.Lerp(0f, 1f, t);
                objectiveText.color = color;
                
                yield return null;
            }

            objectiveText.transform.localScale = Vector3.one;
            color.a = 1f;
            objectiveText.color = color;
        }

        public void SetProgress(int current, int total)
        {
            if (progressText != null)
            {
                progressText.text = $"Task {current}/{total}";
            }
        }

        public void ShowTimer(bool show)
        {
            if (timerText != null)
            {
                timerText.gameObject.SetActive(show);
            }
        }

        public void UpdateTimer(float seconds)
        {
            if (timerText != null)
            {
                int min = Mathf.FloorToInt(seconds / 60f);
                int sec = Mathf.FloorToInt(seconds % 60f);
                timerText.text = $"{min:00}:{sec:00}";
            }
        }

        public void ShowNotification(string text, float duration = 3f)
        {
            if (notificationText != null)
            {
                if (notificationCoroutine != null)
                {
                    StopCoroutine(notificationCoroutine);
                }
                notificationCoroutine = StartCoroutine(NotificationRoutine(text, duration));
            }
        }

        private IEnumerator NotificationRoutine(string text, float duration)
        {
            notificationText.text = text;
            notificationText.gameObject.SetActive(true);
            
            Color c = notificationText.color;
            c.a = 1f;
            notificationText.color = c;

            yield return new WaitForSeconds(duration);

            float fadeTime = 0.5f;
            float elapsed = 0f;

            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                c.a = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
                notificationText.color = c;
                yield return null;
            }

            notificationText.gameObject.SetActive(false);
            notificationCoroutine = null;
        }

        private void HandlePhaseChanged(MissionPhase phase)
        {
            switch (phase)
            {
                case MissionPhase.Introduction:
                    SetObjective("Introduction: Get ready.");
                    SetProgress(0, 4);
                    break;
                case MissionPhase.PlankTraversal:
                    SetObjective("Walk across the plank.");
                    SetProgress(1, 4);
                    break;
                case MissionPhase.PlatformJumping:
                    SetObjective("Jump across the platforms.");
                    SetProgress(2, 4);
                    break;
                case MissionPhase.DroneRescue:
                    SetObjective("Rescue the drone from the ledge.");
                    SetProgress(3, 4);
                    break;
                case MissionPhase.GlassBridgeCrossing:
                    SetObjective("Cross the glass bridge carefully.");
                    SetProgress(4, 4);
                    break;
                case MissionPhase.Complete:
                    SetObjective("Mission Complete!");
                    SetProgress(4, 4);
                    ShowTimer(false);
                    break;
                default:
                    SetObjective("");
                    SetProgress(0, 4);
                    break;
            }
        }
    }
}
