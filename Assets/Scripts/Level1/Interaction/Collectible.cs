using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace FearExposure.Level1
{
    /// <summary>
    /// A small floating pickup (beacon / paper plane). Collected when the player's head or a hand
    /// gets close. Awards Courage Points. Put the visual (any mesh) as this object or as a child.
    /// </summary>
    public class Collectible : MonoBehaviour
    {
        [Tooltip("The thing that spins and bobs. Empty = this object.")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private float headPickupRadius = 1.1f;
        [SerializeField] private float handPickupRadius = 0.3f;
        [SerializeField] private int courageReward = 5;
        [SerializeField] private string rewardReason = "Collected a beacon";
        [SerializeField] private float bobHeight = 0.06f;
        [SerializeField] private float bobSpeed = 1.6f;
        [SerializeField] private float spinSpeed = 70f;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip collectClip;

        public UnityEvent onCollected;
        public event Action<Collectible> Collected;

        public bool IsCollected { get; private set; }

        private Vector3 _baseLocalPos;
        private Vector3 _baseScale;
        private float _nextCheck;
        private bool _collecting;

        private void Awake()
        {
            if (visualRoot == null) visualRoot = transform;
            _baseLocalPos = visualRoot.localPosition;
            _baseScale = visualRoot.localScale;
        }

        private void Update()
        {
            if (IsCollected || _collecting) return;

            visualRoot.localPosition = _baseLocalPos + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
            visualRoot.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.Self);

            if (Time.time < _nextCheck) return;
            _nextCheck = Time.time + 0.1f;

            PlayerRig rig = PlayerRig.Instance;
            if (rig == null || rig.Head == null) return;
            if (rig.IsWithinReach(visualRoot.position, headPickupRadius, handPickupRadius))
                StartCoroutine(CollectRoutine());
        }

        private IEnumerator CollectRoutine()
        {
            _collecting = true;
            IsCollected = true;

            if (audioSource != null && collectClip != null) audioSource.PlayOneShot(collectClip);
            Level1Director director = Level1Director.Current;
            if (director != null && director.Courage != null) director.Courage.Award(courageReward, rewardReason);
            if (PlayerRig.Instance != null) PlayerRig.Instance.Pulse(0.3f, 0.1f);

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.25f;
                visualRoot.localScale = _baseScale * (1f - Mathf.Clamp01(t));
                yield return null;
            }
            visualRoot.localScale = Vector3.zero;

            if (Collected != null) Collected(this);
            if (onCollected != null) onCollected.Invoke();
        }

        public void ResetCollectible()
        {
            IsCollected = false;
            _collecting = false;
            if (visualRoot != null)
            {
                visualRoot.localScale = _baseScale;
                visualRoot.localPosition = _baseLocalPos;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Transform v = visualRoot != null ? visualRoot : transform;
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(v.position, headPickupRadius);
        }
    }
}
