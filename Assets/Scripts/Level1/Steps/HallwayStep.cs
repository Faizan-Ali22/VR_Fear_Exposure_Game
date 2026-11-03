using System.Collections;
using UnityEngine;

namespace FearExposure.Level1
{
    /// <summary>
    /// STEP 3 - Explore the Room and Hallway (3-4 min).
    /// A door opens to the hallway. Floor arrows lead to the elevator. Near the elevator the player
    /// presses the small call button, which opens the elevator doors.
    /// </summary>
    public class HallwayStep : LevelStep
    {
        [Header("Scene references")]
        [Tooltip("The door between the room and the hallway.")]
        [SerializeField] private SlidingDoor roomDoor;
        [Tooltip("Floor arrows from the room to the elevator.")]
        [SerializeField] private GuidePath floorGuide;
        [Tooltip("Zone in front of the elevator doors.")]
        [SerializeField] private ZoneVolume elevatorLobbyZone;
        [Tooltip("The small button beside the elevator door.")]
        [SerializeField] private WorldButton callButton;
        [SerializeField] private ElevatorController elevator;

        [Header("Reward")]
        [SerializeField] private int courageReward = 5;

        [Header("Messages")]
        [SerializeField, TextArea] private string doorMessage = "A door has opened. Follow the glowing arrows on the floor to find the elevator.";
        [SerializeField, TextArea] private string foundMessage = "You found the elevator. Press the small button beside the door.";
        [SerializeField, TextArea] private string openedMessage = "The elevator is here. Step inside when you are ready.";

        private bool _found;
        private bool _pressed;

        protected override void OnBegin()
        {
            _found = false;
            _pressed = false;

            roomDoor.Open();
            floorGuide.Show();
            callButton.SetInteractable(false);
            Say(doorMessage);

            elevatorLobbyZone.Entered += OnLobbyEntered;
            callButton.Pressed += OnCallPressed;
        }

        private void OnLobbyEntered()
        {
            if (_found) return;
            _found = true;
            Say(foundMessage);
            callButton.SetInteractable(true);
            callButton.SetAttention(true);
        }

        private void OnCallPressed()
        {
            if (!_found || _pressed) return;
            _pressed = true;
            callButton.SetAttention(false);
            callButton.SetInteractable(false);
            Award(courageReward, "Found the elevator");
            elevator.OpenCabDoor();
            StartCoroutine(FinishWhenDoorOpen());
        }

        private IEnumerator FinishWhenDoorOpen()
        {
            float waited = 0f;
            while (!elevator.CabDoorOpen && waited < 8f)
            {
                waited += Time.deltaTime;
                yield return null;
            }
            Say(openedMessage);
            Finish();
        }

        protected override void OnSkip()
        {
            if (roomDoor != null) roomDoor.Open();
            if (elevator != null) elevator.OpenCabDoor();
        }

        protected override void OnFinished()
        {
            if (elevatorLobbyZone != null) elevatorLobbyZone.Entered -= OnLobbyEntered;
            if (callButton != null)
            {
                callButton.Pressed -= OnCallPressed;
                callButton.SetAttention(false);
                callButton.SetInteractable(false);
            }
            if (floorGuide != null) floorGuide.Hide();
        }

        public override void Validate(ValidationReport report)
        {
            report.Require(roomDoor, "Room Door", this);
            report.Require(floorGuide, "Floor Guide", this);
            report.Require(elevatorLobbyZone, "Elevator Lobby Zone", this);
            report.Require(callButton, "Call Button", this);
            report.Require(elevator, "Elevator", this);
            if (roomDoor != null) roomDoor.Validate(report);
            if (floorGuide != null) floorGuide.Validate(report);
            if (callButton != null) callButton.Validate(report);
        }
    }
}
