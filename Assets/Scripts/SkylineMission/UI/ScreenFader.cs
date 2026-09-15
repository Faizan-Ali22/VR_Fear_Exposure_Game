using System.Collections;
using UnityEngine;

namespace SkylineMission.UI
{
    /// <summary>
    /// VR-safe screen fading utility using a sphere mesh around the camera.
    /// </summary>
    public class ScreenFader : MonoBehaviour
    {
        private MeshRenderer fadeRenderer;
        private Material fadeMaterial;
        private WaitForEndOfFrame waitForEndOfFrame;
        private Coroutine currentCoroutine;

        /// <summary>
        /// True if the screen is currently fading.
        /// </summary>
        public bool IsFading { get; private set; }

        private void Awake()
        {
            waitForEndOfFrame = new WaitForEndOfFrame();
            CreateFadeSphere();
        }

        private void CreateFadeSphere()
        {
            GameObject fadeSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fadeSphere.name = "FadeSphere";
            
            if (Camera.main != null)
            {
                fadeSphere.transform.SetParent(Camera.main.transform, false);
            }
            
            fadeSphere.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            fadeSphere.transform.localPosition = Vector3.zero;

            Destroy(fadeSphere.GetComponent<Collider>());

            fadeRenderer = fadeSphere.GetComponent<MeshRenderer>();
            fadeMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            fadeMaterial.SetColor("_BaseColor", new Color(0, 0, 0, 0));
            fadeMaterial.SetFloat("_Surface", 1); // Transparent
            fadeMaterial.SetFloat("_Cull", 1); // Front
            fadeMaterial.renderQueue = 5000;
            
            fadeMaterial.SetOverrideTag("RenderType", "Transparent");
            fadeMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            fadeMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            fadeMaterial.SetInt("_ZWrite", 0);
            
            fadeRenderer.material = fadeMaterial;
            fadeRenderer.enabled = false;
        }

        /// <summary>
        /// Fades the screen to black over the specified duration.
        /// </summary>
        public Coroutine FadeOut(float duration = 0.5f)
        {
            if (currentCoroutine != null) StopCoroutine(currentCoroutine);
            return currentCoroutine = StartCoroutine(FadeCoroutine(1f, duration));
        }

        /// <summary>
        /// Fades the screen to clear over the specified duration.
        /// </summary>
        public Coroutine FadeIn(float duration = 0.5f)
        {
            if (currentCoroutine != null) StopCoroutine(currentCoroutine);
            return currentCoroutine = StartCoroutine(FadeCoroutine(0f, duration));
        }

        /// <summary>
        /// Sets the fade alpha immediately without interpolation.
        /// </summary>
        public void SetFadeImmediate(float alpha)
        {
            alpha = Mathf.Clamp01(alpha);
            Color c = fadeMaterial.GetColor("_BaseColor");
            c.a = alpha;
            fadeMaterial.SetColor("_BaseColor", c);
            fadeRenderer.enabled = (alpha > 0f);
        }

        private IEnumerator FadeCoroutine(float targetAlpha, float duration)
        {
            IsFading = true;
            fadeRenderer.enabled = true;
            
            Color startColor = fadeMaterial.GetColor("_BaseColor");
            float startAlpha = startColor.a;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
                
                Color c = startColor;
                c.a = newAlpha;
                fadeMaterial.SetColor("_BaseColor", c);
                
                yield return waitForEndOfFrame;
            }

            Color finalColor = startColor;
            finalColor.a = targetAlpha;
            fadeMaterial.SetColor("_BaseColor", finalColor);

            if (targetAlpha <= 0.01f)
            {
                fadeRenderer.enabled = false;
            }

            IsFading = false;
            currentCoroutine = null;
        }
    }
}
