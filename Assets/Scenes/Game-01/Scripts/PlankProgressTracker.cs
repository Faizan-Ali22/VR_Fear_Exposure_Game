using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
/// <summary>
/// Detects the player on the plank via trigger and calculates progress 0→1.
/// PlankStart and PlankEnd are transform markers — drag them in from Hierarchy.
/// The BoxCollider on THIS GameObject must have IsTrigger = ON.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PlankProgressTracker : MonoBehaviour
{
    [Header("Plank Axis Markers")]
    [SerializeField] private Transform _plankStart;
    [SerializeField] private Transform _plankEnd;

    [Header("Detection")]
    [SerializeField] private string _playerTag          = "Player";
    [SerializeField] private float  _completionThreshold = 0.92f;

    // Events consumed by PlankTraversalManager
    public event Action         OnPlayerEnteredPlank;
    public event Action         OnPlayerExitedPlank;
    public event Action<float>  OnProgressUpdated;
    public event Action         OnReachedEnd;

    private Transform _playerTransform;
    private bool      _playerOnPlank;
    private bool      _endFired;
    private Vector3   _plankAxis;
    private float     _plankLength;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Update()
    {
        if (!_playerOnPlank || _playerTransform == null) return;
        float progress = CalculateProgress(_playerTransform.position);
        OnProgressUpdated?.Invoke(progress);
        if (!_endFired && progress >= _completionThreshold)
        {
            _endFired = true;
            OnReachedEnd?.Invoke();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(_playerTag)) return;
        _playerTransform = other.transform;
        _playerOnPlank   = true;
        _endFired        = false;
        BuildPlankAxis();
        OnPlayerEnteredPlank?.Invoke();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(_playerTag)) return;
        _playerOnPlank   = false;
        _playerTransform = null;
        OnPlayerExitedPlank?.Invoke();
    }

    private void BuildPlankAxis()
    {
        if (_plankStart == null || _plankEnd == null)
        {
            Debug.LogWarning("[PlankProgressTracker] PlankStart or PlankEnd not assigned!", this);
            return;
        }
        Vector3 diff = _plankEnd.position - _plankStart.position;
        _plankLength  = diff.magnitude;
        _plankAxis    = _plankLength > 0.001f ? diff / _plankLength : Vector3.forward;
    }

    private float CalculateProgress(Vector3 worldPos)
    {
        if (_plankLength < 0.001f) return 0f;
        Vector3 fromStart = worldPos - _plankStart.position;
        float projected  = Vector3.Dot(fromStart, _plankAxis);
        return Mathf.Clamp01(projected / _plankLength);
    }
}