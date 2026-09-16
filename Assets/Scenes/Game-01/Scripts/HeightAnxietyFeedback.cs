using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Drives URP post-processing to simulate height anxiety.
/// Vignette darkens + chromatic aberration increases at plank center.
/// Both effects taper back toward the safe edges.
///
/// _anxietyVolume → drag AnxietyVolume GameObject
/// _progressLabel → drag PlankFeedbackCanvas / ProgressLabel (optional)
/// </summary>
public class HeightAnxietyFeedback : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Volume _anxietyVolume;
    [SerializeField] private TMPro.TextMeshProUGUI _progressLabel;

    [Header("Effect Peaks (at plank center)")]
    [SerializeField] private float _peakVignette   = 0.42f;
    [SerializeField] private float _peakAberration = 0.28f;
    [SerializeField] private float _lerpSpeed      = 1.8f;

    private Vignette            _vignette;
    private ChromaticAberration _chromatic;
    private bool  _active;
    private float _vigTarget;
    private float _chromTarget;

    private void Awake()
    {
        if (_anxietyVolume == null) { Debug.LogWarning("[HeightAnxietyFeedback] No Volume assigned."); return; }
        _anxietyVolume.profile.TryGet(out _vignette);
        _anxietyVolume.profile.TryGet(out _chromatic);
    }

    private void Update()
    {
        if (!_active) return;
        if (_vignette != null)
            _vignette.intensity.Override(Mathf.Lerp(
                _vignette.intensity.value, _vigTarget, Time.deltaTime * _lerpSpeed));
        if (_chromatic != null)
            _chromatic.intensity.Override(Mathf.Lerp(
                _chromatic.intensity.value, _chromTarget, Time.deltaTime * _lerpSpeed));
    }

    public void SetActive(bool active)
    {
        _active = active;
        if (!active) { _vigTarget = 0f; _chromTarget = 0f; ForceReset(); }
    }

    public void UpdateIntensity(float progress)
    {
        float center  = 1f - Mathf.Abs(progress - 0.5f) * 2f;
        _vigTarget    = _peakVignette   * center;
        _chromTarget  = _peakAberration * center;
        if (_progressLabel != null)
            _progressLabel.SetText($"{Mathf.RoundToInt(progress * 100f)}%");
    }

    private void ForceReset()
    {
        _vignette?.intensity.Override(0f);
        _chromatic?.intensity.Override(0f);
    }
}