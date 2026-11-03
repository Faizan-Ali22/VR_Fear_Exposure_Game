using UnityEngine;

namespace FearExposure.Level1
{
    /// <summary>
    /// Keeps a world-space canvas in front of the player, comfortably, like a floating tablet.
    /// It only re-centres when you turn well away from it or walk away, then glides back.
    /// Put on a World Space Canvas root.
    /// </summary>
    public class FollowPanel : MonoBehaviour
    {
        [SerializeField] private float distance = 1.8f;
        [Tooltip("Height relative to the eyes. Negative = slightly below eye level (more comfortable).")]
        [SerializeField] private float heightOffset = -0.2f;
        [SerializeField] private float smoothTime = 0.5f;
        [Tooltip("Degrees you can look away before the panel starts moving back to you.")]
        [SerializeField] private float angleDeadZone = 25f;
        [SerializeField] private float distanceTolerance = 0.7f;

        private Vector3 _velocity;
        private bool _recentering;

        private void OnEnable()
        {
            Snap();
        }

        /// <summary>Instantly place the panel in front of the player.</summary>
        public void Snap()
        {
            PlayerRig rig = PlayerRig.Instance;
            if (rig == null || rig.Head == null) return;
            transform.position = TargetPosition(rig.Head);
            FaceHead(rig.Head);
            _velocity = Vector3.zero;
            _recentering = false;
        }

        private Vector3 TargetPosition(Transform head)
        {
            Vector3 fwd = head.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
            else fwd.Normalize();
            return head.position + fwd * distance + Vector3.up * heightOffset;
        }

        private void LateUpdate()
        {
            PlayerRig rig = PlayerRig.Instance;
            if (rig == null || rig.Head == null) return;
            Transform head = rig.Head;

            Vector3 flatToPanel = transform.position - head.position;
            flatToPanel.y = 0f;
            Vector3 flatForward = head.forward;
            flatForward.y = 0f;

            float angle = 0f;
            if (flatToPanel.sqrMagnitude > 0.0001f && flatForward.sqrMagnitude > 0.0001f)
                angle = Vector3.Angle(flatForward, flatToPanel);
            float dist = flatToPanel.magnitude;

            if (angle > angleDeadZone || Mathf.Abs(dist - distance) > distanceTolerance)
                _recentering = true;

            if (_recentering)
            {
                Vector3 target = TargetPosition(head);
                transform.position = Vector3.SmoothDamp(transform.position, target, ref _velocity, smoothTime);
                if (angle < 4f && (target - transform.position).sqrMagnitude < 0.01f)
                    _recentering = false;
            }
            FaceHead(head);
        }

        private void FaceHead(Transform head)
        {
            Vector3 look = transform.position - head.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(look);
        }
    }
}
