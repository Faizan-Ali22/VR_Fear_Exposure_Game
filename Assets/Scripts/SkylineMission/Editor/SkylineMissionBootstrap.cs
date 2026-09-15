#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using SkylineMission.Mechanics;
using SkylineMission.Mission;
using SkylineMission.Mission.Tasks;
using SkylineMission.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public static class SkylineMissionBootstrap
{
    private const string RootName = "[SkylineMission]";
    private const string MaterialsPath = "Assets/Materials/SkylineMission";

    [MenuItem("Skyline Mission/Build Mission Environment")]
    public static void BuildEnvironment()
    {
        ClearEnvironment();

        GameObject root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Build Mission Environment");

        EnsureMaterialsExist();
        AddRequiredTags();

        #region 1. Environment Geometry

        GameObject envParent = CreateFolder("Environment", root.transform);

        CreateBuilding(envParent.transform, "BuildingA",
            new Vector3(12, 0.5f, 12), new Vector3(0, 100, 0),
            new Vector3(12, 100, 12), new Vector3(0, 50, 0));
        CreateBuilding(envParent.transform, "BuildingB",
            new Vector3(14, 0.5f, 14), new Vector3(25, 100, 0),
            new Vector3(14, 100, 14), new Vector3(25, 50, 0));
        CreateBuilding(envParent.transform, "BuildingC",
            new Vector3(10, 0.5f, 10), new Vector3(50, 100, 5),
            new Vector3(10, 100, 10), new Vector3(50, 50, 5));

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "GroundPlane";
        ground.transform.SetParent(envParent.transform);
        ground.transform.position = new Vector3(25, 0, 0);
        ground.transform.localScale = new Vector3(20, 1, 20);
        ApplyMaterial(ground, "SM_Ground");

        GameObject depthCues = CreateFolder("DepthCueBuildings", envParent.transform);
        for (int i = 1; i <= 20; i++)
        {
            GameObject db = GameObject.CreatePrimitive(PrimitiveType.Cube);
            db.name = $"DepthBuilding_{i:00}";
            db.transform.SetParent(depthCues.transform);
            float h = Random.Range(30f, 85f);
            db.transform.localScale = new Vector3(Random.Range(5f, 15f), h, Random.Range(5f, 15f));
            db.transform.position = new Vector3(Random.Range(-50f, 100f), h / 2f, Random.Range(-50f, 100f));
            ApplyMaterial(db, "SM_ConcreteDark");
            db.isStatic = true;
        }

        #endregion

        #region 2. Task 1 — Plank Traversal

        GameObject task1Folder = CreateFolder("Task1_PlankTraversal", root.transform);

        GameObject plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plank.name = "WoodenPlank";
        plank.transform.SetParent(task1Folder.transform);
        plank.transform.localScale = new Vector3(8, 0.1f, 0.6f);
        plank.transform.position = new Vector3(12.5f, 100.05f, 0);
        plank.tag = "NarrowPath";
        ApplyMaterial(plank, "SM_Wood");

        GameObject plankTrigger = new GameObject("PlankTrigger");
        plankTrigger.transform.SetParent(plank.transform);
        plankTrigger.transform.localPosition = Vector3.zero;
        BoxCollider ptc = plankTrigger.AddComponent<BoxCollider>();
        ptc.isTrigger = true;
        ptc.size = new Vector3(1, 5, 1.6f);

        Transform plankStart = CreateEmpty(task1Folder.transform, "PlankStart", new Vector3(6.5f, 100.1f, 0));
        Transform plankEnd = CreateEmpty(task1Folder.transform, "PlankEnd", new Vector3(18.5f, 100.1f, 0));

        PlatformPhysics plankPhysics = plank.AddComponent<PlatformPhysics>();
        SetPrivateField(plankPhysics, "wobbleAmplitude", 1.5f);
        SetPrivateField(plankPhysics, "wobbleSpeed", 1.5f);
        SetPrivateField(plankPhysics, "canCrumble", false);
        SetPrivateField(plankPhysics, "enableWobble", true);

        PlankTraversal plankTask = task1Folder.AddComponent<PlankTraversal>();
        SetPrivateField(plankTask, "startTrigger", plankStart);
        SetPrivateField(plankTask, "endTrigger", plankEnd);
        SetPrivateField(plankTask, "plankPlatformPhysics", plankPhysics);
        SetPrivateField(plankTask, "plankObject", plank);

        #endregion

        #region 3. Task 2 — Platform Jumping

        GameObject task2Folder = CreateFolder("Task2_PlatformJumping", root.transform);

        PlatformPhysics[] platformPhysicsArr = new PlatformPhysics[6];
        platformPhysicsArr[0] = CreatePlatformWithPhysics(task2Folder.transform, "Platform_01",
            new Vector3(2, 0.3f, 2), new Vector3(27, 100.2f, 4), false);
        platformPhysicsArr[1] = CreatePlatformWithPhysics(task2Folder.transform, "Platform_02",
            new Vector3(1.8f, 0.3f, 1.8f), new Vector3(29, 100.5f, 6), false);
        platformPhysicsArr[2] = CreatePlatformWithPhysics(task2Folder.transform, "Platform_03",
            new Vector3(2.2f, 0.3f, 1.5f), new Vector3(31.5f, 100.0f, 8), true);
        platformPhysicsArr[3] = CreatePlatformWithPhysics(task2Folder.transform, "Platform_04",
            new Vector3(1.5f, 0.3f, 2.0f), new Vector3(34, 100.8f, 9), false);
        platformPhysicsArr[4] = CreatePlatformWithPhysics(task2Folder.transform, "Platform_05",
            new Vector3(2.0f, 0.3f, 2.0f), new Vector3(36, 100.3f, 7), true);
        platformPhysicsArr[5] = CreatePlatformWithPhysics(task2Folder.transform, "Platform_06_Final",
            new Vector3(3, 0.3f, 3), new Vector3(38, 100.0f, 5), false);

        platformPhysicsArr[5].gameObject.tag = "SafeZone";

        Transform platformGoal = CreateEmpty(task2Folder.transform, "PlatformGoal", new Vector3(38, 100.0f, 5));

        PlatformJumping platformTask = task2Folder.AddComponent<PlatformJumping>();
        SetPrivateField(platformTask, "platforms", platformPhysicsArr);
        SetPrivateField(platformTask, "finalPlatformTrigger", platformGoal);
        SetPrivateField(platformTask, "timedPlatformIndices", new int[] { 2, 4 });

        #endregion

        #region 4. Task 3 — Drone Rescue

        GameObject task3Folder = CreateFolder("Task3_DroneRescue", root.transform);

        GameObject droneLedge = GameObject.CreatePrimitive(PrimitiveType.Cube);
        droneLedge.name = "DroneLedge";
        droneLedge.transform.SetParent(task3Folder.transform);
        droneLedge.transform.localScale = new Vector3(3, 0.1f, 1);
        droneLedge.transform.position = new Vector3(50, 101, 10);
        droneLedge.tag = "NarrowPath";
        ApplyMaterial(droneLedge, "SM_Concrete");

        GameObject droneBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
        droneBody.name = "DroneBody";
        droneBody.transform.SetParent(task3Folder.transform);
        droneBody.transform.localScale = new Vector3(0.4f, 0.15f, 0.4f);
        droneBody.transform.position = new Vector3(50, 101.3f, 10.2f);
        ApplyMaterial(droneBody, "SM_DronRed");

        Rigidbody droneRb = droneBody.AddComponent<Rigidbody>();
        droneRb.isKinematic = true;
        droneBody.AddComponent<XRGrabInteractable>();

        Transform[] rotors = new Transform[4];
        string[] rotorNames = { "Rotor_FL", "Rotor_FR", "Rotor_BL", "Rotor_BR" };
        Vector3[] rotorOffsets = {
            new Vector3(-0.18f, 0.08f, 0.18f),
            new Vector3(0.18f, 0.08f, 0.18f),
            new Vector3(-0.18f, 0.08f, -0.18f),
            new Vector3(0.18f, 0.08f, -0.18f)
        };
        for (int i = 0; i < 4; i++)
        {
            GameObject rotor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rotor.name = rotorNames[i];
            rotor.transform.SetParent(droneBody.transform);
            rotor.transform.localScale = new Vector3(0.35f, 0.02f, 0.35f);
            rotor.transform.localPosition = rotorOffsets[i];
            ApplyMaterial(rotor, "SM_ConcreteDark");
            Object.DestroyImmediate(rotor.GetComponent<Collider>());
            rotors[i] = rotor.transform;
        }

        GameObject deliveryZone = GameObject.CreatePrimitive(PrimitiveType.Cube);
        deliveryZone.name = "DroneDeliveryZone";
        deliveryZone.transform.SetParent(task3Folder.transform);
        deliveryZone.transform.localScale = new Vector3(1.5f, 0.05f, 1.5f);
        deliveryZone.transform.position = new Vector3(50, 100.05f, 5);
        ApplyMaterial(deliveryZone, "SM_DeliveryZone");

        DroneRescue droneTask = task3Folder.AddComponent<DroneRescue>();
        SetPrivateField(droneTask, "droneObject", droneBody);
        SetPrivateField(droneTask, "droneLedge", droneLedge.transform);
        SetPrivateField(droneTask, "deliveryZone", deliveryZone.transform);
        SetPrivateField(droneTask, "rotorTransforms", rotors);

        CreateEmpty(task3Folder.transform, "DroneRescueCheckpoint", new Vector3(50, 100.1f, 7));

        #endregion

        #region 5. Task 4 — Glass Bridge

        GameObject task4Folder = CreateFolder("Task4_GlassBridge", root.transform);
        GameObject glassBridgeParent = CreateFolder("GlassBridge", task4Folder.transform);

        Vector3 bridgeStartPos = new Vector3(25, 100.5f, 7);
        GameObject[] glassPanels = new GameObject[8];
        for (int i = 0; i < 8; i++)
        {
            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = $"GlassPanel_{i + 1:00}";
            panel.transform.SetParent(glassBridgeParent.transform);
            panel.transform.localScale = new Vector3(1.2f, 0.05f, 1.5f);
            panel.transform.position = bridgeStartPos + new Vector3(i * 1.5f, 0, i * 0.5f);
            panel.tag = "GlassBridge";
            ApplyMaterial(panel, "SM_Glass");
            glassPanels[i] = panel;
        }

        Transform bridgeStartT = CreateEmpty(task4Folder.transform, "BridgeStart", bridgeStartPos);
        Vector3 bridgeEndPos = bridgeStartPos + new Vector3(7 * 1.5f, 0, 7 * 0.5f);
        Transform bridgeEndT = CreateEmpty(task4Folder.transform, "BridgeEnd", bridgeEndPos);

        Material crackedMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/SM_GlassCracked.mat");

        GlassBridgeCrossing glassTask = task4Folder.AddComponent<GlassBridgeCrossing>();
        SetPrivateField(glassTask, "bridgeStart", bridgeStartT);
        SetPrivateField(glassTask, "bridgeEnd", bridgeEndT);
        SetPrivateField(glassTask, "glassPanels", glassPanels);
        SetPrivateField(glassTask, "crackablePanelIndices", new int[] { 2, 4, 6 });
        SetPrivateField(glassTask, "crackMaterial", crackedMat);

        #endregion

        #region 6. Checkpoints

        GameObject checksFolder = CreateFolder("Checkpoints", root.transform);
        Transform cp0 = CreateEmpty(checksFolder.transform, "Checkpoint_Start", new Vector3(0, 100.5f, 0));
        Transform cp1 = CreateEmpty(checksFolder.transform, "Checkpoint_Task2", new Vector3(27, 100.5f, 4));
        Transform cp2 = CreateEmpty(checksFolder.transform, "Checkpoint_Task3", new Vector3(50, 100.5f, 5));
        Transform cp3 = CreateEmpty(checksFolder.transform, "Checkpoint_Task4", bridgeStartPos);
        Transform[] checkpointArray = { cp0, cp1, cp2, cp3 };

        #endregion

        #region 7. Managers

        GameObject mgrFolder = CreateFolder("Managers", root.transform);

        GameObject windSystemGO = new GameObject("WindSystem");
        windSystemGO.transform.SetParent(mgrFolder.transform);
        WindForceSystem windSystem = windSystemGO.AddComponent<WindForceSystem>();

        GameObject missionMgrGO = new GameObject("MissionManager");
        missionMgrGO.transform.SetParent(mgrFolder.transform);

        #endregion

        #region 8. XR Origin — Player Systems

        GameObject xrOrigin = FindXROrigin();
        BalanceSystem balanceSystem = null;
        FallDetector fallDetector = null;
        ScreenFader screenFader = null;

        if (xrOrigin != null)
        {
            balanceSystem = xrOrigin.GetComponent<BalanceSystem>();
            if (balanceSystem == null) balanceSystem = xrOrigin.AddComponent<BalanceSystem>();

            JumpSystem jumpSystem = xrOrigin.GetComponent<JumpSystem>();
            if (jumpSystem == null) jumpSystem = xrOrigin.AddComponent<JumpSystem>();
            SetPrivateField(jumpSystem, "jumpMode", (int)JumpMode.StepOver);

            screenFader = xrOrigin.GetComponent<ScreenFader>();
            if (screenFader == null) screenFader = xrOrigin.AddComponent<ScreenFader>();

            ComfortVignette vignette = xrOrigin.GetComponent<ComfortVignette>();
            if (vignette == null) vignette = xrOrigin.AddComponent<ComfortVignette>();

            fallDetector = xrOrigin.GetComponent<FallDetector>();
            if (fallDetector == null) fallDetector = xrOrigin.AddComponent<FallDetector>();
            SetPrivateField(fallDetector, "checkpoints", checkpointArray);
            SetPrivateField(fallDetector, "screenFader", screenFader);

            try { xrOrigin.tag = "Player"; } catch { }

            Debug.Log($"Skyline Mission: Player systems attached to '{xrOrigin.name}'");
        }
        else
        {
            Debug.LogWarning("Skyline Mission: Could not find XR Origin! Add player components manually.");
        }

        #endregion

        #region 9. HUD

        GameObject hudFolder = CreateFolder("HUD", root.transform);
        Transform mainCamera = FindMainCamera(xrOrigin);

        GameObject hudGO = CreateWorldSpaceCanvas(hudFolder.transform, "MissionHUD", 500, 200, 0.002f, mainCamera);

        TMP_FontAsset font = FindTMPFont();
        Transform hudCanvasT = hudGO.transform;

        // Position HUD in world space initially; SkylineHUD handles following
        hudGO.transform.position = mainCamera != null ? mainCamera.position + mainCamera.forward * 2.5f : new Vector3(0, 100.5f, 2.5f);
        hudGO.transform.rotation = Quaternion.identity;

        // Font sizes tuned for world-space VR canvas at 0.002 scale
        GameObject objText = CreateTMPText(hudCanvasT, "ObjectiveText", "", font,
            new Vector2(0, 50), new Vector2(480, 60), 0.35f, TextAlignmentOptions.Center);
        GameObject progText = CreateTMPText(hudCanvasT, "ProgressText", "Task 0/4", font,
            new Vector2(0, -10), new Vector2(480, 40), 0.2f, TextAlignmentOptions.Center);
        GameObject timerTextGO = CreateTMPText(hudCanvasT, "TimerText", "00:00", font,
            new Vector2(0, -50), new Vector2(200, 40), 0.25f, TextAlignmentOptions.Center);
        GameObject notifText = CreateTMPText(hudCanvasT, "NotificationText", "", font,
            new Vector2(0, -90), new Vector2(480, 40), 0.18f, TextAlignmentOptions.Center);
        notifText.SetActive(false);

        SkylineHUD hud = hudGO.AddComponent<SkylineHUD>();
        SetPrivateField(hud, "objectiveText", objText.GetComponent<TextMeshProUGUI>());
        SetPrivateField(hud, "progressText", progText.GetComponent<TextMeshProUGUI>());
        SetPrivateField(hud, "timerText", timerTextGO.GetComponent<TextMeshProUGUI>());
        SetPrivateField(hud, "notificationText", notifText.GetComponent<TextMeshProUGUI>());
        SetPrivateField(hud, "hudCanvas", hudGO.GetComponent<Canvas>());
        if (mainCamera != null)
            SetPrivateField(hud, "followTarget", mainCamera);

        #endregion

        #region 10. Mission Complete Screen

        Transform completeParent = mgrFolder.transform;
        GameObject completeGO = CreateWorldSpaceCanvas(completeParent, "MissionCompleteUI", 600, 400, 0.003f, mainCamera);
        completeGO.transform.position = mainCamera != null ? mainCamera.position + mainCamera.forward * 2f : new Vector3(0, 100.5f, 2f);
        completeGO.transform.rotation = Quaternion.identity;
        Transform completeCanvas = completeGO.transform;

        GameObject panelRoot = new GameObject("PanelRoot");
        panelRoot.transform.SetParent(completeCanvas);
        RectTransform panelRT = panelRoot.AddComponent<RectTransform>();
        panelRT.anchoredPosition = Vector2.zero;
        panelRT.sizeDelta = new Vector2(580, 380);
        Image panelBg = panelRoot.AddComponent<Image>();
        panelBg.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);

        // Font sizes tuned for world-space VR canvas at 0.003 scale
        CreateTMPText(panelRoot.transform, "TitleText", "MISSION COMPLETE", font,
            new Vector2(0, 150), new Vector2(500, 60), 0.4f, TextAlignmentOptions.Center);
        GameObject timeTextGO = CreateTMPText(panelRoot.transform, "TimeText", "00:00", font,
            new Vector2(0, 80), new Vector2(400, 40), 0.25f, TextAlignmentOptions.Center);
        GameObject fallTextGO = CreateTMPText(panelRoot.transform, "FallCountText", "0", font,
            new Vector2(0, 40), new Vector2(400, 40), 0.25f, TextAlignmentOptions.Center);
        GameObject tasksTextGO = CreateTMPText(panelRoot.transform, "TasksText", "4/4", font,
            new Vector2(0, 0), new Vector2(400, 40), 0.25f, TextAlignmentOptions.Center);
        GameObject ratingTextGO = CreateTMPText(panelRoot.transform, "RatingText", "Great Job!", font,
            new Vector2(0, -50), new Vector2(400, 50), 0.3f, TextAlignmentOptions.Center);

        GameObject[] stars = new GameObject[3];
        for (int i = 0; i < 3; i++)
        {
            stars[i] = CreateTMPText(panelRoot.transform, $"Star_{i + 1}", "\u2605", font,
                new Vector2(-40 + i * 40, -100), new Vector2(40, 40), 0.3f, TextAlignmentOptions.Center);
        }

        GameObject retryBtnGO = CreateUIButton(panelRoot.transform, "RetryButton", "RETRY", font,
            new Vector2(-80, -155), new Vector2(140, 45));
        GameObject exitBtnGO = CreateUIButton(panelRoot.transform, "ExitButton", "EXIT", font,
            new Vector2(80, -155), new Vector2(140, 45));

        MissionCompleteScreen mcs = completeGO.AddComponent<MissionCompleteScreen>();
        SetPrivateField(mcs, "panelRoot", panelRoot);
        SetPrivateField(mcs, "timeText", timeTextGO.GetComponent<TextMeshProUGUI>());
        SetPrivateField(mcs, "fallCountText", fallTextGO.GetComponent<TextMeshProUGUI>());
        SetPrivateField(mcs, "tasksText", tasksTextGO.GetComponent<TextMeshProUGUI>());
        SetPrivateField(mcs, "ratingText", ratingTextGO.GetComponent<TextMeshProUGUI>());
        SetPrivateField(mcs, "starImages", stars);
        SetPrivateField(mcs, "retryButton", retryBtnGO.GetComponent<Button>());
        SetPrivateField(mcs, "exitButton", exitBtnGO.GetComponent<Button>());
        SetPrivateField(mcs, "panelCanvas", completeGO.GetComponent<Canvas>());

        #endregion

        #region 11. Wire Mission Manager

        SkylineMissionManager missionManager = missionMgrGO.AddComponent<SkylineMissionManager>();
        SetPrivateField(missionManager, "balanceSystem", balanceSystem);
        SetPrivateField(missionManager, "fallDetector", fallDetector);
        SetPrivateField(missionManager, "screenFader", screenFader);
        SetPrivateField(missionManager, "plankTask", plankTask);
        SetPrivateField(missionManager, "platformTask", platformTask);
        SetPrivateField(missionManager, "droneTask", droneTask);
        SetPrivateField(missionManager, "glassTask", glassTask);

        #endregion

        Debug.Log("\u2713 Skyline Mission Environment built successfully! Save your scene (Ctrl+S).");
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    [MenuItem("Skyline Mission/Clear Mission Environment")]
    public static void ClearEnvironment()
    {
        GameObject existing = GameObject.Find(RootName);
        if (existing != null) Undo.DestroyObjectImmediate(existing);

        GameObject xr = FindXROrigin();
        if (xr != null)
        {
            RemoveComp<ComfortVignette>(xr);
            RemoveComp<ScreenFader>(xr);
            RemoveComp<FallDetector>(xr);
            RemoveComp<JumpSystem>(xr);
            RemoveComp<BalanceSystem>(xr);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("\u2713 Skyline Mission Environment cleared.");
    }

    [MenuItem("Skyline Mission/Start Mission (Play Mode Only)")]
    public static void StartMissionPlayMode()
    {
        if (!Application.isPlaying) { Debug.LogWarning("Only works in Play Mode."); return; }
        if (SkylineMissionManager.Instance != null) SkylineMissionManager.Instance.StartMission();
    }

    #region Helpers

    private static GameObject FindXROrigin()
    {
        string[] names = { "XR Origin (XR Rig)", "XR Origin", "Complete XR Origin Set Up Variant" };
        foreach (string n in names)
        {
            GameObject go = GameObject.Find(n);
            if (go != null) return go;
        }
        var origin = Object.FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
        return origin != null ? origin.gameObject : null;
    }

    private static Transform FindMainCamera(GameObject xrOrigin)
    {
        if (xrOrigin != null)
        {
            Camera cam = xrOrigin.GetComponentInChildren<Camera>();
            if (cam != null) return cam.transform;
        }
        if (Camera.main != null) return Camera.main.transform;
        return null;
    }

    private static GameObject CreateFolder(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        return go;
    }

    private static Transform CreateEmpty(Transform parent, string name, Vector3 pos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = pos;
        return go.transform;
    }

    private static PlatformPhysics CreatePlatformWithPhysics(Transform parent, string name,
        Vector3 scale, Vector3 pos, bool crumbles)
    {
        GameObject plat = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plat.name = name;
        plat.transform.SetParent(parent);
        plat.transform.localScale = scale;
        plat.transform.position = pos;
        plat.tag = "Platform";
        ApplyMaterial(plat, "SM_Concrete");

        GameObject trig = new GameObject(name + "_Trigger");
        trig.transform.SetParent(plat.transform);
        trig.transform.localPosition = Vector3.zero;
        BoxCollider bc = trig.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.size = new Vector3(1, 3, 1);

        PlatformPhysics pp = plat.AddComponent<PlatformPhysics>();
        SetPrivateField(pp, "canCrumble", crumbles);
        if (crumbles)
        {
            SetPrivateField(pp, "crumbleDelay", 4f);
            SetPrivateField(pp, "respawnAfterCrumble", true);
            SetPrivateField(pp, "respawnDelay", 3f);
        }
        SetPrivateField(pp, "enableWobble", true);
        SetPrivateField(pp, "wobbleAmplitude", 0.8f);

        return pp;
    }

    private static void CreateBuilding(Transform parent, string name,
        Vector3 roofScale, Vector3 roofPos, Vector3 bodyScale, Vector3 bodyPos)
    {
        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.name = name + "_Rooftop";
        roof.transform.SetParent(parent);
        roof.transform.localScale = roofScale;
        roof.transform.position = roofPos;
        ApplyMaterial(roof, "SM_Concrete");

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = name + "_Body";
        body.transform.SetParent(parent);
        body.transform.localScale = bodyScale;
        body.transform.position = bodyPos;
        ApplyMaterial(body, "SM_ConcreteDark");
    }

    private static GameObject CreateWorldSpaceCanvas(Transform parent, string name,
        float width, float height, float scale, Transform eventCameraTransform = null)
    {
        GameObject canvasGO = new GameObject(name);
        canvasGO.transform.SetParent(parent);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        if (eventCameraTransform != null)
        {
            Camera cam = eventCameraTransform.GetComponent<Camera>();
            if (cam != null) canvas.worldCamera = cam;
        }
        
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        RectTransform rt = canvasGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, height);
        canvasGO.transform.localScale = new Vector3(scale, scale, scale);

        return canvasGO;
    }

    private static TMP_FontAsset FindTMPFont()
    {
        string[] fonts = AssetDatabase.FindAssets("t:TMP_FontAsset");
        if (fonts.Length > 0)
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(fonts[0]));
        Debug.LogWarning("Skyline Mission: No TMP_FontAsset found.");
        return null;
    }

    private static GameObject CreateTMPText(Transform parent, string name, string text,
        TMP_FontAsset font, Vector2 pos, Vector2 size, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        if (font != null) tmp.font = font;

        return go;
    }

    private static GameObject CreateUIButton(Transform parent, string name, string label,
        TMP_FontAsset font, Vector2 pos, Vector2 size)
    {
        GameObject btnGO = new GameObject(name);
        btnGO.transform.SetParent(parent);
        RectTransform rt = btnGO.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = btnGO.AddComponent<Image>();
        img.color = new Color(0.25f, 0.25f, 0.35f, 1f);

        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = img;

        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(btnGO.transform);
        RectTransform labelRT = labelGO.AddComponent<RectTransform>();
        labelRT.anchoredPosition = Vector2.zero;
        labelRT.sizeDelta = size;
        TextMeshProUGUI tmpLabel = labelGO.AddComponent<TextMeshProUGUI>();
        tmpLabel.text = label;
        tmpLabel.fontSize = 0.25f;
        tmpLabel.alignment = TextAlignmentOptions.Center;
        tmpLabel.color = Color.white;
        if (font != null) tmpLabel.font = font;

        return btnGO;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var so = new SerializedObject((Object)target);
        var prop = so.FindProperty(fieldName);
        if (prop == null)
        {
            // Fallback to reflection for arrays
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public);
            if (field != null)
            {
                field.SetValue(target, value);
                EditorUtility.SetDirty((Object)target);
            }
            else
            {
                Debug.LogWarning($"Could not find field '{fieldName}' on {target.GetType().Name}");
            }
            return;
        }

        switch (prop.propertyType)
        {
            case SerializedPropertyType.Float:
                prop.floatValue = (float)value;
                break;
            case SerializedPropertyType.Boolean:
                prop.boolValue = (bool)value;
                break;
            case SerializedPropertyType.Integer:
                prop.intValue = (int)value;
                break;
            case SerializedPropertyType.String:
                prop.stringValue = (string)value;
                break;
            case SerializedPropertyType.ObjectReference:
                prop.objectReferenceValue = (Object)value;
                break;
            default:
                var fallbackField = target.GetType().GetField(fieldName,
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public);
                if (fallbackField != null)
                {
                    fallbackField.SetValue(target, value);
                    EditorUtility.SetDirty((Object)target);
                }
                break;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void RemoveComp<T>(GameObject go) where T : Component
    {
        T comp = go.GetComponent<T>();
        if (comp != null) Undo.DestroyObjectImmediate(comp);
    }

    #endregion

    #region Materials

    private static void EnsureMaterialsExist()
    {
        if (!Directory.Exists(MaterialsPath))
        {
            Directory.CreateDirectory(MaterialsPath);
            AssetDatabase.Refresh();
        }

        CreateLitMaterial("SM_Concrete", new Color(0.5f, 0.5f, 0.52f));
        CreateLitMaterial("SM_ConcreteDark", new Color(0.3f, 0.3f, 0.32f));
        CreateLitMaterial("SM_Wood", new Color(0.55f, 0.35f, 0.15f));
        CreateLitMaterial("SM_DronRed", new Color(0.8f, 0.15f, 0.1f));
        CreateLitMaterial("SM_Ground", new Color(0.15f, 0.15f, 0.18f));

        CreateTransparentMaterial("SM_Glass", new Color(0.7f, 0.85f, 0.95f, 0.15f));
        CreateTransparentMaterial("SM_DeliveryZone", new Color(0.2f, 0.8f, 0.3f, 0.3f));
        CreateTransparentMaterial("SM_GlassCracked", new Color(0.8f, 0.75f, 0.6f, 0.3f));

        AssetDatabase.SaveAssets();
    }

    private static void CreateLitMaterial(string name, Color color)
    {
        string path = $"{MaterialsPath}/{name}.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(path) == null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(mat, path);
        }
    }

    private static void CreateTransparentMaterial(string name, Color color)
    {
        string path = $"{MaterialsPath}/{name}.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(path) == null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Surface", 1);
            mat.SetFloat("_Blend", 0);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            AssetDatabase.CreateAsset(mat, path);
        }
    }

    private static void ApplyMaterial(GameObject go, string matName)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/{matName}.mat");
        if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
    }

    #endregion

    #region Tags

    private static void AddRequiredTags()
    {
        AddTag("NarrowPath");
        AddTag("Platform");
        AddTag("GlassBridge");
        AddTag("SafeZone");
    }

    private static void AddTag(string tag)
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadMainAssetAtPath("ProjectSettings/TagManager.asset"));
        var tags = tagManager.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
        {
            if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;
        }
        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        tagManager.ApplyModifiedProperties();
    }

    #endregion
}
#endif
