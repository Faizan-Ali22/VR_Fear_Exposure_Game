#if UNITY_EDITOR
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FearExposure.Level1.Editor
{
    public static class AddEnvironmentColliders
    {
        [MenuItem("Level 1/Add Colliders From Floor 50 to Rooftop")]
        public static void AddColliders()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== ADDING COLLIDERS: FLOOR 50 TO ROOFTOP ===");

            GameObject env = GameObject.Find("Envirnment");
            if (env == null)
            {
                Debug.LogError("GameObject 'Envirnment' not found!");
                return;
            }

            // 1. Elevator Cab Colliders (Box colliders for floor, walls, ceiling)
            Transform elevatorT = env.transform.Find("Elevator");
            if (elevatorT != null)
            {
                Transform carShellT = elevatorT.Find("Elevator_CarShell");
                if (carShellT != null)
                {
                    // Clean up any old colliders on CarShell
                    var oldCols = carShellT.GetComponents<BoxCollider>();
                    foreach (var c in oldCols) Object.DestroyImmediate(c);

                    var mf = carShellT.GetComponent<MeshFilter>();
                    MeshRenderer mr = carShellT.GetComponent<MeshRenderer>();
                    Bounds b = mr != null ? mr.bounds : new Bounds(carShellT.position, new Vector3(2.45f, 2.66f, 2.2f));

                    // Convert bounds center to local space
                    Vector3 localCenter = carShellT.InverseTransformPoint(b.center);
                    Vector3 localExtents = carShellT.InverseTransformVector(b.extents);
                    localExtents = new Vector3(Mathf.Abs(localExtents.x), Mathf.Abs(localExtents.y), Mathf.Abs(localExtents.z));

                    // Floor box collider
                    var floorBox = carShellT.gameObject.AddComponent<BoxCollider>();
                    floorBox.center = new Vector3(localCenter.x, localCenter.y - localExtents.y + 0.05f, localCenter.z);
                    floorBox.size = new Vector3(localExtents.x * 2f, 0.12f, localExtents.z * 2f);

                    // Ceiling box collider
                    var ceilBox = carShellT.gameObject.AddComponent<BoxCollider>();
                    ceilBox.center = new Vector3(localCenter.x, localCenter.y + localExtents.y - 0.05f, localCenter.z);
                    ceilBox.size = new Vector3(localExtents.x * 2f, 0.12f, localExtents.z * 2f);

                    // Back wall (positive X)
                    var backBox = carShellT.gameObject.AddComponent<BoxCollider>();
                    backBox.center = new Vector3(localCenter.x + localExtents.x - 0.05f, localCenter.y, localCenter.z);
                    backBox.size = new Vector3(0.12f, localExtents.y * 2f, localExtents.z * 2f);

                    // Left wall (negative Z)
                    var leftBox = carShellT.gameObject.AddComponent<BoxCollider>();
                    leftBox.center = new Vector3(localCenter.x, localCenter.y, localCenter.z - localExtents.z + 0.05f);
                    leftBox.size = new Vector3(localExtents.x * 2f, localExtents.y * 2f, 0.12f);

                    // Right wall (positive Z)
                    var rightBox = carShellT.gameObject.AddComponent<BoxCollider>();
                    rightBox.center = new Vector3(localCenter.x, localCenter.y, localCenter.z + localExtents.z - 0.05f);
                    rightBox.size = new Vector3(localExtents.x * 2f, localExtents.y * 2f, 0.12f);

                    sb.AppendLine("Configured 5 robust BoxColliders on Elevator_CarShell (Floor, Ceiling, Back, Left, Right).");
                }
            }

            // 2. Iterate all MeshFilters under Envirnment
            var meshFilters = env.GetComponentsInChildren<MeshFilter>(true);
            int addedCount = 0;
            int skippedCount = 0;

            foreach (var mf in meshFilters)
            {
                if (mf.sharedMesh == null) continue;

                // Skip buttons which already have BoxColliders
                if (mf.name.StartsWith("LiftButton_") || mf.name.StartsWith("CallButton_")) continue;
                // Skip FloorCounter or text objects
                if (mf.name == "FloorCounter" || mf.name == "LandingFloorCounter") continue;
                // Skip Elevator_CarShell since we handle it explicitly with 5 box colliders
                if (mf.name == "Elevator_CarShell") continue;

                MeshRenderer mr = mf.GetComponent<MeshRenderer>();
                if (mr == null) continue;

                float minY = mr.bounds.min.y;
                float maxY = mr.bounds.max.y;

                // We want all environment objects from Floor 50 (~190) to Rooftop (~430)
                // Also include tower segments and shell in that vertical range
                bool inVerticalRange = (maxY >= 195f && minY <= 425f);

                if (!inVerticalRange)
                {
                    skippedCount++;
                    continue;
                }

                Collider col = mf.GetComponent<Collider>();
                if (col == null)
                {
                    // For moving doors (CarDoor, LandingDoor), use BoxCollider for reliable moving collision without PhysX warnings
                    if (mf.name.StartsWith("CarDoor_") || mf.name.StartsWith("LandingDoor_") || mf.name.Contains("Door"))
                    {
                        var bc = mf.gameObject.AddComponent<BoxCollider>();
                        sb.AppendLine($"[ADDED BoxCollider for Door] '{mf.gameObject.name}' Y:[{minY:F1}..{maxY:F1}]");
                    }
                    else
                    {
                        MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                        mc.sharedMesh = mf.sharedMesh;
                        mc.convex = false;
                        sb.AppendLine($"[ADDED MeshCollider] '{mf.gameObject.name}' Y:[{minY:F1}..{maxY:F1}]");
                    }
                    addedCount++;
                }
            }

            // 3. Check and ensure Floor 50 key objects specifically have colliders
            string[] keyObjects = new string[]
            {
                "F50_FloorSlab",
                "F50_Terrace",
                "F50_TerraceGlass",
                "F50_TerraceHandrail",
                "F50_East_Glass",
                "F50_East_Frame",
                "F50_North_Glass",
                "F50_North_Frame",
                "F50_South_Glass",
                "F50_South_Frame",
                "F50_West_Glass",
                "F50_West_Frame",
                "Core_50_Shell",
                "Landing_50_Portal",
                "LandingDoor_50_L",
                "LandingDoor_50_R",
                "Roof_Surface",
                "Roof_Gravel",
                "Walkway_Pavers",
                "Skydeck_GlassFloor",
                "Skydeck_GlassBalustrade",
                "Skydeck_Handrail",
                "Skydeck_Structure",
                "LandingDoor_R_L",
                "LandingDoor_R_R",
                "Elevator_DoorFrame",
                "Penthouse_Shell"
            };

            foreach (var objName in keyObjects)
            {
                GameObject go = GameObject.Find("Envirnment/" + objName);
                if (go == null)
                {
                    // Search in children
                    var child = env.transform.Find(objName);
                    if (child != null) go = child.gameObject;
                    else
                    {
                        var found = env.GetComponentsInChildren<Transform>(true);
                        foreach (var t in found)
                        {
                            if (t.name == objName) { go = t.gameObject; break; }
                        }
                    }
                }

                if (go != null)
                {
                    var c = go.GetComponent<Collider>();
                    if (c == null)
                    {
                        var mf = go.GetComponent<MeshFilter>();
                        if (go.name.StartsWith("LandingDoor_") || go.name.StartsWith("CarDoor_") || go.name.Contains("Door"))
                        {
                            var bc = go.AddComponent<BoxCollider>();
                            sb.AppendLine($"[KEY OBJECT: Added BoxCollider] {objName}");
                        }
                        else if (mf != null && mf.sharedMesh != null)
                        {
                            var mc = go.AddComponent<MeshCollider>();
                            mc.sharedMesh = mf.sharedMesh;
                            sb.AppendLine($"[KEY OBJECT: Added MeshCollider] {objName}");
                        }
                        else
                        {
                            var mr = go.GetComponent<MeshRenderer>();
                            var bc = go.AddComponent<BoxCollider>();
                            if (mr != null) bc.size = mr.bounds.size;
                            sb.AppendLine($"[KEY OBJECT: Added BoxCollider] {objName}");
                        }
                    }
                }
            }

            // Also check RoomDoor panels under [Level1]/World/RoomDoor
            GameObject roomDoor = GameObject.Find("[Level1]/World/RoomDoor");
            if (roomDoor != null)
            {
                var panels = roomDoor.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var p in panels)
                {
                    if (p.GetComponent<Collider>() == null)
                    {
                        var bc = p.gameObject.AddComponent<BoxCollider>();
                        sb.AppendLine($"[ROOM DOOR: Added BoxCollider] {p.name}");
                    }
                }
            }

            sb.AppendLine($"Summary: Added colliders to {addedCount} objects. Skipped {skippedCount} objects outside Y range.");

            // Mark dirty and save scene
            Scene activeScene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveOpenScenes();
            sb.AppendLine($"Scene '{activeScene.name}' marked dirty and saved successfully!");

            Debug.Log(sb.ToString());
        }
    }
}
#endif
