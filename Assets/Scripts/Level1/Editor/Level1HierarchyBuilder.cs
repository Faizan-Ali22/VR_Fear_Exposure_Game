#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using FearExposure.Level1;

namespace FearExposure.Level1.Editor
{
    public static class Level1CompleteBuilder
    {
        [MenuItem("Level 1/Run Complete Build and Validate")]
        public static void RunBuildFromMenu()
        {
            ExecuteBuild();
        }

        private static void ExecuteBuild()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== STARTING LEVEL 1 COMPLETE BUILD ===");

            Scene scene = SceneManager.GetActiveScene();
            sb.AppendLine($"Active Scene: {scene.name} ({scene.path})");

            // -------------------------------------------------------------
            // PART 3: Create Materials
            // -------------------------------------------------------------
            sb.AppendLine("\n--- PART 3: Materials ---");
            if (!Directory.Exists("Assets/Materials/Level1"))
            {
                Directory.CreateDirectory("Assets/Materials/Level1");
                AssetDatabase.Refresh();
            }

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader == null) unlitShader = Shader.Find("Unlit/Color");

            Material mFade = GetOrCreateMaterial("Assets/Materials/Level1/M_Fade.mat", unlitShader, true, new Color(0, 0, 0, 1));
            Material mMarker = GetOrCreateMaterial("Assets/Materials/Level1/M_Marker.mat", unlitShader, true, new Color(51f/255f, 230f/255f, 255f/255f, 150f/255f));
            Material mGuideArrow = GetOrCreateMaterial("Assets/Materials/Level1/M_GuideArrow.mat", unlitShader, true, new Color(80f/255f, 255f/255f, 150f/255f, 1));
            Material mPickup = GetOrCreateMaterial("Assets/Materials/Level1/M_Pickup.mat", unlitShader, false, new Color(255f/255f, 200f/255f, 40f/255f, 1));
            Material mRed = GetOrCreateMaterial("Assets/Materials/Level1/M_Red.mat", unlitShader, false, new Color(255f/255f, 60f/255f, 50f/255f, 1));
            Material mGreen = GetOrCreateMaterial("Assets/Materials/Level1/M_Green.mat", unlitShader, false, new Color(60f/255f, 230f/255f, 80f/255f, 1));
            Material mBlue = GetOrCreateMaterial("Assets/Materials/Level1/M_Blue.mat", unlitShader, false, new Color(70f/255f, 130f/255f, 255f/255f, 1));
            Material mButtonDisc = GetOrCreateMaterial("Assets/Materials/Level1/M_ButtonDisc.mat", unlitShader, false, new Color(60f/255f, 60f/255f, 66f/255f, 1));
            sb.AppendLine("Created/verified 8 materials in Assets/Materials/Level1.");

            // -------------------------------------------------------------
            // ROOT: [Level1]
            // -------------------------------------------------------------
            GameObject root = GameObject.Find("[Level1]");
            if (root == null)
            {
                root = new GameObject("[Level1]");
                Undo.RegisterCreatedObjectUndo(root, "Create [Level1]");
            }
            root.transform.position = new Vector3(0, 200, 0);
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            Level1Director director = root.GetComponent<Level1Director>();
            if (director == null) director = root.AddComponent<Level1Director>();

            // -------------------------------------------------------------
            // PART 5: Player Rig
            // -------------------------------------------------------------
            sb.AppendLine("\n--- PART 5: Player Rig ---");
            GameObject xrOriginGo = GameObject.Find("XR Origin (XR Rig)");
            PlayerRig playerRig = null;
            if (xrOriginGo != null)
            {
                playerRig = xrOriginGo.GetComponent<PlayerRig>();
                if (playerRig == null) playerRig = xrOriginGo.AddComponent<PlayerRig>();

                // Configure Camera near clip
                Camera mainCam = xrOriginGo.GetComponentInChildren<Camera>(true);
                if (mainCam != null)
                {
                    mainCam.nearClipPlane = 0.05f;
                    sb.AppendLine($"Main camera near clip set to {mainCam.nearClipPlane}");
                }

                Transform locomotion = xrOriginGo.transform.Find("Locomotion");
                Transform leftController = xrOriginGo.transform.Find("Camera Offset/Left Controller");
                Transform rightController = xrOriginGo.transform.Find("Camera Offset/Right Controller");

                SerializedObject rigSo = new SerializedObject(playerRig);
                rigSo.FindProperty("rigRoot").objectReferenceValue = xrOriginGo.transform;
                if (mainCam != null) rigSo.FindProperty("headCamera").objectReferenceValue = mainCam;
                if (locomotion != null) rigSo.FindProperty("locomotionRoot").objectReferenceValue = locomotion.gameObject;
                if (leftController != null) rigSo.FindProperty("leftHand").objectReferenceValue = leftController;
                if (rightController != null) rigSo.FindProperty("rightHand").objectReferenceValue = rightController;
                rigSo.ApplyModifiedProperties();
                sb.AppendLine("PlayerRig configured on XR Origin.");
            }
            else
            {
                sb.AppendLine("WARNING: 'XR Origin (XR Rig)' not found!");
            }

            // -------------------------------------------------------------
            // PART 6: Systems
            // -------------------------------------------------------------
            sb.AppendLine("\n--- PART 6: Systems ---");
            Transform systems = root.transform.Find("Systems");
            CourageScore courage = systems.Find("CourageScore").GetComponent<CourageScore>();
            FadeOverlay fade = systems.Find("FadeOverlay").GetComponent<FadeOverlay>();
            if (fade != null)
            {
                SerializedObject fadeSo = new SerializedObject(fade);
                fadeSo.FindProperty("fadeMaterial").objectReferenceValue = mFade;
                fadeSo.FindProperty("quadDistance").floatValue = 0.2f;
                fadeSo.FindProperty("quadSize").floatValue = 2.0f;
                fadeSo.ApplyModifiedProperties();
            }

            AmbienceController ambience = systems.Find("Ambience").GetComponent<AmbienceController>();
            InstructionPanel instructions = systems.Find("InstructionPanel").GetComponent<InstructionPanel>();
            CompletionScreen completionScreen = systems.Find("CompletionScreen").GetComponent<CompletionScreen>();
            ComfortReturn comfortReturn = systems.Find("ComfortReturn").GetComponent<ComfortReturn>();

            // -------------------------------------------------------------
            // PART 7.6: SpawnPoint
            // -------------------------------------------------------------
            sb.AppendLine("\n--- PART 7.6: SpawnPoint ---");
            Transform spawnPoint = root.transform.Find("SpawnPoint");
            // Floor 50 Safe Room floor is at Y = 200. Spawn in the lounge facing the safety board (East)
            spawnPoint.position = new Vector3(-2, 200, 0);
            spawnPoint.rotation = Quaternion.Euler(0, 90, 0); // facing East toward core/board
            sb.AppendLine($"SpawnPoint set to {spawnPoint.position}, rotation {spawnPoint.eulerAngles}");

            // -------------------------------------------------------------
            // PART 7.5: Safety Board
            // -------------------------------------------------------------
            sb.AppendLine("\n--- PART 7.5: Safety Board ---");
            Transform world = root.transform.Find("World");
            GameObject sbGo = world.Find("SafetyBoard").gameObject;
            Canvas boardCanvas = sbGo.GetComponent<Canvas>();
            if (boardCanvas == null) boardCanvas = sbGo.AddComponent<Canvas>();
            boardCanvas.renderMode = RenderMode.WorldSpace;

            Transform safetyBoard = sbGo.transform;
            safetyBoard.position = new Vector3(8.5f, 201.5f, 0f);
            safetyBoard.rotation = Quaternion.Euler(0, -90, 0);
            safetyBoard.localScale = new Vector3(0.001f, 0.001f, 0.001f);

            RectTransform boardRt = sbGo.GetComponent<RectTransform>();
            boardRt.sizeDelta = new Vector2(1000, 700);

