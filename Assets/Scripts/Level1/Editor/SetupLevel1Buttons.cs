#if UNITY_EDITOR
#pragma warning disable CS0618
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEditor.SceneManagement;

namespace FearExposure.Level1.Editor
{
    public static class SetupLevel1Buttons
    {
        [MenuItem("Level 1/Validate Level 1")]
        public static void ValidateLevel()
        {
            var director = Object.FindAnyObjectByType<Level1Director>();
            if (director == null)
            {
                Debug.LogError("Level1Director not found in scene!");
                return;
            }
            var mi = typeof(Level1Director).GetMethod("ValidateFromMenu", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (mi != null) mi.Invoke(director, null);
        }

        [MenuItem("Level 1/Setup All Buttons")]
        public static void Setup()
        {
            // 1. Ensure XRInteractionManager exists in the scene
            var manager = Object.FindAnyObjectByType<XRInteractionManager>();
            if (manager == null)
            {
                var mgrGo = new GameObject("XR Interaction Manager");
                manager = mgrGo.AddComponent<XRInteractionManager>();
                Undo.RegisterCreatedObjectUndo(mgrGo, "Create XR Interaction Manager");
                Debug.Log("Created missing XR Interaction Manager in scene!");
            }
            else
            {
                Debug.Log($"Found existing XR Interaction Manager on '{manager.gameObject.name}'.");
            }

            // Assign interaction manager to all interactors
            var interactors = Object.FindObjectsByType<XRBaseInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var interactor in interactors)
            {
                if (interactor.interactionManager == null)
                {
                    interactor.interactionManager = manager;
                    EditorUtility.SetDirty(interactor);
                }

                // If it is an XRDirectInteractor, ensure it has a trigger collider
                if (interactor is XRDirectInteractor direct)
                {
                    var col = direct.GetComponent<Collider>();
                    if (col == null)
                    {
                        var sc = direct.gameObject.AddComponent<SphereCollider>();
                        sc.isTrigger = true;
                        sc.radius = 0.08f;
                        EditorUtility.SetDirty(direct.gameObject);
                        Debug.Log($"Added trigger SphereCollider to XRDirectInteractor on '{direct.gameObject.name}'.");
                    }
                    else if (!col.isTrigger)
                    {
                        col.isTrigger = true;
                        EditorUtility.SetDirty(direct.gameObject);
                    }
                }
            }
            Debug.Log($"Updated {interactors.Length} interactors with XRInteractionManager.");

            var clickClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Samples/XR Interaction Toolkit/3.5.1/Hands Interaction Demo/DemoAssets/Audio/ButtonClick.wav")
                ?? AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Kenney Audio/kenney_interface-sounds/Audio/click_002.ogg");

            var buttons = Object.FindObjectsByType<WorldButton>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Debug.Log($"Found {buttons.Length} WorldButtons in scene.");

            foreach (var btn in buttons)
            {
                Undo.RecordObject(btn.gameObject, "Setup Button");

                // 2. BoxCollider
                var col = btn.GetComponent<BoxCollider>();
                if (col == null)
                {
                    col = btn.gameObject.AddComponent<BoxCollider>();
                }
                // Ensure generous size
                col.size = new Vector3(Mathf.Max(col.size.x, 0.12f), Mathf.Max(col.size.y, 0.14f), Mathf.Max(col.size.z, 0.12f));

                // 3. XRSimpleInteractable
                var xri = btn.GetComponent<XRSimpleInteractable>();
                if (xri == null)
                {
                    xri = btn.gameObject.AddComponent<XRSimpleInteractable>();
                }
                xri.interactionManager = manager;
                if (!xri.colliders.Contains(col))
                {
                    xri.colliders.Clear();
                    xri.colliders.Add(col);
                }

                // 4. XRPokeFilter
                var poke = btn.GetComponent<XRPokeFilter>();
                if (poke == null)
                {
                    poke = btn.gameObject.AddComponent<XRPokeFilter>();
                }

                // 5. AudioSource & clip
                var audio = btn.GetComponent<AudioSource>();
                if (audio == null)
                {
                    audio = btn.gameObject.AddComponent<AudioSource>();
                    audio.playOnAwake = false;
                    audio.spatialBlend = 1f;
                    audio.minDistance = 1f;
                    audio.maxDistance = 10f;
                }

                // Assign serialized fields on WorldButton via SerializedObject
                var so = new SerializedObject(btn);
                var propAudio = so.FindProperty("audioSource");
                if (propAudio != null && propAudio.objectReferenceValue == null)
                {
                    propAudio.objectReferenceValue = audio;
                }
                var propClip = so.FindProperty("pressClip");
                if (propClip != null && propClip.objectReferenceValue == null && clickClip != null)
                {
                    propClip.objectReferenceValue = clickClip;
                }
                so.ApplyModifiedProperties();

                EditorUtility.SetDirty(btn);
                EditorUtility.SetDirty(btn.gameObject);
                Debug.Log($"Configured WorldButton on '{btn.gameObject.name}' (size={col.size}, clip={clickClip?.name})");
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("Saved scene with updated buttons and XR Interaction Manager.");
        }
    }
}
#endif
