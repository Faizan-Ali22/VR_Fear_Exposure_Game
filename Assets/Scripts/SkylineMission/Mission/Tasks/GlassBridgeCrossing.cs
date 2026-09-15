using System.Collections;
using UnityEngine;
using SkylineMission.Mechanics;

namespace SkylineMission.Mission.Tasks
{
    public class GlassBridgeCrossing : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private Transform bridgeStart;
        [SerializeField] private Transform bridgeEnd;
        [SerializeField] private float completionRadius = 2f;
        [SerializeField] private float windGustInterval = 4f;
        [SerializeField] private float windGustStrength = 0.7f;
        
        [Header("Glass Panels")]
        [SerializeField] private GameObject[] glassPanels;
        [SerializeField] private int[] crackablePanelIndices;
        [SerializeField] private Material crackMaterial;

        private bool isActive;
        public bool IsActive 
        { 
            get => isActive; 
            set 
            {
                isActive = value;
                if (isActive)
                {
                    playerStartedCrossing = false;
                    if (windCoroutine != null) StopCoroutine(windCoroutine);
                    windCoroutine = StartCoroutine(WindGustRoutine());
                }
                else
                {
                    if (windCoroutine != null) StopCoroutine(windCoroutine);
                    RestorePanels();
                }
            }
        }

        public bool IsComplete { get; private set; }

        public event System.Action OnTaskCompleted;

        private Transform playerTransform;
        private bool playerStartedCrossing = false;
        private Coroutine windCoroutine;
        private Material[] originalMaterials;

        private void Awake()
        {
            if (bridgeStart == null) Debug.LogError("GlassBridgeCrossing: BridgeStart is null!");
            if (bridgeEnd == null) Debug.LogError("GlassBridgeCrossing: BridgeEnd is null!");

            if (glassPanels != null && glassPanels.Length > 0)
            {
                originalMaterials = new Material[glassPanels.Length];
                for (int i = 0; i < glassPanels.Length; i++)
                {
                    if (glassPanels[i] != null)
                    {
                        Renderer r = glassPanels[i].GetComponent<Renderer>();
                        if (r != null)
                        {
                            originalMaterials[i] = r.sharedMaterial;
                        }
                    }
                }
            }
        }

        private void Start()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                Debug.LogError("GlassBridgeCrossing: Could not find object with 'Player' tag.");
            }
        }

        private void Update()
        {
            if (!IsActive || IsComplete || playerTransform == null) return;

            if (!playerStartedCrossing)
            {
                float distToStart = Vector3.Distance(playerTransform.position, bridgeStart.position);
                if (distToStart <= completionRadius)
                {
                    playerStartedCrossing = true;
                }
            }

            if (playerStartedCrossing)
            {
                CheckCrackablePanels();

                float distToEnd = Vector3.Distance(playerTransform.position, bridgeEnd.position);
                if (distToEnd <= completionRadius)
                {
                    IsComplete = true;
                    OnTaskCompleted?.Invoke();
                }
            }
        }

        private void CheckCrackablePanels()
        {
            if (crackablePanelIndices == null || crackMaterial == null || glassPanels == null) return;

            foreach (int index in crackablePanelIndices)
            {
                if (index >= 0 && index < glassPanels.Length)
                {
                    GameObject panel = glassPanels[index];
                    if (panel != null)
                    {
                        float dist = Vector3.Distance(playerTransform.position, panel.transform.position);
                        if (dist <= 1.5f) // Crack when close
                        {
                            Renderer r = panel.GetComponent<Renderer>();
                            if (r != null && r.sharedMaterial != crackMaterial)
                            {
                                r.sharedMaterial = crackMaterial;
                            }
                        }
                    }
                }
            }
        }

        private IEnumerator WindGustRoutine()
        {
            WaitForSeconds wait = new WaitForSeconds(windGustInterval);
            while (true)
            {
                yield return wait;
                if (WindForceSystem.Instance != null)
                {
                    WindForceSystem.Instance.TriggerGust(windGustStrength, 2f);
                }
            }
        }

        private void RestorePanels()
        {
            if (glassPanels == null || originalMaterials == null) return;

            for (int i = 0; i < glassPanels.Length; i++)
            {
                if (glassPanels[i] != null && i < originalMaterials.Length && originalMaterials[i] != null)
                {
                    Renderer r = glassPanels[i].GetComponent<Renderer>();
                    if (r != null)
                    {
                        r.sharedMaterial = originalMaterials[i];
                    }
                }
            }
        }
    }
}
