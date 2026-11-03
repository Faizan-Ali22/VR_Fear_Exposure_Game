#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FearExposure.Level1.Editor
{
    public static class Level1HierarchyBuilder
    {
        private const string RootName = "[Level1]";

        [InitializeOnLoadMethod]
        private static void AutoRun()
        {
            EditorApplication.delayCall += () =>
            {
                CreateHierarchy();
            };
        }

        [MenuItem("Level 1/Create Hierarchy (Empty Objects)")]
        public static void CreateHierarchy()
        {
            Scene scene = SceneManager.GetActiveScene();
            Debug.Log($"[Level1] CreateHierarchy running on scene: '{scene.name}' (path: '{scene.path}')");

            GameObject root = GameObject.Find(RootName);
            if (root == null)
            {
                root = new GameObject(RootName);
                Undo.RegisterCreatedObjectUndo(root, "Create " + RootName);
            }
            ResetTransform(root.transform);

            // Systems
            GameObject systems = GetOrCreateChild(root.transform, "Systems");
            GetOrCreateChild(systems.transform, "CourageScore");
            GetOrCreateChild(systems.transform, "FadeOverlay");
            
            GameObject ambience = GetOrCreateChild(systems.transform, "Ambience");
            GetOrCreateChild(ambience.transform, "Room_Tone");
            GetOrCreateChild(ambience.transform, "City_Bed");
            GetOrCreateChild(ambience.transform, "Music");

            GetOrCreateChild(systems.transform, "InstructionPanel");
            
            GameObject completionScreen = GetOrCreateChild(systems.transform, "CompletionScreen");
            GameObject completionCanvas = GetOrCreateChild(completionScreen.transform, "CompletionCanvas");
            completionCanvas.SetActive(false);

            GetOrCreateChild(systems.transform, "ComfortReturn");

            // SpawnPoint
            GetOrCreateChild(root.transform, "SpawnPoint");

            // World
            GameObject world = GetOrCreateChild(root.transform, "World");
            GetOrCreateChild(world.transform, "RoomDoor");
            
            GameObject elevator = GetOrCreateChild(world.transform, "Elevator");
            GetOrCreateChild(elevator.transform, "CabDoor");
            GetOrCreateChild(elevator.transform, "ArrivalDoor");
            GetOrCreateChild(elevator.transform, "ArrivalStandPoint");
            GetOrCreateChild(elevator.transform, "Rumble");
            GetOrCreateChild(elevator.transform, "Ding");

            GameObject restArea = GetOrCreateChild(world.transform, "RestArea");
            GetOrCreateChild(restArea.transform, "BreathingOrb");

            GetOrCreateChild(world.transform, "SafeSpot_A");
            GetOrCreateChild(world.transform, "SafeSpot_B");
            GetOrCreateChild(world.transform, "SafetyBoard");

            // Step1_SafeRoom
            GameObject step1 = GetOrCreateChild(root.transform, "Step1_SafeRoom");
            GetOrCreateChild(step1.transform, "SafetyBoardLook");
            GetOrCreateChild(step1.transform, "Zone_WindowApproach");

            // Step2_Window
            GameObject step2 = GetOrCreateChild(root.transform, "Step2_Window");
            GetOrCreateChild(step2.transform, "Zone_Stand");
            GetOrCreateChild(step2.transform, "WindowMarker");
            GetOrCreateChild(step2.transform, "BuildingTarget");
            GetOrCreateChild(step2.transform, "BuildingBeacon");

            // Step3_Hallway
            GameObject step3 = GetOrCreateChild(root.transform, "Step3_Hallway");
            GameObject floorGuide = GetOrCreateChild(step3.transform, "FloorGuide");
            GetOrCreateChild(floorGuide.transform, "WP_0");
            GetOrCreateChild(floorGuide.transform, "WP_1");
            GetOrCreateChild(floorGuide.transform, "WP_2");
            GetOrCreateChild(step3.transform, "Zone_ElevatorLobby");

            // Step4_Elevator
            GameObject step4 = GetOrCreateChild(root.transform, "Step4_Elevator");
            GetOrCreateChild(step4.transform, "Zone_Cab");

            // Step5_Rooftop
            GameObject step5 = GetOrCreateChild(root.transform, "Step5_Rooftop");
            GetOrCreateChild(step5.transform, "Zone_Terrace");
            GetOrCreateChild(step5.transform, "CityViewTarget");

            // Step6_FindTargets
            GameObject step6 = GetOrCreateChild(root.transform, "Step6_FindTargets");
            GetOrCreateChild(step6.transform, "Zone_Observation");
            GetOrCreateChild(step6.transform, "MissionMarker");
            GetOrCreateChild(step6.transform, "Collectible_Bonus");
            GetOrCreateChild(step6.transform, "ObjectivePanel");
            GetOrCreateChild(step6.transform, "Target_Red");
            GetOrCreateChild(step6.transform, "Target_Green");
            GetOrCreateChild(step6.transform, "Target_Blue");
            GetOrCreateChild(step6.transform, "Hint_Red");
            GetOrCreateChild(step6.transform, "Hint_Green");
            GetOrCreateChild(step6.transform, "Hint_Blue");

            // Step7_Final
            GameObject step7 = GetOrCreateChild(root.transform, "Step7_Final");
            GetOrCreateChild(step7.transform, "Zone_Viewing");
            GetOrCreateChild(step7.transform, "ViewDirection");
            GetOrCreateChild(step7.transform, "DurationPicker");
            GetOrCreateChild(step7.transform, "FinishEarlyButton");
            GetOrCreateChild(step7.transform, "ProgressCanvas");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Level1] Successfully created/verified Level 1 hierarchy in " + scene.name);
        }

        private static GameObject GetOrCreateChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child == null)
            {
                GameObject go = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(go, "Create " + name);
                go.transform.SetParent(parent, false);
                ResetTransform(go.transform);
                return go;
            }
            else
            {
                ResetTransform(child);
                return child.gameObject;
            }
        }

        private static void ResetTransform(Transform t)
        {
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
        }
    }
}
#endif
