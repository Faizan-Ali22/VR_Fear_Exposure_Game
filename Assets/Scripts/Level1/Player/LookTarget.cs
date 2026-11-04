using System;
using UnityEngine;
using UnityEngine.Events;

namespace FearExposure.Level1
{
    /// <summary>
    /// Something the player must LOOK at for a moment ("identify the building", "find the red object").
    /// Counts time while the head points within Cone Angle of the target, then fires Completed.
    /// A step arms the target when it wants it to count. Optional Gate Zone: only counts
    /// while the player stands inside that zone.
    /// </summary>
    public class LookTarget : MonoBehaviour
    {
        [Tooltip("Point to look at. Empty = this object.")]
        [SerializeField] private Transform aimPoint;
        [SerializeField] private float dwellSeconds = 1.5f;
        [Tooltip("How far (degrees) the head direction may be from the target. Bigger = easier.")]
        [SerializeField] private float coneAngle = 6f;
        [SerializeField] private float maxDistance = 1000f;
        [Tooltip("Optional: the target only counts while the player stands in this zone.")]
        [SerializeField] private ZoneVolume gateZone;
        [Tooltip("Off = progress drains slowly when looking away (forgiving). On = progress resets.")]
        [SerializeField] private bool resetWhenLookingAway;

        public UnityEvent onCompleted;

        public event Action<LookTarget> Completed;
        public event Action<float> ProgressChanged;
        public event Action<bool> GazeStateChanged;

        public bool IsArmed { get; private set; }
        public bool IsCompleted { get; private set; }
        public float Progress { get; private set; }
        public float DwellSeconds => dwellSeconds;
        public float ConeAngle => coneAngle;
        public float MaxDistance => maxDistance;
        public ZoneVolume GateZone => gateZone;
        public float CurrentSeconds => _seconds;
        public float CurrentAngle { get; private set; }
        public bool IsCurrentlyGazed { get; private set; }
        public bool IsInGateZone => gateZone == null || gateZone.PlayerInside;

        private float _seconds;

        public void Arm()
        {
            if (!IsCompleted) IsArmed = true;
        }

        public void Disarm()
        {
            IsArmed = false;
            SetGazed(false);
        }

        public void ResetTarget()
        {
            IsArmed = false;
            IsCompleted = false;
            Progress = 0f;
            _seconds = 0f;
            SetGazed(false);
        }

        private void OnDisable()
        {
            IsArmed = false;
            SetGazed(false);
        }

        private void SetGazed(bool gazed)
        {
            if (IsCurrentlyGazed != gazed)
            {
                IsCurrentlyGazed = gazed;
                GazeStateChanged?.Invoke(gazed);
            }
        }

        private void Update()
        {
            if (!IsArmed || IsCompleted)
            {
                SetGazed(false);
                return;
            }
            PlayerRig rig = PlayerRig.Instance;
            if (rig == null || rig.Head == null)
            {
                SetGazed(false);
                return;
            }

            Transform head = rig.Head;
            Transform aim = aimPoint != null ? aimPoint : transform;
            Vector3 to = aim.position - head.position;
            CurrentAngle = Vector3.Angle(head.forward, to);

            bool gazed = IsLookedAt(rig);
            SetGazed(gazed);

            if (gazed) _seconds += Time.deltaTime;
            else if (resetWhenLookingAway) _seconds = 0f;
            else _seconds = Mathf.Max(0f, _seconds - Time.deltaTime * 0.5f);

            float p = Mathf.Clamp01(_seconds / Mathf.Max(0.05f, dwellSeconds));
            if (!Mathf.Approximately(p, Progress))
            {
                Progress = p;
                ProgressChanged?.Invoke(p);
            }

            if (p >= 1f)
            {
                IsCompleted = true;
                IsArmed = false;
                SetGazed(false);
                Completed?.Invoke(this);
                onCompleted?.Invoke();
            }
        }

        public bool IsLookedAt(PlayerRig rig)
        {
            if (gateZone != null && !gateZone.PlayerInside) return false;
            Transform head = rig.Head;
            Transform aim = aimPoint != null ? aimPoint : transform;
            Vector3 to = aim.position - head.position;
            float dist = to.magnitude;
            if (dist < 0.01f || dist > maxDistance) return false;
            return Vector3.Angle(head.forward, to) <= coneAngle;
        }

        private void OnDrawGizmos()
        {
            Transform aim = aimPoint != null ? aimPoint : transform;
            Gizmos.color = IsCompleted ? Color.green : new Color(1f, 0.85f, 0.1f, 1f);
            Gizmos.DrawWireSphere(aim.position, 0.6f);
        }
    }
}
