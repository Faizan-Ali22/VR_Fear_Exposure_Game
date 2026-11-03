using UnityEngine;
using UnityEngine.Rendering;

namespace FearExposure.Level1
{
    /// <summary>
    /// A glowing marker built from Unity primitives (no art needed):
    /// FloorRing = glowing disc on the floor, Beam = tall light column, FloatingDiamond = spinning gem.
    /// Steps call Show() / Hide(). Assign any URP/Unlit material (Transparent looks best).
    /// </summary>
    public class MarkerVisual : MonoBehaviour
    {
        public enum MarkerStyle { FloorRing, Beam, FloatingDiamond }

        [SerializeField] private MarkerStyle style = MarkerStyle.FloorRing;
        [Tooltip("Any URP/Unlit material. The colour below is applied to a private copy.")]
        [SerializeField] private Material material;
        [SerializeField] private Color color = new Color(0.2f, 0.9f, 1f, 0.6f);
        [Tooltip("Ring diameter, beam width or diamond size, in metres.")]
        [SerializeField] private float size = 1f;
        [Tooltip("Beam height in metres (Beam style only).")]
        [SerializeField] private float height = 40f;
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float pulseAmount = 0.1f;
        [SerializeField] private float spinSpeed = 45f;
        [SerializeField] private bool visibleOnStart;

        private GameObject _visual;
        private Transform _visualT;
        private Material _instance;
        private Vector3 _baseScale;
        private Vector3 _basePos;
        private bool _visible;

        public bool IsVisible { get { return _visible; } }

        private void Start()
        {
            if (visibleOnStart) Show();
        }

        public void Show()
        {
            Build();
            if (_visual == null) return;
            _visual.SetActive(true);
            _visible = true;
        }

        public void Hide()
        {
            _visible = false;
            if (_visual != null) _visual.SetActive(false);
        }

        public void SetColor(Color c)
        {
            color = c;
            if (_instance != null) MaterialUtil.SetColor(_instance, c);
        }

        private void Build()
        {
            if (_visual != null) return;
            if (material == null)
            {
                Debug.LogError("[Level1] MarkerVisual on '" + name + "' has no Material assigned.", this);
                return;
            }

            PrimitiveType primitive = style == MarkerStyle.FloatingDiamond ? PrimitiveType.Cube : PrimitiveType.Cylinder;
            _visual = GameObject.CreatePrimitive(primitive);
            _visual.name = "MarkerMesh (runtime)";
            Collider col = _visual.GetComponent<Collider>();
            if (col != null) Destroy(col);

            _visualT = _visual.transform;
            _visualT.SetParent(transform, false);
            switch (style)
            {
                case MarkerStyle.FloorRing:
                    _visualT.localScale = new Vector3(size, 0.01f, size);
                    _visualT.localPosition = new Vector3(0f, 0.015f, 0f);
                    break;
                case MarkerStyle.Beam:
                    _visualT.localScale = new Vector3(size, height * 0.5f, size);
                    _visualT.localPosition = new Vector3(0f, height * 0.5f, 0f);
                    break;
                default:
                    _visualT.localScale = Vector3.one * size;
                    _visualT.localRotation = Quaternion.Euler(45f, 45f, 0f);
                    _visualT.localPosition = Vector3.zero;
                    break;
            }
            _baseScale = _visualT.localScale;
            _basePos = _visualT.localPosition;

            Renderer r = _visual.GetComponent<Renderer>();
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            _instance = MaterialUtil.Clone(material);
            MaterialUtil.SetColor(_instance, color);
            r.sharedMaterial = _instance;
            _visual.SetActive(false);
        }

        private void Update()
        {
            if (!_visible || _visualT == null) return;
            float k = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            if (style == MarkerStyle.FloatingDiamond)
            {
                _visualT.localScale = _baseScale * k;
                _visualT.localPosition = _basePos + new Vector3(0f, Mathf.Sin(Time.time * 1.3f) * 0.12f, 0f);
                _visualT.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
            }
            else
            {
                _visualT.localScale = new Vector3(_baseScale.x * k, _baseScale.y, _baseScale.z * k);
            }
        }

        private void OnDestroy()
        {
            if (_instance != null) Destroy(_instance);
        }

        public void Validate(ValidationReport report)
        {
            report.Require(material, "Material", this);
        }
    }
}
