using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace FearExposure.Level1
{
    /// <summary>
    /// Runs the elevator ride: doors close, a slow ride with a floor counter and gentle rumble,
    /// doors open at the destination.
    ///
    /// Ride Mode:
    ///   TeleportWithFade (default, works in ANY scene): the cab stays put. Near the end of the ride the
    ///       screen fades out, the player is moved into the arrival cab, and the screen fades back in.
    ///   PhysicalRise: the cab really travels from where it is to Top Anchor with the player riding inside.
    ///       Use only if your cab sits in a free vertical shaft that reaches the rooftop floor.
    /// </summary>
    public class ElevatorController : MonoBehaviour
    {
        public enum RideMode { TeleportWithFade, PhysicalRise }

        [Header("Doors")]
        [Tooltip("Door of the cab the player stands in at the START.")]
        [SerializeField] private SlidingDoor cabDoor;
        [Tooltip("Door the player walks out of at the END. Same object as Cab Door in Physical mode.")]
        [SerializeField] private SlidingDoor arrivalDoor;

        [Header("Ride")]
        [SerializeField] private RideMode mode = RideMode.TeleportWithFade;
        [SerializeField] private float rideSeconds = 24f;
        [SerializeField] private float pauseAfterDoorsClose = 1.5f;
        [SerializeField] private float fadeSeconds = 0.7f;

        [Header("Teleport mode")]
        [Tooltip("Empty object on the FLOOR inside the arrival cab. Its blue arrow shows which way the player will face.")]
        [SerializeField] private Transform arrivalStandPoint;
        [SerializeField] private FadeOverlay fade;

        [Header("Physical mode")]
        [Tooltip("The moving cab. The player is parented to it during the ride.")]
        [SerializeField] private Transform cabRoot;
        [Tooltip("Empty object where Cab Root ends up.")]
        [SerializeField] private Transform topAnchor;

        [Header("Floor counter")]
        [Tooltip("Every display that should show the current floor number (inside the cab, beside the doors).")]
        [SerializeField] private TMP_Text[] floorCounters;
        [SerializeField] private int startFloor = 25;
        [SerializeField] private int endFloor = 50;

        [Header("Sound and feel")]
        [SerializeField] private AudioSource rumbleLoop;
        [SerializeField] private AudioSource dingSource;
        [SerializeField] private float rumbleVolume = 0.35f;
        [SerializeField] private float hapticEverySeconds = 2.5f;

        public event Action RideStarted;
        public event Action RideFinished;

        public bool IsRiding { get; private set; }
        public bool CabDoorOpen { get { return cabDoor != null && cabDoor.IsOpen; } }

        private int _lastFloor = int.MinValue;

        public void OpenCabDoor()
        {
            if (cabDoor != null) cabDoor.Open();
        }

        public void StartRide()
        {
            if (IsRiding) return;
            StartCoroutine(RideRoutine());
        }

        private IEnumerator RideRoutine()
        {
            IsRiding = true;
            PlayerRig rig = PlayerRig.Instance;
            if (RideStarted != null) RideStarted();
            if (rig != null) rig.SetLocomotionEnabled(false);

            // 1. Doors close.
            if (cabDoor != null)
            {
                cabDoor.Close();
                float waited = 0f;
                while (!cabDoor.IsClosed && waited < 6f)
                {
                    waited += Time.deltaTime;
                    yield return null;
                }
            }
            UpdateCounters(startFloor);
            yield return new WaitForSeconds(pauseAfterDoorsClose);

            // 2. The ride.
            bool physical = mode == RideMode.PhysicalRise && cabRoot != null && topAnchor != null;
            Vector3 cabStart = cabRoot != null ? cabRoot.position : Vector3.zero;
            if (physical && rig != null) rig.AttachTo(cabRoot);
            if (rumbleLoop != null)
            {
                rumbleLoop.volume = 0f;
                rumbleLoop.loop = true;
                rumbleLoop.Play();
            }

            float t = 0f;
            float hapticTimer = 0f;
            bool teleported = false;
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(1f, rideSeconds);
                float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                UpdateCounters(Mathf.RoundToInt(Mathf.Lerp(startFloor, endFloor, eased)));

                if (physical) cabRoot.position = Vector3.Lerp(cabStart, topAnchor.position, eased);

                if (rumbleLoop != null)
                    rumbleLoop.volume = rumbleVolume * Mathf.Clamp01(Mathf.Min(t * 6f, (1f - t) * 6f));

                hapticTimer += Time.deltaTime;
                if (hapticTimer >= hapticEverySeconds)
                {
                    hapticTimer = 0f;
                    if (rig != null) rig.Pulse(0.08f, 0.25f);
                }

                if (!physical && !teleported && t >= 0.88f)
                {
                    teleported = true;
                    yield return StartCoroutine(TeleportRoutine(rig));
                }
                yield return null;
            }
            UpdateCounters(endFloor);

            // 3. Arrival.
            if (physical)
            {
                cabRoot.position = topAnchor.position;
                if (rig != null) rig.Detach();
            }
            else if (!teleported)
            {
                yield return StartCoroutine(TeleportRoutine(rig));
            }

            if (rumbleLoop != null) rumbleLoop.Stop();
            if (dingSource != null) dingSource.Play();

            if (arrivalDoor != null)
            {
                arrivalDoor.Open();
                float waited = 0f;
                while (!arrivalDoor.IsOpen && waited < 6f)
                {
                    waited += Time.deltaTime;
                    yield return null;
                }
            }

            if (rig != null) rig.SetLocomotionEnabled(true);
            IsRiding = false;
            if (RideFinished != null) RideFinished();
        }

        private IEnumerator TeleportRoutine(PlayerRig rig)
        {
            if (fade != null) yield return fade.StartFade(1f, fadeSeconds);
            if (rig != null && arrivalStandPoint != null)
                rig.PlaceFeetAt(arrivalStandPoint.position, arrivalStandPoint.eulerAngles.y);
            yield return new WaitForSeconds(0.25f);
            if (fade != null) yield return fade.StartFade(0f, fadeSeconds);
        }

        private void UpdateCounters(int floor)
        {
            if (floor == _lastFloor) return;
            _lastFloor = floor;
            if (floorCounters == null) return;
            string text = floor.ToString();
            for (int i = 0; i < floorCounters.Length; i++)
                if (floorCounters[i] != null) floorCounters[i].text = text;
        }

        private void OnDisable()
        {
            // Never leave the player stuck if this object is switched off mid-ride.
            if (!IsRiding) return;
            IsRiding = false;
            PlayerRig rig = PlayerRig.Instance;
            if (rig != null)
            {
                if (mode == RideMode.PhysicalRise) rig.Detach();
                rig.SetLocomotionEnabled(true);
            }
            if (rumbleLoop != null) rumbleLoop.Stop();
        }

        private void OnDrawGizmosSelected()
        {
            if (arrivalStandPoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(arrivalStandPoint.position + Vector3.up * 0.1f, 0.3f);
                Gizmos.DrawRay(arrivalStandPoint.position + Vector3.up * 0.1f, arrivalStandPoint.forward);
            }
            if (topAnchor != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(topAnchor.position, 0.4f);
            }
        }

        public void Validate(ValidationReport report)
        {
            report.Require(cabDoor, "Cab Door", this);
            report.Require(arrivalDoor, "Arrival Door", this);
            if (cabDoor != null) cabDoor.Validate(report);
            if (arrivalDoor != null && arrivalDoor != cabDoor) arrivalDoor.Validate(report);

            if (mode == RideMode.TeleportWithFade)
            {
                report.Require(arrivalStandPoint, "Arrival Stand Point", this);
                report.Require(fade, "Fade", this);
            }
            else
            {
                report.Require(cabRoot, "Cab Root", this);
                report.Require(topAnchor, "Top Anchor", this);
            }
            if (floorCounters == null || floorCounters.Length == 0)
                report.Warn("No Floor Counters assigned - the ride will have no number display.", this);
        }
    }
}
