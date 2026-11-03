using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace FearExposure.Level1
{
    /// <summary>
    /// Fade to black and back using a quad parked in front of the camera.
    /// No post-processing is used, so it costs almost nothing on Quest.
    /// Assign a URP/Unlit material with Surface Type = Transparent.
    /// </summary>
    public class FadeOverlay : MonoBehaviour
    {
        [Tooltip("URP/Unlit material, Surface Type = Transparent, Base Color = black.")]
        [SerializeField] private Material fadeMaterial;
        [SerializeField] private float quadDistance = 0.2f;
        [SerializeField] private float quadSize = 2f;

        private Material _material;
        private Renderer _renderer;
        private float _alpha;
        private bool _fading;
        private float _from;
        private float _to;
        private float _seconds = 1f;
        private float _elapsed;
        private int _requestId;

        public float Alpha { get { return _alpha; } }

        public void SetAlpha(float alpha)
        {
            EnsureBuilt();
            _alpha = Mathf.Clamp01(alpha);
            if (_renderer == null) return;
            MaterialUtil.SetColor(_material, new Color(0f, 0f, 0f, _alpha));
            _renderer.enabled = _alpha > 0.001f;
        }

        /// <summary>Starts a fade. "yield return" the result to wait for it to finish.</summary>
        public Coroutine StartFade(float targetAlpha, float seconds)
        {
            EnsureBuilt();
            _requestId++;
            _from = _alpha;
            _to = Mathf.Clamp01(targetAlpha);
            _seconds = Mathf.Max(0.01f, seconds);
            _elapsed = 0f;
            _fading = true;
            return StartCoroutine(WaitForFade(_requestId));
        }

        private IEnumerator WaitForFade(int id)
        {
            // Ends when this fade finishes OR a newer fade replaces it, so callers never hang.
            while (_fading && _requestId == id) yield return null;
        }

        private void Update()
        {
            if (!_fading) return;
            _elapsed += Time.deltaTime / _seconds;
            float t = Mathf.Clamp01(_elapsed);
            SetAlpha(Mathf.Lerp(_from, _to, Mathf.SmoothStep(0f, 1f, t)));
            if (t >= 1f)
            {
                SetAlpha(_to);
                _fading = false;
            }
        }

        private void EnsureBuilt()
        {
            if (_renderer != null) return;
            PlayerRig rig = PlayerRig.Instance;
            if (rig == null || rig.Head == null || fadeMaterial == null) return;

            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "FadeQuad (runtime)";
            Collider col = quad.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Transform t = quad.transform;
            t.SetParent(rig.Head, false);
            t.localPosition = new Vector3(0f, 0f, quadDistance);
            t.localRotation = Quaternion.identity;
            t.localScale = new Vector3(quadSize, quadSize, 1f);

            _renderer = quad.GetComponent<Renderer>();
            _renderer.shadowCastingMode = ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _material = MaterialUtil.Clone(fadeMaterial);
            _material.renderQueue = 4000;
            _renderer.sharedMaterial = _material;
            _renderer.enabled = false;
        }

        private void OnDestroy()
        {
            if (_material != null) Destroy(_material);
        }

        public void Validate(ValidationReport report)
        {
            report.Require(fadeMaterial, "Fade Material", this);
        }
    }
}
