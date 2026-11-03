using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace FearExposure.Level1
{
    /// <summary>
    /// Floor arrows ("follow the floor indicators") that lead the player along a list of waypoints.
    /// A bright pulse travels along the arrows; each arrow disappears when the player walks past it.
    /// Arrows are built at runtime (a chevron texture is drawn in code), so no art is needed.
    /// Keep this object at Scale (1,1,1).
    /// </summary>
    public class GuidePath : MonoBehaviour
    {
        [Tooltip("Empty objects standing on the floor, in walking order: start ... elevator.")]
        [SerializeField] private Transform[] waypoints;
        [Tooltip("Any URP/Unlit material set to Surface Type = Transparent. Colour and texture are replaced at runtime.")]
        [SerializeField] private Material material;
        [SerializeField] private Color dimColor = new Color(0.3f, 1f, 0.6f, 0.30f);
        [SerializeField] private Color litColor = new Color(0.3f, 1f, 0.6f, 1f);
        [SerializeField] private float spacing = 1.2f;
        [SerializeField] private float arrowSize = 0.55f;
        [SerializeField] private float floorOffset = 0.03f;
        [Tooltip("Arrows per second that the bright pulse travels.")]
        [SerializeField] private float sweepSpeed = 3f;
        [Tooltip("An arrow switches off when the player is closer than this (metres).")]
        [SerializeField] private float hideRadius = 0.9f;

        private readonly List<Renderer> _arrows = new List<Renderer>();
        private readonly List<Vector3> _positions = new List<Vector3>();
        private bool[] _passed;
        private Material _dim;
        private Material _lit;
        private Texture2D _texture;
        private GameObject _container;
        private bool _shown;

        public bool IsShown { get { return _shown; } }

        public void Show()
        {
            Build();
            if (_container == null) return;
            for (int i = 0; i < _arrows.Count; i++)
            {
                _passed[i] = false;
                _arrows[i].enabled = true;
            }
            _container.SetActive(true);
            _shown = true;
        }

        public void Hide()
        {
            _shown = false;
            if (_container != null) _container.SetActive(false);
        }

        private void Update()
        {
            if (!_shown || _arrows.Count == 0) return;
            PlayerRig rig = PlayerRig.Instance;
            if (rig == null || rig.Head == null) return;

            Vector3 feet = rig.FeetPosition;
            int n = _arrows.Count;
            int lit = Mathf.FloorToInt(Time.time * sweepSpeed) % n;
            float hideSqr = hideRadius * hideRadius;

            for (int i = 0; i < n; i++)
            {
                if (_passed[i]) continue;
                Vector3 d = _positions[i] - feet;
                d.y = 0f;
                if (d.sqrMagnitude < hideSqr)
                {
                    _passed[i] = true;
                    _arrows[i].enabled = false;
                    continue;
                }
                bool isLit = ((i - lit + n) % n) < 2;
                Material wanted = isLit ? _lit : _dim;
                if (_arrows[i].sharedMaterial != wanted) _arrows[i].sharedMaterial = wanted;
            }
        }

        private void Build()
        {
            if (_container != null) return;
            if (waypoints == null || waypoints.Length < 2 || material == null)
            {
                Debug.LogError("[Level1] GuidePath on '" + name + "' needs at least 2 Waypoints and a Material.", this);
                return;
            }

            _container = new GameObject("GuideArrows (runtime)");
            _container.transform.SetParent(transform, false);

            _texture = MakeChevronTexture();
            _dim = MaterialUtil.Clone(material);
            MaterialUtil.SetColor(_dim, dimColor);
            MaterialUtil.SetTexture(_dim, _texture);
            _lit = MaterialUtil.Clone(material);
            MaterialUtil.SetColor(_lit, litColor);
            MaterialUtil.SetTexture(_lit, _texture);

            for (int s = 0; s < waypoints.Length - 1; s++)
            {
                if (waypoints[s] == null || waypoints[s + 1] == null) continue;
                Vector3 a = waypoints[s].position;
                Vector3 b = waypoints[s + 1].position;
                Vector3 flat = b - a;
                flat.y = 0f;
                float length = flat.magnitude;
                if (length < 0.1f) continue;

                Vector3 dir = flat / length;
                float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                int count = Mathf.Max(1, Mathf.FloorToInt(length / Mathf.Max(0.3f, spacing)));
                for (int i = 0; i < count; i++)
                {
                    float t = (i + 0.5f) / count;
                    Vector3 pos = Vector3.Lerp(a, b, t);
                    pos.y += floorOffset;
                    CreateArrow(pos, yaw);
                }
            }

            _passed = new bool[_arrows.Count];
            _container.SetActive(false);
        }

        private void CreateArrow(Vector3 position, float yaw)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Arrow";
            Collider col = quad.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Transform t = quad.transform;
            t.SetParent(_container.transform, true);
            t.position = position;
            t.rotation = Quaternion.Euler(90f, yaw, 0f);
            t.localScale = new Vector3(arrowSize, arrowSize, 1f);

            Renderer r = quad.GetComponent<Renderer>();
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.sharedMaterial = _dim;
            _arrows.Add(r);
            _positions.Add(position);
        }

        private static Texture2D MakeChevronTexture()
        {
            const int n = 64;
            Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            Color32[] pixels = new Color32[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n * 2f - 1f;
                    float v = (y + 0.5f) / n * 2f - 1f;
                    float d = v - (0.7f - Mathf.Abs(u) * 1.1f);
                    float band = Mathf.Clamp01((0.22f - Mathf.Abs(d)) / 0.06f);
                    float side = Mathf.Clamp01((0.85f - Mathf.Abs(u)) / 0.06f);
                    byte alpha = (byte)(band * side * 255f);
                    pixels[y * n + x] = new Color32(255, 255, 255, alpha);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return tex;
        }

        private void OnDestroy()
        {
            if (_dim != null) Destroy(_dim);
            if (_lit != null) Destroy(_lit);
            if (_texture != null) Destroy(_texture);
        }

        private void OnDrawGizmosSelected()
        {
            if (waypoints == null) return;
            Gizmos.color = Color.green;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null) continue;
                Gizmos.DrawWireSphere(waypoints[i].position + Vector3.up * 0.05f, 0.2f);
                if (i + 1 < waypoints.Length && waypoints[i + 1] != null)
                    Gizmos.DrawLine(waypoints[i].position + Vector3.up * 0.05f, waypoints[i + 1].position + Vector3.up * 0.05f);
            }
        }

        public void Validate(ValidationReport report)
        {
            report.Require(material, "Material", this);
            if (waypoints == null || waypoints.Length < 2) report.Error("Needs at least 2 Waypoints.", this);
            else
                for (int i = 0; i < waypoints.Length; i++)
                    if (waypoints[i] == null) report.Error("Waypoints[" + i + "] is empty.", this);
        }
    }
}
