using System.Collections;
using UnityEngine;

namespace FearExposure.Level1
{
    /// <summary>
    /// STEP 2 - First Height Observation (2-3 min).
    /// A glowing marker appears by the large window. Standing near it, the player is asked to look
    /// outside and identify the highlighted building (a glowing beacon). No need to go outside.
    /// </summary>
    public class WindowObservationStep : LevelStep
    {
        [Header("Scene references")]
        [Tooltip("Small zone around the glowing marker, close to the window.")]
        [SerializeField] private ZoneVolume standZone;
        [Tooltip("FloorRing marker standing on the floor at the window.")]
        [SerializeField] private MarkerVisual windowMarker;
        [Tooltip("LookTarget on the highlighted building. Set its Gate Zone to the Stand Zone.")]
        [SerializeField] private LookTarget buildingTarget;
        [Tooltip("Beam marker above the highlighted building.")]
        [SerializeField] private MarkerVisual buildingBeacon;

        [Header("Reward and hints")]
        [SerializeField] private int courageReward = 10;
        [SerializeField] private float hintAfterSeconds = 40f;

        [Header("Messages")]
        [SerializeField, TextArea] private string walkMessage = "Walk to the glowing marker beside the window.";
        [SerializeField, TextArea] private string lookMessage = "Stand comfortably and look outside. Find the building with the glowing beacon across the street.";
        [SerializeField, TextArea] private string hintMessage = "Take your time. Look slightly downward across the street - the beacon is a tall glowing light.";
        [SerializeField, TextArea] private string doneMessage = "Well done. You found it, and you are doing just fine.";

        private bool _lookPhase;
        private bool _spotted;
        private bool _hinted;
        private float _lookPhaseStart;
        private Color _beaconBaseColor = new Color(255f/255f, 220f/255f, 60f/255f, 160f/255f);
        private Color _beaconGazedColor = new Color(80f/255f, 255f/255f, 150f/255f, 240f/255f);

        protected override void OnBegin()
        {
            _lookPhase = false;
            _spotted = false;
            _hinted = false;

            // Automatically sync floor ring marker position with stand zone
            if (windowMarker != null && standZone != null)
            {
                windowMarker.transform.position = standZone.transform.position;
            }

            buildingTarget.ResetTarget();
            windowMarker.Show();
            Say(walkMessage);

            standZone.Entered += OnStandZoneEntered;
            buildingTarget.Completed += OnSpotted;
            if (standZone.PlayerInside) OnStandZoneEntered();
        }

        private void OnStandZoneEntered()
        {
            if (_spotted || _lookPhase) return;
            _lookPhase = true;
            _lookPhaseStart = Time.time;
            Say(lookMessage);
            buildingBeacon.Show();
            buildingBeacon.SetColor(_beaconBaseColor);
            buildingTarget.Arm();
        }

        private void Update()
        {
            if (!IsRunning || !_lookPhase || _spotted) return;

            // Visual feedback on beacon when player is locking gaze on it
            if (buildingBeacon != null && buildingTarget != null)
            {
                if (buildingTarget.IsCurrentlyGazed)
                {
                    buildingBeacon.SetColor(_beaconGazedColor);
                }
                else
                {
                    buildingBeacon.SetColor(_beaconBaseColor);
                }
            }

            if (!_hinted && Time.time - _lookPhaseStart >= hintAfterSeconds)
            {
                _hinted = true;
                Say(hintMessage);
            }
        }

        private void OnSpotted(LookTarget target)
        {
            if (_spotted) return;
            _spotted = true;
            Award(courageReward, "Spotted the highlighted building");
            if (PlayerRig.Instance != null) PlayerRig.Instance.Pulse(0.3f, 0.12f);
            Say(doneMessage, 5f);
            StartCoroutine(FinishAfter(3f));
        }

        private IEnumerator FinishAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            Finish();
        }

        protected override void OnFinished()
        {
            if (standZone != null) standZone.Entered -= OnStandZoneEntered;
            if (buildingTarget != null)
            {
                buildingTarget.Completed -= OnSpotted;
                buildingTarget.Disarm();
            }
            if (windowMarker != null) windowMarker.Hide();
            if (buildingBeacon != null) buildingBeacon.Hide();
        }

        public override void Validate(ValidationReport report)
        {
            report.Require(standZone, "Stand Zone", this);
            report.Require(windowMarker, "Window Marker", this);
            report.Require(buildingTarget, "Building Target", this);
            report.Require(buildingBeacon, "Building Beacon", this);
            if (windowMarker != null) windowMarker.Validate(report);
            if (buildingBeacon != null) buildingBeacon.Validate(report);
        }
    }
}
