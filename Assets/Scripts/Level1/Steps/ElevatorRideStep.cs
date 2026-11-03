using UnityEngine;

namespace FearExposure.Level1
{
    /// <summary>
    /// STEP 4 - Elevator Exposure (4-5 min).
    /// The player enters the cab and presses the glowing rooftop button (the other floor buttons stay
    /// locked in Level 1). The elevator rises slowly; the player may look through the glass or face
    /// the door. The step ends when the doors open at the destination.
    /// </summary>
    public class ElevatorRideStep : LevelStep
    {
        [Header("Scene references")]
        [SerializeField] private ElevatorController elevator;
        [Tooltip("Zone covering the floor inside the cab.")]
        [SerializeField] private ZoneVolume cabZone;
        [Tooltip("ALL floor buttons on the cab panel, in any order. Each needs a WorldButton component.")]
        [SerializeField] private WorldButton[] panelButtons;
        [Tooltip("Which entry of Panel Buttons is the rooftop / Sky Terrace button (first entry = 0).")]
        [SerializeField] private int destinationButtonIndex = 2;

        [Header("Reward")]
        [SerializeField] private int courageReward = 10;

        [Header("Messages")]
        [SerializeField, TextArea] private string enterMessage = "Step inside the elevator when you are ready.";
        [SerializeField, TextArea] private string pressMessage = "Press the glowing button to travel up to the rooftop.";
        [SerializeField, TextArea] private string stepInsideMessage = "Step fully inside the elevator first.";
        [SerializeField, TextArea] private string rideMessage = "The elevator will rise slowly. You can look out through the glass, or face the door - your choice.";
        [SerializeField, TextArea] private string arrivedMessage = "You have arrived. Take a slow breath.";

        private WorldButton _destination;
        private bool _inside;
        private bool _riding;

        protected override void OnBegin()
        {
            _inside = false;
            _riding = false;

            for (int i = 0; i < panelButtons.Length; i++)
                if (panelButtons[i] != null) panelButtons[i].SetInteractable(false);

            _destination = panelButtons[destinationButtonIndex];
            Say(enterMessage);

            cabZone.Entered += OnCabEntered;
            _destination.Pressed += OnDestinationPressed;
            elevator.RideFinished += OnRideFinished;
        }

        private void OnCabEntered()
        {
            if (_inside || _riding) return;
            _inside = true;
            Say(pressMessage);
            _destination.SetInteractable(true);
            _destination.SetAttention(true);
        }

        private void OnDestinationPressed()
        {
            if (_riding || !_inside) return;
            if (!cabZone.PlayerInside)
            {
                Say(stepInsideMessage);
                return;
            }
            _riding = true;
            _destination.SetAttention(false);
            _destination.SetInteractable(false);
            Say(rideMessage);
            elevator.StartRide();
        }

        private void OnRideFinished()
        {
            if (!_riding) return;
            Award(courageReward, "Rode the elevator to the rooftop");
            Say(arrivedMessage, 6f);
            Finish();
        }

        protected override void OnFinished()
        {
            if (cabZone != null) cabZone.Entered -= OnCabEntered;
            if (_destination != null)
            {
                _destination.Pressed -= OnDestinationPressed;
                _destination.SetAttention(false);
                _destination.SetInteractable(false);
            }
            if (elevator != null) elevator.RideFinished -= OnRideFinished;
        }

        public override void Validate(ValidationReport report)
        {
            report.Require(elevator, "Elevator", this);
            report.Require(cabZone, "Cab Zone", this);
            if (elevator != null) elevator.Validate(report);

            if (panelButtons == null || panelButtons.Length == 0)
            {
                report.Error("Panel Buttons is empty.", this);
                return;
            }
            for (int i = 0; i < panelButtons.Length; i++)
            {
                if (panelButtons[i] == null) report.Error("Panel Buttons[" + i + "] is empty.", this);
                else panelButtons[i].Validate(report);
            }
            if (destinationButtonIndex < 0 || destinationButtonIndex >= panelButtons.Length)
                report.Error("Destination Button Index (" + destinationButtonIndex + ") is outside the Panel Buttons list.", this);
        }
    }
}
