using UnityEngine;
using SkylineMission.Mechanics;

namespace SkylineMission.UI
{
    /// <summary>
    /// Dynamic FOV vignette that activates during fast movement or wind gusts.
    /// </summary>
    public class ComfortVignette : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Master toggle for the vignette")]
        [SerializeField] private bool isEnabled = true;
        [Tooltip("Velocity threshold before vignette starts appearing")]
        [SerializeField] private float comfortThreshold = 1f;
        [Tooltip("Velocity range over which vignette intensifies")]
        [SerializeField] private float velocityRange = 3f;
        [Tooltip("Maximum alpha for the vignette overlay")]
        [SerializeField] private float maxAlpha = 0.6f;
        [Tooltip("Smoothing speed for vignette changes")]
        [SerializeField] private float smoothSpeed = 5f;

        private CharacterController characterController;
        private MeshRenderer vignetteRenderer;
        private Material vignetteMaterial;

        /// <summary>
        /// Master toggle for the vignette system.
        /// </summary>
        public bool IsEnabled { get => isEnabled; set => isEnabled = value; }
        
        /// <summary>
        /// Current intensity of the vignette (0-1).
        /// </summary>
        public float VignetteIntensity { get; private set; }

        private void Awake()
        {
            characterController = GetComponentInParent<CharacterController>();
            if (characterController == null)
            {
                characterController = FindFirstObjectByType<CharacterController>();
            }

            CreateVignetteQuad();
        }

        private void CreateVignetteQuad()
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "ComfortVignetteQuad";
            
            if (Camera.main != null)
            {
                quad.transform.SetParent(Camera.main.transform, false);
            }
            
            quad.transform.localPosition = new Vector3(0, 0, 0.3f);
            quad.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
            Destroy(quad.GetComponent<Collider>());

            vignetteRenderer = quad.GetComponent<MeshRenderer>();
            vignetteMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            
            Texture2D gradTex = CreateGradientTexture();
            vignetteMaterial.mainTexture = gradTex;
            
            vignetteMaterial.SetFloat("_Surface", 1); // Transparent
            vignetteMaterial.SetOverrideTag("RenderType", "Transparent");
            vignetteMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            vignetteMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            vignetteMaterial.SetInt("_ZWrite", 0);
            vignetteMaterial.SetColor("_BaseColor", new Color(0, 0, 0, 0));
            vignetteMaterial.renderQueue = 4000;

            vignetteRenderer.material = vignetteMaterial;
            vignetteRenderer.enabled = false;
        }

        private Texture2D CreateGradientTexture()
        {
            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color centerColor = new Color(0, 0, 0, 0);
            Color edgeColor = new Color(0, 0, 0, 1);
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float maxRadius = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(center, new Vector2(x, y));
                    float t = Mathf.Clamp01((dist - maxRadius * 0.5f) / (maxRadius * 0.5f));
                    tex.SetPixel(x, y, Color.Lerp(centerColor, edgeColor, t));
                }
            }
            tex.Apply();
            return tex;
        }

        private void Update()
        {
            if (!isEnabled)
            {
                UpdateAlpha(0f);
                return;
            }

            float velocityMag = characterController != null ? characterController.velocity.magnitude : 0f;
            float windContrib = WindForceSystem.Instance != null ? WindForceSystem.Instance.CurrentWindStrength : 0f;

            float targetIntensity = Mathf.Clamp01((velocityMag - comfortThreshold) / velocityRange + windContrib);
            
            VignetteIntensity = Mathf.Lerp(VignetteIntensity, targetIntensity, Time.deltaTime * smoothSpeed);

            UpdateAlpha(VignetteIntensity * maxAlpha);
        }

        private void UpdateAlpha(float alpha)
        {
            if (alpha < 0.01f)
            {
                vignetteRenderer.enabled = false;
            }
            else
            {
                vignetteRenderer.enabled = true;
                Color c = vignetteMaterial.GetColor("_BaseColor");
                c.a = alpha;
                vignetteMaterial.SetColor("_BaseColor", c);
            }
        }
    }
}