            // Background
            Transform bgT = safetyBoard.Find("Background");
            if (bgT == null)
            {
                GameObject bgGo = new GameObject("Background", typeof(RectTransform));
                bgGo.transform.SetParent(safetyBoard, false);
                bgT = bgGo.transform;
            }
            Image bgImg = bgT.GetComponent<Image>();
            if (bgImg == null) bgImg = bgT.gameObject.AddComponent<Image>();
            bgImg.color = new Color(10f/255f, 20f/255f, 30f/255f, 230f/255f);
            bgImg.raycastTarget = false;
            RectTransform bgRt = bgT.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            // Title
            Transform titleT = safetyBoard.Find("Title");
            if (titleT == null)
            {
                GameObject tGo = new GameObject("Title", typeof(RectTransform));
                tGo.transform.SetParent(safetyBoard, false);
                titleT = tGo.transform;
            }
            TextMeshProUGUI titleTmp = titleT.GetComponent<TextMeshProUGUI>();
            if (titleTmp == null) titleTmp = titleT.gameObject.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "Safety Board";
            titleTmp.fontSize = 72;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.raycastTarget = false;
            RectTransform titleRt = titleT.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0, 0.75f);
            titleRt.anchorMax = new Vector2(1, 1);
            titleRt.offsetMin = Vector2.zero;
            titleRt.offsetMax = Vector2.zero;

            // Body
            Transform bodyT = safetyBoard.Find("Body");
            if (bodyT == null)
            {
                GameObject bGo = new GameObject("Body", typeof(RectTransform));
                bGo.transform.SetParent(safetyBoard, false);
                bodyT = bGo.transform;
            }
            TextMeshProUGUI bodyTmp = bodyT.GetComponent<TextMeshProUGUI>();
            if (bodyTmp == null) bodyTmp = bodyT.gameObject.AddComponent<TextMeshProUGUI>();
            bodyTmp.text = "This experience helps you practise feeling comfortable at height, slowly and at your own pace.\n\n" +
                           "• You are always in control. Nothing here can hurt you.\n" +
                           "• Look away, close your eyes or sit down whenever you like.\n" +
                           "• To return to a safe place at any time, squeeze BOTH grip buttons until the screen fades.\n" +
                           "• Breathe slowly. There are no timers and no penalties.\n" +
                           "• Tell your therapist or guide if you want to stop.";
            bodyTmp.fontSize = 38;
            bodyTmp.enableWordWrapping = true;
            bodyTmp.alignment = TextAlignmentOptions.TopLeft;
            bodyTmp.raycastTarget = false;
            RectTransform bodyRt = bodyT.GetComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0.05f, 0.05f);
            bodyRt.anchorMax = new Vector2(0.95f, 0.75f);
            bodyRt.offsetMin = Vector2.zero;
            bodyRt.offsetMax = Vector2.zero;

            // -------------------------------------------------------------
            // PART 7.1 & 7.2: Doors & Elevator
            // -------------------------------------------------------------
            sb.AppendLine("\n--- PART 7.1 & 7.2: Doors & Elevator ---");

            // 1. RoomDoor (Disabled for open-concept floor plan)
            Transform roomDoorT = world.Find("RoomDoor");
            if (roomDoorT != null)
            {
                // Remove placeholder door panels if present
                for (int i = roomDoorT.childCount - 1; i >= 0; i--)
                {
                    GameObject.DestroyImmediate(roomDoorT.GetChild(i).gameObject);
                }
                roomDoorT.gameObject.SetActive(false);
            }
            sb.AppendLine("RoomDoor removed/disabled for open-concept floor layout.");

            // 2. Elevator CabDoor (Floor 50 Landing Doors)
            Transform cabDoorT = world.Find("Elevator/CabDoor");
            SlidingDoor cabDoor = cabDoorT.GetComponent<SlidingDoor>();
            if (cabDoor == null) cabDoor = cabDoorT.gameObject.AddComponent<SlidingDoor>();

            Transform door50L = GameObject.Find("LandingDoor_50_L")?.transform;
            Transform door50R = GameObject.Find("LandingDoor_50_R")?.transform;

            SerializedObject cdSo = new SerializedObject(cabDoor);
            var cdPanelsProp = cdSo.FindProperty("panels");
            cdPanelsProp.arraySize = 2;
            cdPanelsProp.GetArrayElementAtIndex(0).objectReferenceValue = door50L;
            cdPanelsProp.GetArrayElementAtIndex(1).objectReferenceValue = door50R;

            var cdOffsetsProp = cdSo.FindProperty("openOffsets");
            cdOffsetsProp.arraySize = 2;
            // Left slides negative Z (-0.55), Right slides positive Z (+0.55)
            cdOffsetsProp.GetArrayElementAtIndex(0).vector3Value = new Vector3(0, 0, -0.55f);
            cdOffsetsProp.GetArrayElementAtIndex(1).vector3Value = new Vector3(0, 0, 0.55f);
            cdSo.FindProperty("openSeconds").floatValue = 2.0f;
            cdSo.FindProperty("startOpen").boolValue = false;
            cdSo.ApplyModifiedProperties();
            sb.AppendLine("Configured CabDoor (LandingDoor_50_L/R).");

            // 3. Elevator ArrivalDoor (Rooftop Landing Doors)
            Transform arrivalDoorT = world.Find("Elevator/ArrivalDoor");
            SlidingDoor arrivalDoor = arrivalDoorT.GetComponent<SlidingDoor>();
            if (arrivalDoor == null) arrivalDoor = arrivalDoorT.gameObject.AddComponent<SlidingDoor>();

            Transform doorRL = GameObject.Find("LandingDoor_R_L")?.transform;
            Transform doorRR = GameObject.Find("LandingDoor_R_R")?.transform;

            SerializedObject adSo = new SerializedObject(arrivalDoor);
            var adPanelsProp = adSo.FindProperty("panels");
            adPanelsProp.arraySize = 2;
            adPanelsProp.GetArrayElementAtIndex(0).objectReferenceValue = doorRL;
            adPanelsProp.GetArrayElementAtIndex(1).objectReferenceValue = doorRR;

            var adOffsetsProp = adSo.FindProperty("openOffsets");
            adOffsetsProp.arraySize = 2;
            adOffsetsProp.GetArrayElementAtIndex(0).vector3Value = new Vector3(0, 0, -0.55f);
            adOffsetsProp.GetArrayElementAtIndex(1).vector3Value = new Vector3(0, 0, 0.55f);
            adSo.FindProperty("openSeconds").floatValue = 2.0f;
            adSo.FindProperty("startOpen").boolValue = false;
            adSo.ApplyModifiedProperties();
            sb.AppendLine("Configured ArrivalDoor (LandingDoor_R_L/R).");

            // 4. Elevator Controller
            Transform elevatorT = world.Find("Elevator");
            ElevatorController elevatorCtrl = elevatorT.GetComponent<ElevatorController>();
            if (elevatorCtrl == null) elevatorCtrl = elevatorT.gameObject.AddComponent<ElevatorController>();

            Transform envElevator = GameObject.Find("Envirnment/Elevator")?.transform;
            if (envElevator != null)
            {
                envElevator.position = new Vector3(-0.413f, 198.82f, 0f);
            }

            // TopAnchor at Rooftop (y = 398.798)
            Transform topAnchor = world.Find("Elevator/TopAnchor");
            if (topAnchor == null)
            {
                GameObject taGo = new GameObject("TopAnchor");
                taGo.transform.SetParent(elevatorT, false);
                topAnchor = taGo.transform;
            }
            topAnchor.position = new Vector3(-0.413f, 398.798f, 0f);

            // ArrivalStandPoint inside cab at rooftop
            Transform arrivalStandPoint = world.Find("Elevator/ArrivalStandPoint");
            arrivalStandPoint.position = new Vector3(12.3f, 400.0f, 0f);
            arrivalStandPoint.rotation = Quaternion.Euler(0, -90, 0); // facing out toward Rooftop

            // Sound
            AudioSource rumbleSource = world.Find("Elevator/Rumble").GetComponent<AudioSource>();
            if (rumbleSource == null) rumbleSource = world.Find("Elevator/Rumble").gameObject.AddComponent<AudioSource>();
            rumbleSource.playOnAwake = false;
            rumbleSource.loop = true;

            AudioSource dingSource = world.Find("Elevator/Ding").GetComponent<AudioSource>();
            if (dingSource == null) dingSource = world.Find("Elevator/Ding").gameObject.AddComponent<AudioSource>();
            dingSource.playOnAwake = false;

            // Floor counters 3D TextMeshPro
            Transform counter1T = envElevator != null ? envElevator.Find("Elevator_CarDisplayBezel/FloorCounter") : null;
            if (counter1T == null && envElevator != null)
            {
                Transform bezel = envElevator.Find("Elevator_CarDisplayBezel");
                GameObject cGo = new GameObject("FloorCounter");
                cGo.transform.SetParent(bezel != null ? bezel : envElevator, false);
                cGo.transform.localPosition = new Vector3(11.17f, 202.42f, 0f);
                cGo.transform.localRotation = Quaternion.Euler(0, -90, 0);
                var tmp = cGo.AddComponent<TextMeshPro>();
                tmp.fontSize = 5;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = new Color(90f/255f, 255f/255f, 120f/255f);
                tmp.text = "50";
                counter1T = cGo.transform;
            }

            Transform counter2T = world.Find("Elevator/LandingFloorCounter");
            if (counter2T == null)
            {
                GameObject cGo = new GameObject("LandingFloorCounter");
                cGo.transform.SetParent(elevatorT, false);
                cGo.transform.position = new Vector3(10.96f, 202.58f, 0f);
                cGo.transform.rotation = Quaternion.Euler(0, -90, 0);
                var tmp = cGo.AddComponent<TextMeshPro>();
                tmp.fontSize = 5;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = new Color(90f/255f, 255f/255f, 120f/255f);
                tmp.text = "50";
                counter2T = cGo.transform;
            }

            SerializedObject elSo = new SerializedObject(elevatorCtrl);
            elSo.FindProperty("cabDoor").objectReferenceValue = cabDoor;
            elSo.FindProperty("arrivalDoor").objectReferenceValue = arrivalDoor;
            elSo.FindProperty("mode").enumValueIndex = (int)ElevatorController.RideMode.PhysicalRise;
            elSo.FindProperty("rideSeconds").floatValue = 24f;
            elSo.FindProperty("pauseAfterDoorsClose").floatValue = 1.5f;
            elSo.FindProperty("fadeSeconds").floatValue = 0.7f;
            elSo.FindProperty("arrivalStandPoint").objectReferenceValue = arrivalStandPoint;
            elSo.FindProperty("fade").objectReferenceValue = fade;
            elSo.FindProperty("cabRoot").objectReferenceValue = envElevator;
            elSo.FindProperty("topAnchor").objectReferenceValue = topAnchor;

            var countersProp = elSo.FindProperty("floorCounters");
            countersProp.arraySize = 2;
            countersProp.GetArrayElementAtIndex(0).objectReferenceValue = counter1T?.GetComponent<TMP_Text>();
            countersProp.GetArrayElementAtIndex(1).objectReferenceValue = counter2T?.GetComponent<TMP_Text>();

            elSo.FindProperty("startFloor").intValue = 50;
            elSo.FindProperty("endFloor").intValue = 100;
            elSo.FindProperty("rumbleLoop").objectReferenceValue = rumbleSource;
            elSo.FindProperty("dingSource").objectReferenceValue = dingSource;
            elSo.ApplyModifiedProperties();
            sb.AppendLine("ElevatorController configured in PhysicalRise mode (from y=198.82 to y=398.798).");

            // -------------------------------------------------------------
            // PART 7.3: Buttons Setup
            // -------------------------------------------------------------
            sb.AppendLine("\n--- PART 7.3: Buttons Setup ---");
            // CallButton_50
            GameObject callBtnGo = GameObject.Find("Envirnment/CallButton_50");
            WorldButton callBtn = SetupWorldButton(callBtnGo, 
                new Color(255f/255f, 200f/255f, 60f/255f), 
                new Color(255f/255f, 240f/255f, 160f/255f), 
                new Color(80f/255f, 70f/255f, 30f/255f));

            // Cab buttons: LiftButton_R, LiftButton_75, LiftButton_50, LiftButton_25, LiftButton_G
            GameObject btnR_Go = GameObject.Find("Envirnment/Elevator/LiftButton_R");
            GameObject btn75_Go = GameObject.Find("Envirnment/Elevator/LiftButton_75");
            GameObject btn50_Go = GameObject.Find("Envirnment/Elevator/LiftButton_50");
            GameObject btn25_Go = GameObject.Find("Envirnment/Elevator/LiftButton_25");
            GameObject btnG_Go = GameObject.Find("Envirnment/Elevator/LiftButton_G");

            WorldButton btnR = SetupWorldButton(btnR_Go, new Color(60f/255f, 60f/255f, 66f/255f), new Color(255f/255f, 110f/255f, 100f/255f), new Color(35f/255f, 35f/255f, 38f/255f));
            WorldButton btn75 = SetupWorldButton(btn75_Go, new Color(60f/255f, 60f/255f, 66f/255f), new Color(255f/255f, 170f/255f, 80f/255f), new Color(35f/255f, 35f/255f, 38f/255f));
            WorldButton btn50 = SetupWorldButton(btn50_Go, new Color(60f/255f, 60f/255f, 66f/255f), new Color(120f/255f, 255f/255f, 150f/255f), new Color(35f/255f, 35f/255f, 38f/255f));
            WorldButton btn25 = SetupWorldButton(btn25_Go, new Color(60f/255f, 60f/255f, 66f/255f), new Color(100f/255f, 220f/255f, 240f/255f), new Color(35f/255f, 35f/255f, 38f/255f));
            WorldButton btnG = SetupWorldButton(btnG_Go, new Color(60f/255f, 60f/255f, 66f/255f), new Color(240f/255f, 240f/255f, 240f/255f), new Color(35f/255f, 35f/255f, 38f/255f));
            sb.AppendLine("Configured all 6 buttons with BoxCollider, XRSimpleInteractable, and WorldButton.");

            // -------------------------------------------------------------
            // PART 7.4: Rest Area & Safe Spots
            // -------------------------------------------------------------
            sb.AppendLine("\n--- PART 7.4: Rest Area & Safe Spots ---");
            Transform restAreaT = world.Find("RestArea");
            // Located on rooftop walkway/terrace near Y=400
            restAreaT.position = new Vector3(5, 400, 8);
            ZoneVolume restZone = restAreaT.GetComponent<ZoneVolume>();
            if (restZone == null) restZone = restAreaT.gameObject.AddComponent<ZoneVolume>();
            SetZoneVolume(restZone, new Vector3(5, 3, 5), new Vector3(0, 1.5f, 0));

            RestArea restArea = restAreaT.GetComponent<RestArea>();
            if (restArea == null) restArea = restAreaT.gameObject.AddComponent<RestArea>();

            Transform breathingOrb = restAreaT.Find("BreathingOrb");
            breathingOrb.position = new Vector3(5, 401.1f, 8);
            breathingOrb.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            MeshFilter orbMf = breathingOrb.GetComponent<MeshFilter>();
            if (orbMf == null)
            {
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                orbMf = breathingOrb.gameObject.AddComponent<MeshFilter>();
                orbMf.sharedMesh = sphere.GetComponent<MeshFilter>().sharedMesh;
                GameObject.DestroyImmediate(sphere);
            }
            MeshRenderer orbMr = breathingOrb.GetComponent<MeshRenderer>();
            if (orbMr == null) orbMr = breathingOrb.gameObject.AddComponent<MeshRenderer>();
            orbMr.material = mMarker;
            var orbCol = breathingOrb.GetComponent<Collider>();
            if (orbCol != null) GameObject.DestroyImmediate(orbCol);

            SerializedObject raSo = new SerializedObject(restArea);
            raSo.FindProperty("zone").objectReferenceValue = restZone;
            raSo.FindProperty("breathingOrb").objectReferenceValue = breathingOrb;
            raSo.FindProperty("orbMinScale").floatValue = 0.12f;
            raSo.FindProperty("orbMaxScale").floatValue = 0.32f;
            raSo.ApplyModifiedProperties();

            Transform safeSpotA = world.Find("SafeSpot_A");
            safeSpotA.position = new Vector3(3.5f, 400, 8);
            safeSpotA.rotation = Quaternion.Euler(0, -90, 0);

            Transform safeSpotB = world.Find("SafeSpot_B");
            safeSpotB.position = new Vector3(6.5f, 400, 8);
            safeSpotB.rotation = Quaternion.Euler(0, -90, 0);

            SerializedObject crSo = new SerializedObject(comfortReturn);
            crSo.FindProperty("fade").objectReferenceValue = fade;
            crSo.FindProperty("holdSeconds").floatValue = 1.5f;
            crSo.FindProperty("cooldownSeconds").floatValue = 6.0f;
            var spotsProp = crSo.FindProperty("safeSpots");
            spotsProp.arraySize = 2;
            spotsProp.GetArrayElementAtIndex(0).objectReferenceValue = safeSpotA;
            spotsProp.GetArrayElementAtIndex(1).objectReferenceValue = safeSpotB;
            crSo.ApplyModifiedProperties();
            sb.AppendLine("Configured Rest Area and Safe Spots on Rooftop.");

            // -------------------------------------------------------------
            // PART 8: The Seven Steps
            // -------------------------------------------------------------
            sb.AppendLine("\n--- PART 8: The Seven Steps ---");

            // STEP 1: SafeRoomStep
            Transform s1 = root.transform.Find("Step1_SafeRoom");
            SafeRoomStep step1 = s1.GetComponent<SafeRoomStep>();
            if (step1 == null) step1 = s1.gameObject.AddComponent<SafeRoomStep>();

            Transform s1LookT = s1.Find("SafetyBoardLook");
            s1LookT.position = safetyBoard.position;
            LookTarget s1Look = s1LookT.GetComponent<LookTarget>();
            if (s1Look == null) s1Look = s1LookT.gameObject.AddComponent<LookTarget>();
            SetLookTarget(s1Look, 2.5f, 20f, 20f, null, false);

            Transform s1ZoneT = s1.Find("Zone_WindowApproach");
            s1ZoneT.position = new Vector3(-18, 200, 0);
            ZoneVolume s1Zone = s1ZoneT.GetComponent<ZoneVolume>();
            if (s1Zone == null) s1Zone = s1ZoneT.gameObject.AddComponent<ZoneVolume>();
            SetZoneVolume(s1Zone, new Vector3(8, 3, 10), new Vector3(0, 1.5f, 0));

            SerializedObject s1So = new SerializedObject(step1);
            s1So.FindProperty("safetyBoardLook").objectReferenceValue = s1Look;
            s1So.FindProperty("windowApproachZone").objectReferenceValue = s1Zone;
            s1So.FindProperty("sectorsRequired").intValue = 5;
            s1So.FindProperty("courageReward").intValue = 5;
            s1So.ApplyModifiedProperties();
            sb.AppendLine("Step 1 (SafeRoomStep) configured.");

            // STEP 2: WindowObservationStep
            Transform s2 = root.transform.Find("Step2_Window");
            WindowObservationStep step2 = s2.GetComponent<WindowObservationStep>();
            if (step2 == null) step2 = s2.gameObject.AddComponent<WindowObservationStep>();

            Transform s2StandT = s2.Find("Zone_Stand");
            if (s2StandT.position == Vector3.zero || s2StandT.position == new Vector3(0, 200, 0))
                s2StandT.position = new Vector3(-26, 200, 0);

            ZoneVolume s2StandZone = s2StandT.GetComponent<ZoneVolume>();
            if (s2StandZone == null) s2StandZone = s2StandT.gameObject.AddComponent<ZoneVolume>();
            SetZoneVolume(s2StandZone, new Vector3(3, 3, 3), new Vector3(0, 1.5f, 0));

            Transform s2MarkerT = s2.Find("WindowMarker");
            // Sync WindowMarker with Zone_Stand position so they always match
            s2MarkerT.position = s2StandT.position;
            MarkerVisual s2Marker = s2MarkerT.GetComponent<MarkerVisual>();
            if (s2Marker == null) s2Marker = s2MarkerT.gameObject.AddComponent<MarkerVisual>();
            SetMarkerVisual(s2Marker, MarkerVisual.MarkerStyle.FloorRing, mMarker, new Color(51f/255f, 230f/255f, 255f/255f, 150f/255f), 1.2f, 2f, 0.1f, false);

            // User target building: Bldg_Near_08 (roof center ~ (-98, 136, -22))
            GameObject bldg08 = GameObject.Find("Envirnment/Bldg_Near_08");
            Vector3 bldgRoofPos = new Vector3(-98f, 136f, -22f);
            if (bldg08 != null)
            {
                var bmr = bldg08.GetComponent<MeshRenderer>();
                if (bmr != null) bldgRoofPos = new Vector3(bmr.bounds.center.x, bmr.bounds.max.y, bmr.bounds.center.z);
            }

            Transform s2TargetT = s2.Find("BuildingTarget");
            s2TargetT.position = bldgRoofPos;
            LookTarget s2Target = s2TargetT.GetComponent<LookTarget>();
            if (s2Target == null) s2Target = s2TargetT.gameObject.AddComponent<LookTarget>();
            // 14 degree cone angle ensures smooth, responsive gaze locking when testing in VR or simulator
            SetLookTarget(s2Target, 1.5f, 14f, 600f, s2StandZone, false);

            Transform s2BeaconT = s2.Find("BuildingBeacon");
            s2BeaconT.position = bldgRoofPos;
            MarkerVisual s2Beacon = s2BeaconT.GetComponent<MarkerVisual>();
            if (s2Beacon == null) s2Beacon = s2BeaconT.gameObject.AddComponent<MarkerVisual>();
            SetMarkerVisual(s2Beacon, MarkerVisual.MarkerStyle.Beam, mMarker, new Color(255f/255f, 220f/255f, 60f/255f, 160f/255f), 5f, 0f, 0f, false, 150f);

            SerializedObject s2So = new SerializedObject(step2);
            s2So.FindProperty("standZone").objectReferenceValue = s2StandZone;
            s2So.FindProperty("windowMarker").objectReferenceValue = s2Marker;
            s2So.FindProperty("buildingTarget").objectReferenceValue = s2Target;
            s2So.FindProperty("buildingBeacon").objectReferenceValue = s2Beacon;
            s2So.FindProperty("courageReward").intValue = 10;
            s2So.FindProperty("hintAfterSeconds").floatValue = 40f;
            s2So.ApplyModifiedProperties();
            sb.AppendLine("Step 2 (WindowObservationStep) configured pointing to Bldg_Near_08.");

            // STEP 3: HallwayStep
            Transform s3 = root.transform.Find("Step3_Hallway");
            HallwayStep step3 = s3.GetComponent<HallwayStep>();
            if (step3 == null) step3 = s3.gameObject.AddComponent<HallwayStep>();

            Transform floorGuideT = s3.Find("FloorGuide");
            GuidePath guidePath = floorGuideT.GetComponent<GuidePath>();
            if (guidePath == null) guidePath = floorGuideT.gameObject.AddComponent<GuidePath>();

            Vector3[] waypoints = {
                new Vector3(-24, 200, 0),
                new Vector3(-14, 200, 0),
                new Vector3(0, 200, 0),
                new Vector3(6, 200, 0),
                new Vector3(9.5f, 200, 0)
            };
            Transform[] wpTransforms = new Transform[waypoints.Length];
            for (int i = 0; i < waypoints.Length; i++)
            {
                Transform wp = floorGuideT.Find($"WP_{i}");
                if (wp == null)
                {
                    GameObject go = new GameObject($"WP_{i}");
                    go.transform.SetParent(floorGuideT, false);
                    wp = go.transform;
                }
                wp.position = waypoints[i];
                wpTransforms[i] = wp;
            }

            SerializedObject gpSo = new SerializedObject(guidePath);
            var wpsProp = gpSo.FindProperty("waypoints");
            wpsProp.arraySize = wpTransforms.Length;
            for (int i = 0; i < wpTransforms.Length; i++) wpsProp.GetArrayElementAtIndex(i).objectReferenceValue = wpTransforms[i];
            gpSo.FindProperty("material").objectReferenceValue = mGuideArrow;
            gpSo.FindProperty("spacing").floatValue = 1.2f;
            gpSo.FindProperty("arrowSize").floatValue = 0.55f;
            gpSo.FindProperty("floorOffset").floatValue = 0.03f;
            gpSo.FindProperty("sweepSpeed").floatValue = 3f;
            gpSo.FindProperty("hideRadius").floatValue = 0.9f;
            gpSo.ApplyModifiedProperties();

            Transform s3LobbyT = s3.Find("Zone_ElevatorLobby");
            s3LobbyT.position = new Vector3(9.5f, 200, 0);
            ZoneVolume s3LobbyZone = s3LobbyT.GetComponent<ZoneVolume>();
            if (s3LobbyZone == null) s3LobbyZone = s3LobbyT.gameObject.AddComponent<ZoneVolume>();
            SetZoneVolume(s3LobbyZone, new Vector3(4, 3, 3), new Vector3(0, 1.5f, 0));

            SerializedObject s3So = new SerializedObject(step3);
            s3So.FindProperty("roomDoor").objectReferenceValue = null;
            s3So.FindProperty("floorGuide").objectReferenceValue = guidePath;
            s3So.FindProperty("elevatorLobbyZone").objectReferenceValue = s3LobbyZone;
            s3So.FindProperty("callButton").objectReferenceValue = callBtn;
            s3So.FindProperty("elevator").objectReferenceValue = elevatorCtrl;
            s3So.FindProperty("courageReward").intValue = 5;
            s3So.ApplyModifiedProperties();
            sb.AppendLine("Step 3 (HallwayStep) configured.");

            // STEP 4: ElevatorRideStep
            Transform s4 = root.transform.Find("Step4_Elevator");
            ElevatorRideStep step4 = s4.GetComponent<ElevatorRideStep>();
            if (step4 == null) step4 = s4.gameObject.AddComponent<ElevatorRideStep>();

            Transform cabZoneT = s4.Find("Zone_Cab");
            cabZoneT.position = new Vector3(12.3f, 200, 0);
            ZoneVolume cabZone = cabZoneT.GetComponent<ZoneVolume>();
            if (cabZone == null) cabZone = cabZoneT.gameObject.AddComponent<ZoneVolume>();
            SetZoneVolume(cabZone, new Vector3(1.8f, 3, 1.8f), new Vector3(0, 1.5f, 0));

            WorldButton[] pButtons = { btnR, btn75, btn50, btn25, btnG };

            SerializedObject s4So = new SerializedObject(step4);
            s4So.FindProperty("elevator").objectReferenceValue = elevatorCtrl;
            s4So.FindProperty("cabZone").objectReferenceValue = cabZone;
            var pbProp = s4So.FindProperty("panelButtons");
            pbProp.arraySize = pButtons.Length;
            for (int i = 0; i < pButtons.Length; i++) pbProp.GetArrayElementAtIndex(i).objectReferenceValue = pButtons[i];
            s4So.FindProperty("destinationButtonIndex").intValue = 0; // LiftButton_R
            s4So.FindProperty("courageReward").intValue = 10;
            s4So.ApplyModifiedProperties();
            sb.AppendLine("Step 4 (ElevatorRideStep) configured.");

            // STEP 5: RooftopArrivalStep
            Transform s5 = root.transform.Find("Step5_Rooftop");
            RooftopArrivalStep step5 = s5.GetComponent<RooftopArrivalStep>();
            if (step5 == null) step5 = s5.gameObject.AddComponent<RooftopArrivalStep>();

            Transform s5TerraceT = s5.Find("Zone_Terrace");
            s5TerraceT.position = new Vector3(6, 400, 0);
            ZoneVolume s5TerraceZone = s5TerraceT.GetComponent<ZoneVolume>();
            if (s5TerraceZone == null) s5TerraceZone = s5TerraceT.gameObject.AddComponent<ZoneVolume>();
            SetZoneVolume(s5TerraceZone, new Vector3(10, 3, 10), new Vector3(0, 1.5f, 0));

            Transform cityViewT = s5.Find("CityViewTarget");
            cityViewT.position = new Vector3(-100, 400, 0);
            LookTarget cityView = cityViewT.GetComponent<LookTarget>();
            if (cityView == null) cityView = cityViewT.gameObject.AddComponent<LookTarget>();
            SetLookTarget(cityView, 3f, 20f, 1000f, null, false);

            SerializedObject s5So = new SerializedObject(step5);
            s5So.FindProperty("terraceZone").objectReferenceValue = s5TerraceZone;
            s5So.FindProperty("cityViewTarget").objectReferenceValue = cityView;
            s5So.FindProperty("minSecondsOnTerrace").floatValue = 10f;
            s5So.FindProperty("courageReward").intValue = 10;
            s5So.ApplyModifiedProperties();
            sb.AppendLine("Step 5 (RooftopArrivalStep) configured.");

            // STEP 6: FindTargetsStep
            Transform s6 = root.transform.Find("Step6_FindTargets");
            FindTargetsStep step6 = s6.GetComponent<FindTargetsStep>();
            if (step6 == null) step6 = s6.gameObject.AddComponent<FindTargetsStep>();

            Transform obsZoneT = s6.Find("Zone_Observation");
            obsZoneT.position = new Vector3(-6, 400, 18);
            ZoneVolume obsZone = obsZoneT.GetComponent<ZoneVolume>();
            if (obsZone == null) obsZone = obsZoneT.gameObject.AddComponent<ZoneVolume>();
            SetZoneVolume(obsZone, new Vector3(8, 3, 8), new Vector3(0, 1.5f, 0));

            // Mission Marker floating diamond over distant building
            Transform missionMarkerT = s6.Find("MissionMarker");
            missionMarkerT.position = new Vector3(0, 335, 120);
            MarkerVisual missionMarker = missionMarkerT.GetComponent<MarkerVisual>();
            if (missionMarker == null) missionMarker = missionMarkerT.gameObject.AddComponent<MarkerVisual>();
            SetMarkerVisual(missionMarker, MarkerVisual.MarkerStyle.FloatingDiamond, mMarker, new Color(255f/255f, 220f/255f, 60f/255f, 200f/255f), 8f, 2f, 0.2f, false);

            // Bonus collectible
            Transform bonusT = s6.Find("Collectible_Bonus");
            bonusT.position = new Vector3(2, 401.2f, 8);
            MeshFilter bonusMf = bonusT.GetComponent<MeshFilter>();
            if (bonusMf == null)
            {
                GameObject s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bonusMf = bonusT.gameObject.AddComponent<MeshFilter>();
                bonusMf.sharedMesh = s.GetComponent<MeshFilter>().sharedMesh;
                GameObject.DestroyImmediate(s);
            }
            MeshRenderer bonusMr = bonusT.GetComponent<MeshRenderer>();
            if (bonusMr == null) bonusMr = bonusT.gameObject.AddComponent<MeshRenderer>();
            bonusMr.material = mPickup;
            bonusT.localScale = new Vector3(0.25f, 0.25f, 0.25f);
            var bCol = bonusT.GetComponent<Collider>();
            if (bCol != null) GameObject.DestroyImmediate(bCol);

            Collectible collectible = bonusT.GetComponent<Collectible>();
            if (collectible == null) collectible = bonusT.gameObject.AddComponent<Collectible>();
            SerializedObject colSo = new SerializedObject(collectible);
            colSo.FindProperty("headPickupRadius").floatValue = 1.1f;
            colSo.FindProperty("handPickupRadius").floatValue = 0.3f;
            colSo.FindProperty("courageReward").intValue = 5;
            colSo.ApplyModifiedProperties();

            // Objective Panel
            GameObject objPanelGo = s6.Find("ObjectivePanel").gameObject;
            Canvas objCanvas = objPanelGo.GetComponent<Canvas>();
            if (objCanvas == null) objCanvas = objPanelGo.AddComponent<Canvas>();
            objCanvas.renderMode = RenderMode.WorldSpace;

            Transform objPanelT = objPanelGo.transform;
            objPanelT.position = new Vector3(-6, 401.8f, 16);
            objPanelT.localScale = new Vector3(0.001f, 0.001f, 0.001f);

            RectTransform objRt = objPanelGo.GetComponent<RectTransform>();
            objRt.sizeDelta = new Vector2(900, 300);

            FollowPanel objFollow = objPanelGo.GetComponent<FollowPanel>();
            if (objFollow == null) objFollow = objPanelGo.AddComponent<FollowPanel>();
            SerializedObject ofSo = new SerializedObject(objFollow);
            ofSo.FindProperty("distance").floatValue = 2.2f;
            ofSo.FindProperty("heightOffset").floatValue = -0.45f;
            ofSo.ApplyModifiedProperties();

            ObjectiveListPanel objList = objPanelGo.GetComponent<ObjectiveListPanel>();
            if (objList == null) objList = objPanelGo.AddComponent<ObjectiveListPanel>();
            SetupObjectiveListHierarchy(objPanelT, objList);

            // Three Targets: Red, Green, Blue
            Vector3 redPos = new Vector3(98f, 205f, 98f);
            Vector3 greenPos = new Vector3(0f, 325f, 120f);
            Vector3 bluePos = new Vector3(-142f, 320f, 22f);

            LookTarget redLook = SetupTargetCube(s6.Find("Target_Red"), redPos, mRed, obsZone);
            LookTarget greenLook = SetupTargetCube(s6.Find("Target_Green"), greenPos, mGreen, obsZone);
            LookTarget blueLook = SetupTargetCube(s6.Find("Target_Blue"), bluePos, mBlue, obsZone);

            MarkerVisual redHint = SetupHintMarker(s6.Find("Hint_Red"), redPos + Vector3.up * 6, mMarker, Color.red);
            MarkerVisual greenHint = SetupHintMarker(s6.Find("Hint_Green"), greenPos + Vector3.up * 6, mMarker, Color.green);
            MarkerVisual blueHint = SetupHintMarker(s6.Find("Hint_Blue"), bluePos + Vector3.up * 6, mMarker, Color.blue);

            SerializedObject s6So = new SerializedObject(step6);
            s6So.FindProperty("missionMarker").objectReferenceValue = missionMarker;
            s6So.FindProperty("objectivePanel").objectReferenceValue = objList;
            s6So.FindProperty("bonusCollectible").objectReferenceValue = collectible;
            s6So.FindProperty("rewardPerTarget").intValue = 15;
            s6So.FindProperty("hintAfterSeconds").floatValue = 45f;

            var targetsProp = s6So.FindProperty("targets");
            targetsProp.arraySize = 3;

            var t0 = targetsProp.GetArrayElementAtIndex(0);
            t0.FindPropertyRelative("label").stringValue = "red object";
            t0.FindPropertyRelative("color").colorValue = new Color(1f, 0.25f, 0.2f, 1f);
            t0.FindPropertyRelative("look").objectReferenceValue = redLook;
            t0.FindPropertyRelative("hintMarker").objectReferenceValue = redHint;

            var t1 = targetsProp.GetArrayElementAtIndex(1);
            t1.FindPropertyRelative("label").stringValue = "green object";
            t1.FindPropertyRelative("color").colorValue = new Color(0.25f, 0.9f, 0.3f, 1f);
            t1.FindPropertyRelative("look").objectReferenceValue = greenLook;
            t1.FindPropertyRelative("hintMarker").objectReferenceValue = greenHint;

            var t2 = targetsProp.GetArrayElementAtIndex(2);
            t2.FindPropertyRelative("label").stringValue = "blue object";
            t2.FindPropertyRelative("color").colorValue = new Color(0.3f, 0.5f, 1f, 1f);
            t2.FindPropertyRelative("look").objectReferenceValue = blueLook;
            t2.FindPropertyRelative("hintMarker").objectReferenceValue = blueHint;

            s6So.ApplyModifiedProperties();
            sb.AppendLine("Step 6 (FindTargetsStep) configured with 3 targets.");

            // STEP 7: FinalObservationStep
            Transform s7 = root.transform.Find("Step7_Final");
            FinalObservationStep step7 = s7.GetComponent<FinalObservationStep>();
            if (step7 == null) step7 = s7.gameObject.AddComponent<FinalObservationStep>();

            Transform viewZoneT = s7.Find("Zone_Viewing");
            viewZoneT.position = new Vector3(-6, 400, 21);
            ZoneVolume viewZone = viewZoneT.GetComponent<ZoneVolume>();
            if (viewZone == null) viewZone = viewZoneT.gameObject.AddComponent<ZoneVolume>();
            SetZoneVolume(viewZone, new Vector3(3, 3, 2.5f), new Vector3(0, 1.5f, 0));

            Transform viewDirT = s7.Find("ViewDirection");
            viewDirT.position = new Vector3(-6, 401.2f, 21);
            viewDirT.rotation = Quaternion.Euler(0, 0, 0); // blue arrow forward = +Z (outward)

            // DurationPicker
            Transform pickerT = s7.Find("DurationPicker");
            pickerT.position = new Vector3(-5.2f, 401.1f, 21.5f);
            pickerT.rotation = Quaternion.Euler(0, 180, 0); // facing player looking toward railing

            WorldButton dur5 = SetupButtonCube(pickerT, "Dur_5s", new Vector3(-0.25f, 0, 0), "5 s", mButtonDisc);
            WorldButton dur10 = SetupButtonCube(pickerT, "Dur_10s", new Vector3(0, 0, 0), "10 s", mButtonDisc);
            WorldButton dur20 = SetupButtonCube(pickerT, "Dur_20s", new Vector3(0.25f, 0, 0), "20 s", mButtonDisc);
            WorldButton[] durButtons = { dur5, dur10, dur20 };

            // FinishEarlyButton
            Transform earlyT = s7.Find("FinishEarlyButton");
            earlyT.position = new Vector3(-6.8f, 401.1f, 21.5f);
            earlyT.rotation = Quaternion.Euler(0, 180, 0);
            WorldButton earlyBtn = SetupButtonCube(earlyT.parent, "FinishEarlyButton", earlyT.localPosition, "I'm done", mRed, 
                new Color(200f/255f, 60f/255f, 60f/255f), new Color(255f/255f, 100f/255f, 100f/255f), new Color(80f/255f, 30f/255f, 30f/255f));

            // ProgressCanvas
            GameObject progCanvasGo = s7.Find("ProgressCanvas").gameObject;
            Canvas pCanvas = progCanvasGo.GetComponent<Canvas>();
            if (pCanvas == null) pCanvas = progCanvasGo.AddComponent<Canvas>();
            pCanvas.renderMode = RenderMode.WorldSpace;

            Transform progCanvasT = progCanvasGo.transform;
            progCanvasT.position = new Vector3(-6, 401.5f, 21.8f);
            progCanvasT.rotation = Quaternion.Euler(0, 180, 0);
            progCanvasT.localScale = new Vector3(0.001f, 0.001f, 0.001f);

            RectTransform pRt = progCanvasGo.GetComponent<RectTransform>();
            pRt.sizeDelta = new Vector2(400, 400);

            // Progress Canvas hierarchy: Background, Fill, Label
            Image progFill = SetupProgressCanvasHierarchy(progCanvasT);
            TextMeshProUGUI progLabel = progCanvasGo.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();

            SerializedObject s7So = new SerializedObject(step7);
            s7So.FindProperty("viewingZone").objectReferenceValue = viewZone;
            s7So.FindProperty("viewDirection").objectReferenceValue = viewDirT;
            s7So.FindProperty("durationPicker").objectReferenceValue = pickerT.gameObject;

            var dbProp = s7So.FindProperty("durationButtons");
            dbProp.arraySize = durButtons.Length;
            for (int i = 0; i < durButtons.Length; i++) dbProp.GetArrayElementAtIndex(i).objectReferenceValue = durButtons[i];

            s7So.FindProperty("finishEarlyButton").objectReferenceValue = earlyBtn;
            s7So.FindProperty("progressRoot").objectReferenceValue = progCanvasT.gameObject;
            s7So.FindProperty("progressFill").objectReferenceValue = progFill;
            s7So.FindProperty("progressLabel").objectReferenceValue = progLabel;
            s7So.FindProperty("minPitchDown").floatValue = 20f;
            s7So.FindProperty("minOutwardDot").floatValue = 0.2f;
            s7So.ApplyModifiedProperties();
            sb.AppendLine("Step 7 (FinalObservationStep) configured.");

            // -------------------------------------------------------------
            // PART 9: Director Wiring & Validation
            // -------------------------------------------------------------
            sb.AppendLine("\n--- PART 9: Director Wiring & Validation ---");
            SerializedObject dirSo = new SerializedObject(director);
            dirSo.FindProperty("playerRig").objectReferenceValue = playerRig;
            dirSo.FindProperty("instructions").objectReferenceValue = instructions;
            dirSo.FindProperty("courage").objectReferenceValue = courage;
            dirSo.FindProperty("fade").objectReferenceValue = fade;
            dirSo.FindProperty("ambience").objectReferenceValue = ambience;
            dirSo.FindProperty("completionScreen").objectReferenceValue = completionScreen;
            dirSo.FindProperty("spawnPoint").objectReferenceValue = spawnPoint;

            LevelStep[] allSteps = { step1, step2, step3, step4, step5, step6, step7 };
            var stepsProp = dirSo.FindProperty("steps");
            stepsProp.arraySize = allSteps.Length;
            for (int i = 0; i < allSteps.Length; i++) stepsProp.GetArrayElementAtIndex(i).objectReferenceValue = allSteps[i];

            dirSo.FindProperty("autoStart").boolValue = true;
            dirSo.FindProperty("startDelay").floatValue = 2.0f;
            dirSo.FindProperty("pauseBetweenSteps").floatValue = 1.5f;
            dirSo.FindProperty("hideStepObjectsWhenFinished").boolValue = true;
            dirSo.FindProperty("validateOnStart").boolValue = true;
            dirSo.ApplyModifiedProperties();
            sb.AppendLine("Level1Director wired with all 7 steps and references.");

            // Run Validation
            ValidationReport report = director.BuildReport();
            sb.AppendLine($"\nValidation Report: {report.Errors.Count} errors, {report.Warnings.Count} warnings");
            foreach (var err in report.Errors) sb.AppendLine($"  [ERROR] {err}");
            foreach (var w in report.Warnings) sb.AppendLine($"  [WARN]  {w}");

            // Save Scene
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            sb.AppendLine("\nScene saved successfully!");

            File.WriteAllText("BuildExecution_Result.txt", sb.ToString());
            Debug.Log("[Level1] Complete Build executed and saved.");
        }

        // --- Helpers ---

        private static Material GetOrCreateMaterial(string path, Shader shader, bool transparent, Color color)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                if (transparent)
                {
                    mat.SetFloat("_Surface", 1); // Transparent
                    mat.SetFloat("_Blend", 0);   // Alpha
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                }
                mat.SetColor("_BaseColor", color);
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        private static void SetZoneVolume(ZoneVolume zv, Vector3 size, Vector3 center)
        {
            SerializedObject so = new SerializedObject(zv);
            so.FindProperty("size").vector3Value = size;
            so.FindProperty("center").vector3Value = center;
            so.ApplyModifiedProperties();
        }

        private static void SetLookTarget(LookTarget lt, float dwell, float cone, float maxDist, ZoneVolume gate, bool resetLookingAway)
        {
            SerializedObject so = new SerializedObject(lt);
            so.FindProperty("dwellSeconds").floatValue = dwell;
            so.FindProperty("coneAngle").floatValue = cone;
            so.FindProperty("maxDistance").floatValue = maxDist;
            so.FindProperty("gateZone").objectReferenceValue = gate;
            so.FindProperty("resetWhenLookingAway").boolValue = resetLookingAway;
            so.ApplyModifiedProperties();
        }

        private static void SetMarkerVisual(MarkerVisual mv, MarkerVisual.MarkerStyle style, Material mat, Color color, float size, float pulseSpeed, float pulseAmount, bool visibleOnStart, float height = 100f)
        {
            SerializedObject so = new SerializedObject(mv);
            so.FindProperty("style").enumValueIndex = (int)style;
            so.FindProperty("material").objectReferenceValue = mat;
            so.FindProperty("color").colorValue = color;
            so.FindProperty("size").floatValue = size;
            so.FindProperty("pulseSpeed").floatValue = pulseSpeed;
            so.FindProperty("pulseAmount").floatValue = pulseAmount;
            so.FindProperty("visibleOnStart").boolValue = visibleOnStart;
            so.FindProperty("height").floatValue = height;
            so.ApplyModifiedProperties();
        }

        private static WorldButton SetupWorldButton(GameObject go, Color idle, Color hover, Color disabled)
        {
            if (go == null) return null;
            var col = go.GetComponent<BoxCollider>();
            if (col == null)
            {
                col = go.AddComponent<BoxCollider>();
                col.size = new Vector3(0.08f, 0.08f, 0.08f);
            }
            col.isTrigger = false;

            var xri = go.GetComponent<XRSimpleInteractable>();
            if (xri == null) xri = go.AddComponent<XRSimpleInteractable>();

            var wb = go.GetComponent<WorldButton>();
            if (wb == null) wb = go.AddComponent<WorldButton>();

            var mr = go.GetComponent<MeshRenderer>();
            SerializedObject so = new SerializedObject(wb);
            so.FindProperty("visual").objectReferenceValue = mr;
            so.FindProperty("idleColor").colorValue = idle;
            so.FindProperty("hoverColor").colorValue = hover;
            so.FindProperty("disabledColor").colorValue = disabled;
            so.FindProperty("pressedColor").colorValue = Color.white;
            so.FindProperty("startInteractable").boolValue = false;
            so.FindProperty("cooldownSeconds").floatValue = 0.6f;
            so.ApplyModifiedProperties();
            return wb;
        }

        private static LookTarget SetupTargetCube(Transform t, Vector3 pos, Material mat, ZoneVolume gateZone)
        {
            if (t == null) return null;
            t.position = pos;
            t.localScale = new Vector3(3, 3, 3);
            MeshFilter mf = t.GetComponent<MeshFilter>();
            if (mf == null)
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                mf = t.gameObject.AddComponent<MeshFilter>();
                mf.sharedMesh = cube.GetComponent<MeshFilter>().sharedMesh;
                GameObject.DestroyImmediate(cube);
            }
            MeshRenderer mr = t.GetComponent<MeshRenderer>();
            if (mr == null) mr = t.gameObject.AddComponent<MeshRenderer>();
            mr.material = mat;

            var col = t.GetComponent<BoxCollider>();
            if (col != null) GameObject.DestroyImmediate(col);

            LookTarget lt = t.GetComponent<LookTarget>();
            if (lt == null) lt = t.gameObject.AddComponent<LookTarget>();
            SetLookTarget(lt, 1.5f, 7f, 800f, gateZone, false);
            return lt;
        }

        private static MarkerVisual SetupHintMarker(Transform t, Vector3 pos, Material mat, Color color)
        {
            if (t == null) return null;
            t.position = pos;
            MarkerVisual mv = t.GetComponent<MarkerVisual>();
            if (mv == null) mv = t.gameObject.AddComponent<MarkerVisual>();
            SetMarkerVisual(mv, MarkerVisual.MarkerStyle.FloatingDiamond, mat, new Color(color.r, color.g, color.b, 180f/255f), 5f, 2f, 0.2f, false);
            return mv;
        }

        private static WorldButton SetupButtonCube(Transform parent, string name, Vector3 localPos, string labelText, Material mat, 
            Color? idle = null, Color? hover = null, Color? disabled = null)
        {
            Transform t = parent.Find(name);
            if (t == null)
            {
                GameObject go = new GameObject(name);
                go.transform.SetParent(parent, false);
                t = go.transform;
            }
            t.localPosition = localPos;
            t.localScale = new Vector3(0.14f, 0.14f, 0.05f);

            var mf = t.GetComponent<MeshFilter>();
            if (mf == null)
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                mf = t.gameObject.AddComponent<MeshFilter>();
                mf.sharedMesh = cube.GetComponent<MeshFilter>().sharedMesh;
                GameObject.DestroyImmediate(cube);
            }

            var mr = t.GetComponent<MeshRenderer>();
            if (mr == null) mr = t.gameObject.AddComponent<MeshRenderer>();
            mr.material = mat;

            var col = t.GetComponent<BoxCollider>();
            if (col == null) col = t.gameObject.AddComponent<BoxCollider>();
            col.isTrigger = false;

            var xri = t.GetComponent<XRSimpleInteractable>();
            if (xri == null) xri = t.gameObject.AddComponent<XRSimpleInteractable>();

            var wb = t.GetComponent<WorldButton>();
            if (wb == null) wb = t.gameObject.AddComponent<WorldButton>();

            Color idl = idle ?? new Color(60f/255f, 60f/255f, 66f/255f);
            Color hov = hover ?? new Color(120f/255f, 255f/255f, 150f/255f);
            Color dis = disabled ?? new Color(35f/255f, 35f/255f, 38f/255f);

            SerializedObject so = new SerializedObject(wb);
            so.FindProperty("visual").objectReferenceValue = mr;
            so.FindProperty("idleColor").colorValue = idl;
            so.FindProperty("hoverColor").colorValue = hov;
            so.FindProperty("disabledColor").colorValue = dis;
            so.FindProperty("pressedColor").colorValue = Color.white;
            so.FindProperty("startInteractable").boolValue = false;
            so.ApplyModifiedProperties();

            // Label
            Transform lblT = t.Find("Label");
            if (lblT == null)
            {
                GameObject lGo = new GameObject("Label");
                lGo.transform.SetParent(t, false);
                lblT = lGo.transform;
                lblT.localPosition = new Vector3(0, 0, -0.6f);
                lblT.localRotation = Quaternion.identity;
                var tmp = lGo.AddComponent<TextMeshPro>();
                tmp.fontSize = 2.5f;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
                tmp.text = labelText;
            }
            return wb;
        }

        private static void SetupObjectiveListHierarchy(Transform panel, ObjectiveListPanel listPanel)
        {
            // Background
            Transform bgT = panel.Find("Background");
            if (bgT == null)
            {
                GameObject go = new GameObject("Background", typeof(RectTransform));
                go.transform.SetParent(panel, false);
                bgT = go.transform;
                var img = go.AddComponent<Image>();
                img.color = new Color(0, 0, 0, 170f/255f);
                img.raycastTarget = false;
                RectTransform rt = go.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            // Header
            Transform headerT = panel.Find("Header");
            if (headerT == null)
            {
                GameObject go = new GameObject("Header", typeof(RectTransform));
                go.transform.SetParent(panel, false);
                headerT = go.transform;
                var tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.fontSize = 44;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.text = "Objectives";
                tmp.raycastTarget = false;
                RectTransform rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 0.75f);
                rt.anchorMax = new Vector2(1, 1);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            // Rows: Row_0, Row_1, Row_2
            float[] yOffsets = { 60f, 0f, -60f };
            SerializedObject lpSo = new SerializedObject(listPanel);
            lpSo.FindProperty("headerText").objectReferenceValue = headerT.GetComponent<TextMeshProUGUI>();
            var rowsProp = lpSo.FindProperty("rows");
            rowsProp.arraySize = 3;

            for (int i = 0; i < 3; i++)
            {
                Transform rowT = panel.Find($"Row_{i}");
                if (rowT == null)
                {
                    GameObject go = new GameObject($"Row_{i}", typeof(RectTransform));
                    go.transform.SetParent(panel, false);
                    rowT = go.transform;
                    RectTransform rt = go.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(800, 50);
                    rt.anchoredPosition = new Vector2(0, yOffsets[i]);

                    // Dot
                    GameObject dotGo = new GameObject("Dot", typeof(RectTransform));
                    dotGo.transform.SetParent(rowT, false);
                    var dotImg = dotGo.AddComponent<Image>();
                    dotImg.raycastTarget = false;
                    RectTransform dotRt = dotGo.GetComponent<RectTransform>();
                    dotRt.sizeDelta = new Vector2(30, 30);
                    dotRt.anchoredPosition = new Vector2(-350, 0);

                    // Label
                    GameObject lblGo = new GameObject("Label", typeof(RectTransform));
                    lblGo.transform.SetParent(rowT, false);
                    var lblTmp = lblGo.AddComponent<TextMeshProUGUI>();
                    lblTmp.fontSize = 36;
                    lblTmp.text = $"Target {i+1}";
                    lblTmp.raycastTarget = false;
                    RectTransform lblRt = lblGo.GetComponent<RectTransform>();
                    lblRt.sizeDelta = new Vector2(600, 45);
                    lblRt.anchoredPosition = new Vector2(0, 0);

                    // Check
                    GameObject chkGo = new GameObject("Check", typeof(RectTransform));
                    chkGo.transform.SetParent(rowT, false);
                    var chkImg = chkGo.AddComponent<Image>();
                    chkImg.color = new Color(90f/255f, 255f/255f, 130f/255f);
                    chkImg.raycastTarget = false;
                    RectTransform chkRt = chkGo.GetComponent<RectTransform>();
                    chkRt.sizeDelta = new Vector2(30, 30);
                    chkRt.anchoredPosition = new Vector2(350, 0);
                }

                var rowElem = rowsProp.GetArrayElementAtIndex(i);
                rowElem.FindPropertyRelative("root").objectReferenceValue = rowT.gameObject;
                rowElem.FindPropertyRelative("dot").objectReferenceValue = rowT.Find("Dot")?.GetComponent<Image>();
                rowElem.FindPropertyRelative("label").objectReferenceValue = rowT.Find("Label")?.GetComponent<TextMeshProUGUI>();
                rowElem.FindPropertyRelative("checkMark").objectReferenceValue = rowT.Find("Check")?.gameObject;
            }
            lpSo.ApplyModifiedProperties();
        }

        private static Image SetupProgressCanvasHierarchy(Transform canvasT)
        {
            // Background
            Transform bgT = canvasT.Find("Background");
            if (bgT == null)
            {
                GameObject go = new GameObject("Background", typeof(RectTransform));
                go.transform.SetParent(canvasT, false);
                bgT = go.transform;
                var img = go.AddComponent<Image>();
                img.color = new Color(0, 0, 0, 170f/255f);
                img.raycastTarget = false;
                RectTransform rt = go.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            // Fill
            Transform fillT = canvasT.Find("Fill");
            Image fillImg = null;
            if (fillT == null)
            {
                GameObject go = new GameObject("Fill", typeof(RectTransform));
                go.transform.SetParent(canvasT, false);
                fillT = go.transform;
                fillImg = go.AddComponent<Image>();
                fillImg.type = Image.Type.Filled;
                fillImg.fillMethod = Image.FillMethod.Radial360;
                fillImg.fillOrigin = (int)Image.Origin360.Top;
                fillImg.color = new Color(90f/255f, 255f/255f, 130f/255f);
                fillImg.raycastTarget = false;
                RectTransform rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.1f, 0.1f);
                rt.anchorMax = new Vector2(0.9f, 0.9f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
            else
            {
                fillImg = fillT.GetComponent<Image>();
            }

            // Label
            Transform lblT = canvasT.Find("Label");
            if (lblT == null)
            {
                GameObject go = new GameObject("Label", typeof(RectTransform));
                go.transform.SetParent(canvasT, false);
                lblT = go.transform;
                var tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.fontSize = 56;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.text = "Look down";
                tmp.raycastTarget = false;
                RectTransform rt = go.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
            return fillImg;
        }
    }
}
#endif
