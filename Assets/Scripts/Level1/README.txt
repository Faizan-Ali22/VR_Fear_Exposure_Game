FearExposure.Level1 - VR Fear Exposure Game, Level 1 scripts (30 files)

INSTALL
1. Extract this zip into your Unity project ROOT (the folder that contains Assets/). Unity merges it into Assets/Scripts/Level1.
2. Wait for the compile spinner. The Console must show no red errors.
3. Follow the build guide (Claude Doc "Level 1 - Complete Build Guide") from Part 3 onward.

FOLDERS
Core/         Level1Director, LevelStep, CourageScore, LevelProgress, ValidationReport
Player/       PlayerRig, ZoneVolume, LookTarget, ComfortReturn
UI/           InstructionPanel, FollowPanel, FadeOverlay, ObjectiveListPanel, CompletionScreen
FX/           MarkerVisual, GuidePath, MaterialUtil
Interaction/  WorldButton, SlidingDoor, ElevatorController, Collectible, RestArea
Audio/        AmbienceController
Steps/        SafeRoomStep, WindowObservationStep, HallwayStep, ElevatorRideStep,
              RooftopArrivalStep, FindTargetsStep, FinalObservationStep

Everything lives in the namespace FearExposure.Level1, so nothing clashes with your existing scripts.
Requires: Unity 6 (URP), XR Interaction Toolkit 3.x, Input System, TextMeshPro (all already in your project).

DEV KEYS (Editor / Development Build)   F6 = skip the current step   |   H (hold) = comfort return
Right-click Level1Director > "Validate Setup" lists every missing reference in one go.
