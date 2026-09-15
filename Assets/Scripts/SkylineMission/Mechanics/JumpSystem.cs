using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using InputDevice = UnityEngine.XR.InputDevice;
using CommonUsages = UnityEngine.XR.CommonUsages;

namespace SkylineMission.Mechanics
{
    public enum JumpMode { ArmSwing, ButtonPress, StepOver }

    /// <summary>
    /// Swappable jump mechanic with 3 modes.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class JumpSystem : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Active jump mode")]
        [SerializeField] private JumpMode jumpMode = JumpMode.ArmSwing;
        [Tooltip("Downward velocity threshold for arm swing jump")]
        [SerializeField] private float swingThreshold = 1.5f;
        [Tooltip("Multiplier for swing speed to jump force")]
        [SerializeField] private float swingMultiplier = 3f;
        [Tooltip("Minimum force applied during an arm swing jump")]
        [SerializeField] private float minJumpForce = 2f;
        [Tooltip("Maximum force applied during an arm swing jump")]
        [SerializeField] private float maxJumpForce = 5f;
        [Tooltip("Fixed force applied during a button press jump")]
        [SerializeField] private float fixedJumpForce = 3.5f;
        [Tooltip("Distance threshold for step over jump detection")]
        [SerializeField] private float stepOverDistance = 1f;
        [Tooltip("Multiplier for physics gravity")]
        [SerializeField] private float gravityMultiplier = 1f;
        [Tooltip("Input action for button jump mode")]
        [SerializeField] private InputActionReference jumpButtonAction;

        private CharacterController characterController;
        private float verticalVelocity;
        private bool wasGrounded;
        private bool isStepOverJumping;

        public JumpMode CurrentMode { get => jumpMode; set => jumpMode = value; }
        
        /// <summary>
        /// True if the player is currently grounded.
        /// </summary>
        public bool IsGrounded => characterController.isGrounded || Physics.SphereCast(transform.position, 0.2f, Vector3.down, out _, 0.1f);
        
        /// <summary>
        /// True if the player is currently in the air jumping.
        /// </summary>
        public bool IsJumping => !IsGrounded && verticalVelocity > 0;

        public event System.Action OnJumpStarted;
        public event System.Action OnLanded;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            if (characterController == null)
            {
                Debug.LogError("JumpSystem requires CharacterController.");
            }
        }

        private void OnEnable()
        {
            if (jumpButtonAction != null && jumpButtonAction.action != null)
            {
                jumpButtonAction.action.Enable();
            }
        }

        private void OnDisable()
        {
            if (jumpButtonAction != null && jumpButtonAction.action != null)
            {
                jumpButtonAction.action.Disable();
            }
        }

        private void Update()
        {
            if (characterController == null || !characterController.enabled) return;
            if (isStepOverJumping) return;

            bool currentlyGrounded = IsGrounded;

            if (currentlyGrounded && !wasGrounded && verticalVelocity < 0)
            {
                verticalVelocity = 0;
                OnLanded?.Invoke();
            }

            if (!currentlyGrounded)
            {
                verticalVelocity += Physics.gravity.y * gravityMultiplier * Time.deltaTime;
            }
            else if (verticalVelocity < 0)
            {
                verticalVelocity = -2f; // Keep grounded stick to floor
            }

            wasGrounded = currentlyGrounded;

            switch (jumpMode)
            {
                case JumpMode.ArmSwing:
                    HandleArmSwingJump(currentlyGrounded);
                    break;
                case JumpMode.ButtonPress:
                    HandleButtonPressJump(currentlyGrounded);
                    break;
                case JumpMode.StepOver:
                    HandleStepOverJump(currentlyGrounded);
                    break;
            }

            if (!isStepOverJumping)
            {
                characterController.Move(Vector3.up * verticalVelocity * Time.deltaTime);
            }
        }

        private void HandleArmSwingJump(bool grounded)
        {
            if (!grounded) return;

            InputDevice leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            InputDevice rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

            leftHand.TryGetFeatureValue(CommonUsages.deviceVelocity, out Vector3 leftVelocity);
            rightHand.TryGetFeatureValue(CommonUsages.deviceVelocity, out Vector3 rightVelocity);

            if (leftVelocity.y < -swingThreshold && rightVelocity.y < -swingThreshold)
            {
                float combinedSpeed = Mathf.Abs(leftVelocity.y) + Mathf.Abs(rightVelocity.y);
                float jumpForce = Mathf.Clamp(combinedSpeed * swingMultiplier, minJumpForce, maxJumpForce);
                ExecuteJump(jumpForce);
            }
        }

        private void HandleButtonPressJump(bool grounded)
        {
            if (!grounded) return;

            bool isPressed = false;
            if (jumpButtonAction != null && jumpButtonAction.action != null)
            {
                isPressed = jumpButtonAction.action.WasPressedThisFrame();
            }
            else
            {
                InputDevice rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
                if (rightHand.TryGetFeatureValue(CommonUsages.primaryButton, out bool primaryPressed) && primaryPressed)
                {
                    isPressed = true;
                }
            }

            if (isPressed)
            {
                ExecuteJump(fixedJumpForce);
            }
        }

        private void HandleStepOverJump(bool grounded)
        {
            if (!grounded) return;

            Vector3 origin = transform.position + Vector3.up * 0.1f;
            Vector3 forward = new Vector3(Camera.main.transform.forward.x, 0, Camera.main.transform.forward.z).normalized;

            if (!Physics.Raycast(origin, forward + Vector3.down, stepOverDistance))
            {
                if (Physics.Raycast(origin, forward, out RaycastHit hit, stepOverDistance * 3f))
                {
                    if (hit.collider.CompareTag("Platform"))
                    {
                        StartCoroutine(StepOverCoroutine(hit.point));
                    }
                }
            }
        }

        private void ExecuteJump(float force)
        {
            verticalVelocity = force;
            OnJumpStarted?.Invoke();
        }

        private IEnumerator StepOverCoroutine(Vector3 targetPos)
        {
            isStepOverJumping = true;
            characterController.enabled = false;
            OnJumpStarted?.Invoke();

            Vector3 startPos = transform.position;
            float duration = 0.6f;
            float elapsed = 0;
            float height = 1f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
                currentPos.y += Mathf.Sin(t * Mathf.PI) * height;
                
                transform.position = currentPos;
                yield return null;
            }

            transform.position = targetPos;
            characterController.enabled = true;
            isStepOverJumping = false;
            OnLanded?.Invoke();
        }
    }
}
