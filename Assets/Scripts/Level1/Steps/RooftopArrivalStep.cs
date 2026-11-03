using UnityEngine;

namespace FearExposure.Level1
{
    /// <summary>
    /// STEP 5 - Rooftop Arrival (5-6 min).
    /// The doors are open. The player steps out onto the rooftop observation area and explores at their
    /// own pace (the rest area is always available). Looking out over the wide city view ends the step
    /// once they have spent a minimum time on the terrace.
    /// </summary>
    public class RooftopArrivalStep : LevelStep
    {
        [Header("Scene references")]
        [Tooltip("Zone covering the open terrace just outside the elevator.")]
        [SerializeField] private ZoneVolume terraceZone;
        [Tooltip("LookTarget placed out over the city. Use a big Cone Angle (12-20) and Dwell Seconds 3.")]
        [SerializeField] private LookTarget cityViewTarget;

        [Header("Pacing and reward")]
        [SerializeField] private float minSecondsOnTerrace = 10f;
        [SerializeField] private int courageReward = 10;

        [Header("Messages")]
        [SerializeField, TextArea] private string arrivalMessage = "The doors are open. Step out onto the rooftop whenever you like and explore at your own pace. There is a rest area with benches if you need a break.";
        [SerializeField, TextArea] private string lookMessage = "Take your time. Look out over the city.";
        [SerializeField, TextArea] private string doneMessage = "Nicely done. A mission is waiting whenever you are ready.";

        private bool _onTerrace;
        private bool _viewed;
        private bool _finishing;
        private float _enteredAt;

        protected override void OnBegin()
        {
            _onTerrace = false;
            _viewed = false;
            _finishing = false;

            cityViewTarget.ResetTarget();
            Say(arrivalMessage);
            terraceZone.Entered += OnTerraceEntered;
            cityViewTarget.Completed += OnViewed;
        }

        private void OnTerraceEntered()
        {
            if (_onTerrace) return;
            _onTerrace = true;
            _enteredAt = Time.time;
            Say(lookMessage);
            cityViewTarget.Arm();
        }

        private void OnViewed(LookTarget target)
        {
            if (_viewed) return;
            _viewed = true;
            Award(courageReward, "Looked out over the city");
            if (PlayerRig.Instance != null) PlayerRig.Instance.Pulse(0.3f, 0.12f);
        }

        private void Update()
        {
            if (!IsRunning || !_viewed || _finishing) return;
            if (Time.time - _enteredAt < minSecondsOnTerrace) return;
            _finishing = true;
            Say(doneMessage, 5f);
            Finish();
        }

        protected override void OnFinished()
        {
            if (terraceZone != null) terraceZone.Entered -= OnTerraceEntered;
            if (cityViewTarget != null)
            {
                cityViewTarget.Completed -= OnViewed;
                cityViewTarget.Disarm();
            }
        }

        public override void Validate(ValidationReport report)
        {
            report.Require(terraceZone, "Terrace Zone", this);
            report.Require(cityViewTarget, "City View Target", this);
        }
    }
}
