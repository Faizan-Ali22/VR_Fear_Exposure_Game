using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Controls randomised wind gusts that:
///   1. Fade in wind ambience audio.
///   2. Apply small horizontal push via CharacterController.Move().
///   3. Send haptic pulses to both controllers on each gust.
///
/// _windAudioSource    → drag WindAmbience / AudioSource
/// _characterController → drag XR Origin (XR Rig) / Character Controller
/// </summary>
public class WindController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioSource         _windAudioSource;
    [SerializeField] private CharacterController _characterController;

    [Header("Gust Timing")]
    [SerializeField] private float _minGustInterval = 5f;
    [SerializeField] private float _maxGustInterval = 11f;
    [SerializeField] private float _gustDuration    = 1.8f;

    [Header("Gust Strength")]
    [SerializeField] private float _gustForce = 0.25f;

    [Header("Audio")]
    [SerializeField] private float _windAmbienceTarget = 0.55f;
    [SerializeField] private float _fadeInDuration    = 1.5f;
    [SerializeField] private float _fadeOutDuration   = 1.0f;

    [Header("Haptics")]
    [SerializeField] private float _hapticAmplitude = 0.35f;
    [SerializeField] private float _hapticDuration  = 0.18f;

    private bool     _active;
    private Vector3 _windForce;
    private Coroutine _gustLoop;

    // Pre-allocated — zero GC in Update (Quest requirement)
    private readonly List<InputDevice> _leftDevices  = new List<InputDevice>(2);
    private readonly List<InputDevice> _rightDevices = new List<InputDevice>(2);

    private void Update()
    {
        if (!_active || _characterController == null) return;
        if (_windForce.sqrMagnitude > 0.0001f)
            _characterController.Move(_windForce * Time.deltaTime);
    }

    public void StartWind()
    {
        _active = true;
        if (_windAudioSource != null)
        {
            _windAudioSource.volume = 0f;
            _windAudioSource.Play();
            StartCoroutine(FadeAudio(_windAudioSource, _windAmbienceTarget, _fadeInDuration));
        }
        _gustLoop = StartCoroutine(GustLoop());
    }

    public void StopWind()
    {
        _active    = false;
        _windForce = Vector3.zero;
        if (_gustLoop != null) StopCoroutine(_gustLoop);
        if (_windAudioSource != null)
            StartCoroutine(FadeAudio(_windAudioSource, 0f, _fadeOutDuration));
    }

    private IEnumerator GustLoop()
    {
        while (_active)
        {
            yield return new WaitForSeconds(Random.Range(_minGustInterval, _maxGustInterval));
            if (_active) yield return StartCoroutine(ApplyGust());
        }
    }

    private IEnumerator ApplyGust()
    {
        float   angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector3 dir   = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        PulseHaptics(_hapticAmplitude, _hapticDuration);
        float elapsed = 0f;
        while (elapsed < _gustDuration)
        {
            float t = elapsed / _gustDuration;
            _windForce = dir * (Mathf.Sin(t * Mathf.PI) * _gustForce);
            elapsed += Time.deltaTime;
            yield return null;
        }
        _windForce = Vector3.zero;
    }

    private IEnumerator FadeAudio(AudioSource src, float target, float duration)
    {
        float start = src.volume, elapsed = 0f;
        while (elapsed < duration)
        {
            src.volume = Mathf.Lerp(start, target, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        src.volume = target;
        if (target <= 0f) src.Stop();
    }

    private void PulseHaptics(float amplitude, float duration)
    {
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Left,
            _leftDevices);
        foreach (var d in _leftDevices)  d.SendHapticImpulse(0, amplitude, duration);

        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right,
            _rightDevices);
        foreach (var d in _rightDevices) d.SendHapticImpulse(0, amplitude, duration);
    }
}