using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SkylineMission.Mission.Tasks
{
    public class DroneRescue : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Ensure droneObject has an XRGrabInteractable component")]
        [SerializeField] private GameObject droneObject;
        [SerializeField] private Transform droneLedge;
        [SerializeField] private Transform deliveryZone;
        [SerializeField] private float deliveryRadius = 1.5f;
        [SerializeField] private float droneHoverHeight = 0.1f;
        [SerializeField] private float droneHoverSpeed = 2f;
        
        [Header("Rotors")]
        [SerializeField] private Transform[] rotorTransforms;
        [SerializeField] private float rotorSpeed = 720f;

        private bool isActive;
        public bool IsActive 
        { 
            get => isActive; 
            set 
            {
                isActive = value;
                if (!isActive)
                {
                    ResetDrone();
                }
            }
        }

        public bool IsComplete { get; private set; }

        public event System.Action OnTaskCompleted;

        private XRGrabInteractable grabInteractable;
        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private float hoverTimer = 0f;

        private void Awake()
        {
            if (droneObject == null) Debug.LogError("DroneRescue: DroneObject is null!");
            if (droneLedge == null) Debug.LogError("DroneRescue: DroneLedge is null!");
            if (deliveryZone == null) Debug.LogError("DroneRescue: DeliveryZone is null!");

            if (droneObject != null)
            {
                originalPosition = droneObject.transform.position;
                originalRotation = droneObject.transform.rotation;
                
                grabInteractable = droneObject.GetComponent<XRGrabInteractable>();
                if (grabInteractable == null)
                {
                    Debug.LogError("DroneRescue: DroneObject is missing an XRGrabInteractable component!");
                }
            }
        }

        private void Update()
        {
            if (!IsActive || IsComplete || droneObject == null || grabInteractable == null) return;

            bool isGrabbed = grabInteractable.isSelected;

            AnimateRotors(isGrabbed);

            if (!isGrabbed)
            {
                AnimateHover();
                CheckDelivery();
            }
        }

        private void AnimateRotors(bool isGrabbed)
        {
            float speedMultiplier = isGrabbed ? 0.3f : 1f; // Slow down if malfunctioning
            float rotationAmount = rotorSpeed * speedMultiplier * Time.deltaTime;

            if (rotorTransforms != null)
            {
                foreach (var rotor in rotorTransforms)
                {
                    if (rotor != null)
                    {
                        rotor.Rotate(0, rotationAmount, 0, Space.Self);
                    }
                }
            }
        }

        private void AnimateHover()
        {
            hoverTimer += Time.deltaTime * droneHoverSpeed;
            float newY = originalPosition.y + Mathf.Sin(hoverTimer) * droneHoverHeight;
            droneObject.transform.position = new Vector3(droneObject.transform.position.x, newY, droneObject.transform.position.z);
        }

        private void CheckDelivery()
        {
            float dist = Vector3.Distance(droneObject.transform.position, deliveryZone.position);
            if (dist <= deliveryRadius)
            {
                IsComplete = true;
                OnTaskCompleted?.Invoke();
            }
        }

        private void ResetDrone()
        {
            if (droneObject != null)
            {
                droneObject.transform.position = originalPosition;
                droneObject.transform.rotation = originalRotation;
            }
            hoverTimer = 0f;
        }
    }
}
