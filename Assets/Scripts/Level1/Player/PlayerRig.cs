using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace FearExposure.Level1
{
    /// <summary>
    /// One place that knows about the player. Put it on the XR Origin (XR Rig) object.
    /// Every other script asks PlayerRig for the head position, feet position, teleporting,
    /// locking movement and haptics, so none of them need XR references of their own.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public class PlayerRig : MonoBehaviour
    {
        public static PlayerRig Instance { get; private set; }

        [Header("References (auto-filled when left empty)")]
        [Tooltip("The XR Origin root. Empty = this object.")]
        [SerializeField] private Transform rigRoot;
        [Tooltip("The Main Camera under Camera Offset. Empty = first Camera found under this object.")]
        [SerializeField] private Camera headCamera;
        [Tooltip("The Character Controller on the XR Origin (optional).")]
        [SerializeField] private CharacterController characterController;

        [Header("Assign by hand")]
        [Tooltip("XR Origin > Locomotion. Switched off during the elevator ride so the player cannot walk.")]
        [SerializeField] private GameObject locomotionRoot;
        [Tooltip("XR Origin > Camera Offset > Left Controller (optional, used for reaching collectibles).")]
        [SerializeField] private Transform leftHand;
        [Tooltip("XR Origin > Camera Offset > Right Controller (optional).")]
        [SerializeField] private Transform rightHand;

        private static readonly List<InputDevice> s_Devices = new List<InputDevice>(4);
        private Transform _originalParent;

        public Transform RigRoot { get { return rigRoot; } }
        public Transform Head { get { return headCamera != null ? headCamera.transform : null; } }

        /// <summary>True while a script (the elevator) has taken movement away from the player.</summary>
        public bool InputLocked { get; private set; }

        /// <summary>Point on the floor directly below the head.</summary>
        public Vector3 FeetPosition
        {
            get
            {
                Vector3 h = headCamera.transform.position;
                return new Vector3(h.x, rigRoot.position.y, h.z);
            }
        }

        /// <summary>Waist-height point below the head. Zones test this point.</summary>
        public Vector3 BodyPosition { get { return FeetPosition + Vector3.up * 0.5f; } }

        /// <summary>Compass direction the head faces, in degrees (0 = world +Z).</summary>
        public float HeadYaw
        {
            get
            {
                Vector3 f = headCamera.transform.forward;
                return Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
            }
        }

        /// <summary>Degrees the head looks DOWN (negative when looking up).</summary>
        public float HeadPitchDown
        {
            get
            {
                float y = Mathf.Clamp(-headCamera.transform.forward.y, -1f, 1f);
                return Mathf.Asin(y) * Mathf.Rad2Deg;
            }
        }

        private void Awake()
        {
            Instance = this;
            if (rigRoot == null) rigRoot = transform;
            if (headCamera == null) headCamera = GetComponentInChildren<Camera>(true);
            if (characterController == null) characterController = GetComponent<CharacterController>();
            _originalParent = rigRoot.parent;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnDisable()
        {
            // Never leave the player locked if this object is switched off mid-ride.
            if (InputLocked) SetLocomotionEnabled(true);
        }

        // ---------------------------------------------------------------- movement

        public void SetLocomotionEnabled(bool on)
        {
            InputLocked = !on;
            if (locomotionRoot != null) locomotionRoot.SetActive(on);
        }

        /// <summary>Parent the whole rig to a moving object (physical elevator).</summary>
        public void AttachTo(Transform parent)
        {
            SetCharacterControllerEnabled(false);
            rigRoot.SetParent(parent, true);
        }

        /// <summary>Undo AttachTo.</summary>
        public void Detach()
        {
            rigRoot.SetParent(_originalParent, true);
            SetCharacterControllerEnabled(true);
            Physics.SyncTransforms();
        }

        public void SetCharacterControllerEnabled(bool on)
        {
            if (characterController != null) characterController.enabled = on;
        }

        /// <summary>
        /// Moves the rig so the player's HEAD ends up above floorPoint and faces yawDegrees.
        /// Works for any play-space position, in headset or simulator.
        /// </summary>
        public void PlaceFeetAt(Vector3 floorPoint, float yawDegrees)
        {
            bool ccWasOn = characterController != null && characterController.enabled;
            if (ccWasOn) characterController.enabled = false;

            Transform head = headCamera.transform;
            float delta = Mathf.DeltaAngle(HeadYaw, yawDegrees);
            rigRoot.RotateAround(head.position, Vector3.up, delta);

            Vector3 headPos = head.position;
            rigRoot.position += new Vector3(
                floorPoint.x - headPos.x,
                floorPoint.y - rigRoot.position.y,
                floorPoint.z - headPos.z);

            if (ccWasOn) characterController.enabled = true;
            Physics.SyncTransforms();
        }

        // ---------------------------------------------------------------- reach + haptics

        /// <summary>True when the head or either hand is close enough to a point.</summary>
        public bool IsWithinReach(Vector3 point, float headRadius, float handRadius)
        {
            Transform h = Head;
            if (h != null && (h.position - point).sqrMagnitude <= headRadius * headRadius) return true;
            float hr = handRadius * handRadius;
            if (leftHand != null && (leftHand.position - point).sqrMagnitude <= hr) return true;
            if (rightHand != null && (rightHand.position - point).sqrMagnitude <= hr) return true;
            return false;
        }

        /// <summary>Short buzz on both controllers. Silently does nothing in the simulator.</summary>
        public void Pulse(float amplitude, float seconds)
        {
            s_Devices.Clear();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller, s_Devices);
            for (int i = 0; i < s_Devices.Count; i++)
                s_Devices[i].SendHapticImpulse(0, amplitude, seconds);
        }

        // ---------------------------------------------------------------- validation

        public void Validate(ValidationReport report)
        {
            Camera cam = headCamera != null ? headCamera : GetComponentInChildren<Camera>(true);
            report.Require(cam, "Head Camera (no Camera found under the XR Origin)", this);
            if (cam != null && cam.nearClipPlane > 0.15f)
                report.Warn("Camera Near Clip is " + cam.nearClipPlane.ToString("0.00") + ". Set it to 0.1 or lower so the fade overlay can be seen.", this);
            if (locomotionRoot == null)
                report.Warn("Locomotion Root is empty - the player will still be able to walk during the elevator ride.", this);
            if (leftHand == null || rightHand == null)
                report.Warn("Left/Right Hand are empty - collectibles can only be picked up by walking close to them.", this);
        }
    }
}
