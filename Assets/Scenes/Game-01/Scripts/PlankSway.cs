using UnityEngine;
using System.Collections;
using System.Collections.Generic;
/// <summary>
/// Applies subtle organic sway to the plank. Intensity follows a bell curve:
/// peaks at plank center (progress 0.5), low at the safe edges.
/// Transform-only — no Rigidbody. Zero GC allocations in Update.
/// </summary>
public class PlankSway : MonoBehaviour
{
    [Header("Sway Tuning")]
    [SerializeField] private float _baseFrequency = 0.35f;
    [SerializeField] private float _baseAmplitude = 0.6f;
    [SerializeField] private float _peakAmplitude = 2.8f;
    [SerializeField] private float _rollAmplitude  = 0.4f;
    [SerializeField] private float _joltStrength   = 1.5f;
    [SerializeField] private float _smoothSpeed    = 2.5f;

    private bool       _active;
    private float      _targetAmplitude;
    private float      _currentAmplitude;
    private float      _time;
    private float      _joltTimer;
    private float      _joltValue;
    private Quaternion _restRotation;

    private void Awake()
    {
        _restRotation    = transform.localRotation;
        _targetAmplitude = _baseAmplitude;
    }

    private void Update()
    {
        if (!_active) return;
        _time += Time.deltaTime;
        _currentAmplitude = Mathf.Lerp(_currentAmplitude, _targetAmplitude,
                                        Time.deltaTime * _smoothSpeed);

        // Primary pitch (X) + secondary roll (Z) at offset frequency = organic feel
        float pitch = Mathf.Sin(_time * _baseFrequency * Mathf.PI * 2f) * _currentAmplitude;
        float roll  = Mathf.Sin(_time * _baseFrequency * Mathf.PI * 1.4f + 0.9f) * _rollAmplitude;

        // Random occasional jolt (no allocation — simple timer)
        _joltTimer -= Time.deltaTime;
        if (_joltTimer <= 0f)
        {
            _joltTimer = Random.Range(5f, 12f);
            _joltValue = Random.Range(-_joltStrength, _joltStrength);
        }
        _joltValue = Mathf.Lerp(_joltValue, 0f, Time.deltaTime * 4f);

        transform.localRotation = _restRotation * Quaternion.Euler(pitch + _joltValue, 0f, roll);
    }

    public void SetActive(bool active)
    {
        _active = active;
        if (!active)
        {
            _targetAmplitude = _currentAmplitude = 0f;
            transform.localRotation = _restRotation;
        }
        else { _targetAmplitude = _baseAmplitude; }
    }

    /// <summary>progress 0-1. Bell curve: max at 0.5.</summary>
    public void UpdateIntensity(float progress)
    {
        float center     = 1f - Mathf.Abs(progress - 0.5f) * 2f;
        _targetAmplitude = Mathf.Lerp(_baseAmplitude, _peakAmplitude, center);
    }
}