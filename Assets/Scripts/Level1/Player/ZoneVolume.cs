using System;
using UnityEngine;
using UnityEngine.Events;

namespace FearExposure.Level1
{
    /// <summary>
    /// An invisible box that detects when the player stands inside it.
    /// No collider, no Rigidbody, no tags and no layers are involved, so it cannot be
    /// broken by physics settings. It only works while this component is enabled.
    /// Select the object to see the box in the Scene view (green wire cube).
    /// </summary>
    [DisallowMultipleComponent]
    public class ZoneVolume : MonoBehaviour
    {
        [Tooltip("Box centre, in this object's local space. Y = 1 puts the box on the floor.")]
        [SerializeField] private Vector3 center = new Vector3(0f, 1f, 0f);
        [Tooltip("Box size in metres (multiplied by this object's scale).")]
        [SerializeField] private Vector3 size = new Vector3(2f, 2f, 2f);
        [SerializeField] private float checkInterval = 0.1f;
        [SerializeField] private Color gizmoColor = new Color(0.2f, 0.9f, 0.4f, 1f);

        public UnityEvent onPlayerEntered;
        public UnityEvent onPlayerExited;

        public event Action Entered;
        public event Action Exited;

        public bool PlayerInside { get; private set; }
        public Vector3 Size => size;
        public Vector3 Center => center;

        private float _nextCheck;

        private void OnEnable()
        {
            PlayerInside = false;
            _nextCheck = 0f;
        }

        private void Update()
        {
            if (Time.time < _nextCheck) return;
            _nextCheck = Time.time + checkInterval;

            PlayerRig rig = PlayerRig.Instance;
            if (rig == null || rig.Head == null) return;

            bool inside = Contains(rig.BodyPosition);
            if (inside == PlayerInside) return;
            PlayerInside = inside;

            if (inside)
            {
                if (Entered != null) Entered();
                if (onPlayerEntered != null) onPlayerEntered.Invoke();
            }
            else
            {
                if (Exited != null) Exited();
                if (onPlayerExited != null) onPlayerExited.Invoke();
            }
        }

        public bool Contains(Vector3 worldPoint)
        {
            Vector3 local = transform.InverseTransformPoint(worldPoint) - center;
            Vector3 half = size * 0.5f;
            return Mathf.Abs(local.x) <= half.x
                && Mathf.Abs(local.y) <= half.y
                && Mathf.Abs(local.z) <= half.z;
        }

        private void OnDrawGizmos()
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireCube(center, size);
            Color fill = gizmoColor;
            fill.a = PlayerInside ? 0.25f : 0.06f;
            Gizmos.color = fill;
            Gizmos.DrawCube(center, size);
        }
    }
}
