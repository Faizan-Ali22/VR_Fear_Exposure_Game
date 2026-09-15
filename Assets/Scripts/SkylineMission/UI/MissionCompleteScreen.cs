using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SkylineMission.Mission;
using System;
using UnityEngine.SceneManagement;

namespace SkylineMission.UI
{
    public class MissionCompleteScreen : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI fallCountText;
        [SerializeField] private TextMeshProUGUI tasksText;
        [SerializeField] private TextMeshProUGUI ratingText;
        [SerializeField] private GameObject[] starImages;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private Canvas panelCanvas;

        [Header("Settings")]
        [SerializeField] private float spawnDistance = 2f;

        public event Action OnRetryRequested;
        public event Action OnExitRequested;

        private void Awake()
        {
            if (retryButton != null)
            {
                retryButton.onClick.AddListener(HandleRetry);
            }
            else
            {
                Debug.LogError("MissionCompleteScreen: retryButton is null!");
            }

            if (exitButton != null)
            {
                exitButton.onClick.AddListener(HandleExit);
            }
            else
            {
                Debug.LogError("MissionCompleteScreen: exitButton is null!");
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
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

        public void Show(float totalTime, int fallCount, int tasksCompleted)
        {
            if (panelRoot == null) return;

            panelRoot.SetActive(true);

            if (Camera.main != null)
            {
                Transform cam = Camera.main.transform;
                transform.position = cam.position + cam.forward * spawnDistance;
                Vector3 lookDir = transform.position - cam.position;
                lookDir.y = 0; 
                if (lookDir != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(lookDir);
                }
            }

            if (timeText != null)
            {
                int min = Mathf.FloorToInt(totalTime / 60f);
                int sec = Mathf.FloorToInt(totalTime % 60f);
                timeText.text = $"{min:00}:{sec:00}";
            }

            if (fallCountText != null)
            {
                fallCountText.text = fallCount.ToString();
            }

            if (tasksText != null)
            {
                tasksText.text = $"{tasksCompleted}/4";
            }

            int stars = 1;
            if (fallCount == 0) stars = 3;
            else if (fallCount <= 3) stars = 2;

            if (starImages != null)
            {
                for (int i = 0; i < starImages.Length; i++)
                {
                    if (starImages[i] != null)
                    {
                        starImages[i].SetActive(i < stars);
                    }
                }
            }

            if (ratingText != null)
            {
                if (stars == 3) ratingText.text = "Perfect!";
                else if (stars == 2) ratingText.text = "Great Job!";
                else ratingText.text = "Keep Practicing!";
            }
        }

        public void Hide()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        private void HandleRetry()
        {
            OnRetryRequested?.Invoke();
            if (SkylineMissionManager.Instance != null)
            {
                SkylineMissionManager.Instance.StartMission();
            }
            Hide();
        }

        private void HandleExit()
        {
            OnExitRequested?.Invoke();
            SceneManager.LoadScene("1 Start Scene");
        }

        private void HandlePhaseChanged(MissionPhase phase)
        {
            if (phase == MissionPhase.Complete)
            {
                if (SkylineMissionManager.Instance != null)
                {
                    Show(SkylineMissionManager.Instance.ElapsedTime, SkylineMissionManager.Instance.FallCount, 4);
                }
            }
        }
    }
}
