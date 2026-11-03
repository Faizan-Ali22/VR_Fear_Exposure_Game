using UnityEngine;

namespace FearExposure.Level1
{
    /// <summary>
    /// STEP 1 - Safe Room (0-2 min).
    /// The player spawns in a calm room. A safety board explains the height exposure and the
    /// guide asks them to look around. When they have looked around (and read the board), they
    /// are invited to walk to the large window. Entering the window zone ends the step.
    /// </summary>
    public class SafeRoomStep : LevelStep
    {
        [Header("Scene references")]
        [Tooltip("LookTarget on the safety board: the player must look at it for a moment (= reading it). Optional.")]
        [SerializeField] private LookTarget safetyBoardLook;
        [Tooltip("Big zone in front of the large window. Entering it ends this step.")]
        [SerializeField] private ZoneVolume windowApproachZone;

        [Header("Look-around rule")]
        [Tooltip("The compass is split into 8 sectors of 45 degrees. The player must face this many of them.")]
        [SerializeField, Range(3, 8)] private int sectorsRequired = 5;

        [Header("Reward")]
        [SerializeField] private int courageReward = 5;

        [Header("Messages")]
        [SerializeField, TextArea] private string welcomeMessage = "Welcome. You are in a safe room - nothing here can hurt you. Look around and read the safety board. There is no rush.";
        [SerializeField, TextArea] private string boardMessage = "Take a moment to read the safety board.";
        [SerializeField, TextArea] private string walkMessage = "When you feel ready, walk toward the large window.";

        private int _seenMask;
        private bool _boardRead;
        private bool _lookedAround;
        private bool _boardPrompted;
        private bool _walkPhase;

        protected override void OnBegin()
        {
            _seenMask = 0;
            _lookedAround = false;
            _boardPrompted = false;
            _walkPhase = false;
            _boardRead = safetyBoardLook == null;

            Say(welcomeMessage);
            if (safetyBoardLook != null)
            {
                safetyBoardLook.ResetTarget();
                safetyBoardLook.Completed += OnBoardRead;
                safetyBoardLook.Arm();
            }
        }

        private void OnBoardRead(LookTarget target)
        {
            _boardRead = true;
        }

        private void Update()
        {
            if (!IsRunning || _walkPhase) return;
            PlayerRig rig = PlayerRig.Instance;
            if (rig == null || rig.Head == null) return;

            int sector = Mathf.FloorToInt(Mathf.Repeat(rig.HeadYaw + 22.5f, 360f) / 45f) % 8;
            _seenMask |= 1 << sector;

            if (!_lookedAround && CountBits(_seenMask) >= sectorsRequired) _lookedAround = true;

            if (_lookedAround && !_boardRead && !_boardPrompted)
            {
                _boardPrompted = true;
                Say(boardMessage);
            }
            if (_lookedAround && _boardRead) EnterWalkPhase();
        }

        private void EnterWalkPhase()
        {
            _walkPhase = true;
            Say(walkMessage);
            windowApproachZone.Entered += OnApproachedWindow;
            if (windowApproachZone.PlayerInside) OnApproachedWindow();
        }

        private void OnApproachedWindow()
        {
            if (!IsRunning || !_walkPhase) return;
            Award(courageReward, "Settled into the safe room");
            Finish();
        }

        protected override void OnFinished()
        {
            if (safetyBoardLook != null)
            {
                safetyBoardLook.Completed -= OnBoardRead;
                safetyBoardLook.Disarm();
            }
            if (windowApproachZone != null) windowApproachZone.Entered -= OnApproachedWindow;
        }

        private static int CountBits(int value)
        {
            int count = 0;
            while (value != 0)
            {
                count += value & 1;
                value >>= 1;
            }
            return count;
        }

        public override void Validate(ValidationReport report)
        {
            report.Require(windowApproachZone, "Window Approach Zone", this);
            if (safetyBoardLook == null)
                report.Warn("Safety Board Look is empty - the player will not be required to read the board.", this);
        }
    }
}
