using System.Collections;
using UnityEngine;
using UnityEngine.XR;

namespace FearExposure.Level1
{
    /// <summary>
    /// Safety valve. Hold BOTH grip buttons for 1.5 s (or hold H in the editor) and the player
    /// is faded out and moved to the nearest safe spot (a rest area), with a calming message.
    /// Ignored while the elevator is moving.
    /// </summary>
    public class ComfortReturn : MonoBehaviour
    {
        [Tooltip("Empty objects standing on the floor where the player should reappear (e.g. beside the benches).")]
        [SerializeField] private Transform[] safeSpots;
        [SerializeField] private FadeOverlay fade;
        [SerializeField] private float holdSeconds = 1.5f;
        [SerializeField] private float cooldownSeconds = 6f;
        [SerializeField, TextArea] private string returnMessage = "You are back in a safe place. Take a slow breath. Continue whenever you feel ready.";

        private UnityEngine.XR.InputDevice _left;
        private UnityEngine.XR.InputDevice _right;
        private float _nextDeviceRefresh;
        private float _held;
        private float _nextAllowed;
        private bool _busy;

        private void Update()
        {
            if (_busy || Time.time < _nextAllowed) return;
            PlayerRig rig = PlayerRig.Instance;
            if (rig == null || rig.Head == null || rig.InputLocked) { _held = 0f; return; }

            if (IsHoldInput()) _held += Time.deltaTime;
            else _held = 0f;

            if (_held >= holdSeconds)
            {
                _held = 0f;
                StartCoroutine(ReturnRoutine(rig));
            }
        }

        private bool IsHoldInput()
        {
#if ENABLE_INPUT_SYSTEM
            UnityEngine.InputSystem.Keyboard keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.hKey.isPressed) return true;
#endif
            if (Time.time >= _nextDeviceRefresh)
            {
                _nextDeviceRefresh = Time.time + 1f;
                _left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
                _right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            }
            bool l, r;
            if (!_left.TryGetFeatureValue(CommonUsages.gripButton, out l)) return false;
            if (!_right.TryGetFeatureValue(CommonUsages.gripButton, out r)) return false;
            return l && r;
        }

        private IEnumerator ReturnRoutine(PlayerRig rig)
        {
            _busy = true;
            Transform spot = NearestSpot(rig.FeetPosition);
            if (spot != null)
            {
                if (fade != null) yield return fade.StartFade(1f, 0.4f);
                rig.PlaceFeetAt(spot.position, spot.eulerAngles.y);
                Level1Director director = Level1Director.Current;
                if (director != null && director.Instructions != null) director.Instructions.Show(returnMessage, 8f);
                yield return new WaitForSeconds(0.4f);
                if (fade != null) yield return fade.StartFade(0f, 0.8f);
            }
            _nextAllowed = Time.time + cooldownSeconds;
            _busy = false;
        }

        private Transform NearestSpot(Vector3 from)
        {
            Transform best = null;
            float bestSqr = float.MaxValue;
            if (safeSpots == null) return null;
            for (int i = 0; i < safeSpots.Length; i++)
            {
                if (safeSpots[i] == null) continue;
                Vector3 d = safeSpots[i].position - from;
                d.y = 0f;
                float sqr = d.sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; best = safeSpots[i]; }
            }
            return best;
        }

        private void OnDrawGizmosSelected()
        {
            if (safeSpots == null) return;
            Gizmos.color = Color.cyan;
            for (int i = 0; i < safeSpots.Length; i++)
                if (safeSpots[i] != null) Gizmos.DrawWireSphere(safeSpots[i].position + Vector3.up * 0.1f, 0.3f);
        }
    }
}
