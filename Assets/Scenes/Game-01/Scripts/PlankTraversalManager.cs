using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
/// <summary>
/// Central state machine for Task 1: Traverse the Narrow Wooden Plank.
/// Coordinates PlankSway, WindController, PlankAudioManager, HeightAnxietyFeedback.
/// Wire all serialized references and UnityEvents in the Inspector.
/// </summary>
public class PlankTraversalManager : MonoBehaviour
{
    // ── Inspector References ──────────────────────────────────────────────────
    [Header("Sub-Systems — drag from Hierarchy")]
    [SerializeField] private PlankProgressTracker  _progressTracker;
    [SerializeField] private PlankSway             _plankSway;
    [SerializeField] private WindController        _windController;
    [SerializeField] private PlankAudioManager     _audioManager;
    [SerializeField] private HeightAnxietyFeedback _anxietyFeedback;

    [Header("Task Settings")]
    [Tooltip("0 = no time limit")]
    [SerializeField] private float _taskTimeLimit = 0f;
    [SerializeField] private bool  _allowRetry    = true;

    // ── Events — wire in Inspector or subscribe via code ──────────────────────
    [Header("Events")]
    public UnityEvent        OnTaskActivated;
    public UnityEvent        OnTaskCompleted;
    public UnityEvent        OnPlayerFell;
    public UnityEvent<float> OnProgressChanged;

    // ── State ─────────────────────────────────────────────────────────────────
    private enum State { Inactive, Active, Completed, Failed }
    private State _state = State.Inactive;
    private float _elapsed;

    public bool  IsComplete => _state == State.Completed;
    public float Progress   { get; private set; }

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void OnEnable()
    {
        if (_progressTracker == null) return;
        _progressTracker.OnPlayerEnteredPlank += HandleEntered;
        _progressTracker.OnPlayerExitedPlank  += HandleExited;
        _progressTracker.OnProgressUpdated    += HandleProgress;
        _progressTracker.OnReachedEnd         += HandleCompleted;
    }

    private void OnDisable()
    {
        if (_progressTracker == null) return;
        _progressTracker.OnPlayerEnteredPlank -= HandleEntered;
        _progressTracker.OnPlayerExitedPlank  -= HandleExited;
        _progressTracker.OnProgressUpdated    -= HandleProgress;
        _progressTracker.OnReachedEnd         -= HandleCompleted;
    }

    private void Update()
    {
        if (_state != State.Active || _taskTimeLimit <= 0f) return;
        _elapsed += Time.deltaTime;
        if (_elapsed >= _taskTimeLimit) HandleExited();
    }

    // ── Public API — called by PlankZoneTrigger ───────────────────────────────
    public void NotifyEndZoneReached()
    {
        if (_state == State.Active) HandleCompleted();
    }

    // ── Handlers ──────────────────────────────────────────────────────────────
    private void HandleEntered()
    {
        if (_state == State.Completed || _state == State.Failed) return;
        _state   = State.Active;
        _elapsed = 0f;
        _plankSway?.SetActive(true);
        _windController?.StartWind();
        _audioManager?.StartCreaks();
        _anxietyFeedback?.SetActive(true);
        OnTaskActivated?.Invoke();
    }

    private void HandleExited()
    {
        if (_state != State.Active) return;
        _state = _allowRetry ? State.Inactive : State.Failed;
        _plankSway?.SetActive(false);
        _windController?.StopWind();
        _audioManager?.StopCreaks();
        _anxietyFeedback?.SetActive(false);
        Progress = 0f;
        OnPlayerFell?.Invoke();
    }

    private void HandleProgress(float p)
    {
        Progress = p;
        _plankSway?.UpdateIntensity(p);
        _audioManager?.UpdateCreakIntensity(p);
        _anxietyFeedback?.UpdateIntensity(p);
        OnProgressChanged?.Invoke(p);
    }

    private void HandleCompleted()
    {
        if (_state != State.Active) return;
        _state = State.Completed;
        _plankSway?.SetActive(false);
        _windController?.StopWind();
        _audioManager?.StopCreaks();
        _anxietyFeedback?.SetActive(false);
        OnTaskCompleted?.Invoke();
    }
}