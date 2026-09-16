using UnityEngine;
using System.Collections;
using System.Collections.Generic;
/// <summary>
/// Plays wood creak sounds. Frequency and volume peak when the
/// player is at the plank center (highest anxiety point).
/// _creakSource → drag PlankCreaks / AudioSource
/// Assign 3-5 creak AudioClip assets to _creakClips array.
/// </summary>
public class PlankAudioManager : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private AudioSource _creakSource;

    [Header("Clips — assign 3-5 short wood creak sounds")]
    [SerializeField] private AudioClip[] _creakClips;

    [Header("Settings")]
    [SerializeField] private float _minInterval = 2.0f;
    [SerializeField] private float _maxInterval = 6.0f;
    [SerializeField] private float _minVolume   = 0.15f;
    [SerializeField] private float _maxVolume   = 0.85f;

    private float     _intensity;
    private bool      _active;
    private Coroutine _creakRoutine;

    public void StartCreaks()
    {
        _active       = true;
        _creakRoutine = StartCoroutine(CreakLoop());
    }

    public void StopCreaks()
    {
        _active = false;
        if (_creakRoutine != null) StopCoroutine(_creakRoutine);
    }

    public void UpdateCreakIntensity(float progress)
    {
        // Bell curve: max intensity at plank center
        _intensity = 1f - Mathf.Abs(progress - 0.5f) * 2f;
    }

    private IEnumerator CreakLoop()
    {
        while (_active)
        {
            float interval = Mathf.Lerp(_maxInterval, _minInterval, _intensity);
            yield return new WaitForSeconds(interval);
            if (!_active || _creakClips == null || _creakClips.Length == 0) continue;
            int   idx    = Random.Range(0, _creakClips.Length);
            float volume = Mathf.Lerp(_minVolume, _maxVolume, _intensity);
            if (_creakSource != null)
            {
                _creakSource.clip   = _creakClips[idx];
                _creakSource.volume = volume;
                _creakSource.pitch  = Random.Range(0.85f, 1.15f);
                _creakSource.Play();
            }
        }
    }
}