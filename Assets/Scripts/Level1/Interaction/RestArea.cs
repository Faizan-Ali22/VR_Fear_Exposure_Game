using UnityEngine;

namespace FearExposure.Level1
{
    /// <summary>
    /// The "safe resting area" (the benches on the terrace). While the player stands inside the zone,
    /// a calming message appears and an optional breathing orb pulses (4 s in, 6 s out).
    /// Keep this object ACTIVE in the scene for the whole level (do not put it under a step).
    /// </summary>
    public class RestArea : MonoBehaviour
    {
        [SerializeField] private ZoneVolume zone;
        [Tooltip("Optional sphere that grows and shrinks to guide slow breathing.")]
        [SerializeField] private Transform breathingOrb;
        [SerializeField] private float orbMinScale = 0.12f;
        [SerializeField] private float orbMaxScale = 0.32f;
        [SerializeField, TextArea] private string enterMessage = "This is a safe resting area. Sit or stand here and breathe slowly for as long as you like. Continue whenever you feel ready.";
        [SerializeField] private float messageCooldownSeconds = 30f;

        private float _lastMessageTime = -999f;

        private void OnEnable()
        {
            if (zone != null)
            {
                zone.Entered += HandleEntered;
                zone.Exited += HandleExited;
            }
            if (breathingOrb != null) breathingOrb.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            if (zone != null)
            {
                zone.Entered -= HandleEntered;
                zone.Exited -= HandleExited;
            }
        }

        private void HandleEntered()
        {
            if (breathingOrb != null) breathingOrb.gameObject.SetActive(true);
            if (Time.time - _lastMessageTime < messageCooldownSeconds) return;
            _lastMessageTime = Time.time;
            Level1Director director = Level1Director.Current;
            if (director != null && director.Instructions != null) director.Instructions.Show(enterMessage, 10f);
        }

        private void HandleExited()
        {
            if (breathingOrb != null) breathingOrb.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (breathingOrb == null || zone == null || !zone.PlayerInside) return;
            float cycle = Mathf.Repeat(Time.time, 10f);
            float k = cycle < 4f
                ? Mathf.SmoothStep(0f, 1f, cycle / 4f)
                : 1f - Mathf.SmoothStep(0f, 1f, (cycle - 4f) / 6f);
            breathingOrb.localScale = Vector3.one * Mathf.Lerp(orbMinScale, orbMaxScale, k);
        }

        public void Validate(ValidationReport report)
        {
            report.Require(zone, "Zone", this);
        }
    }
}
