using System;
using UnityEngine;

namespace FearExposure.Level1
{
    /// <summary>
    /// Base class for every step of the Level 1 journey.
    ///
    /// ADD A STEP:    create a class that inherits LevelStep, implement OnBegin(), call Finish()
    ///                when the goal is reached, then drag it into Level1Director > Steps.
    /// REMOVE A STEP: take it out of that list. Nothing else depends on it.
    ///
    /// Each step lives on its own GameObject under [Level1]. The Director switches that object
    /// ON when the step starts and OFF when it finishes, so zones, markers and panels that are
    /// children of the step only exist while the step is running.
    /// </summary>
    public abstract class LevelStep : MonoBehaviour
    {
        [Header("Step Info")]
        [SerializeField] private string stepTitle = "Step";
        [Tooltip("Name of an AmbienceController preset to blend to when this step starts (optional).")]
        [SerializeField] private string ambiencePreset = "";

        public string StepTitle { get { return stepTitle; } }
        public string AmbiencePreset { get { return ambiencePreset; } }
        public bool IsRunning { get; private set; }
        public bool IsComplete { get; private set; }

        /// <summary>Raised once, when the step calls Finish().</summary>
        public event Action<LevelStep> Finished;

        protected Level1Director Director { get; private set; }

        protected InstructionPanel Instructions
        {
            get { return Director != null ? Director.Instructions : null; }
        }

        protected CourageScore Courage
        {
            get { return Director != null ? Director.Courage : null; }
        }

        /// <summary>Called by Level1Director. Do not call from other scripts.</summary>
        public void Begin(Level1Director director)
        {
            if (IsRunning) return;
            Director = director;
            IsRunning = true;
            IsComplete = false;
            OnBegin();
        }

        /// <summary>Dev tool (F6): puts the world into this step's finished state and ends it.</summary>
        public void Skip()
        {
            if (!IsRunning) return;
            OnSkip();
            Finish();
        }

        /// <summary>Call this when the step's goal has been reached.</summary>
        protected void Finish()
        {
            if (!IsRunning) return;
            IsRunning = false;
            IsComplete = true;
            OnFinished();
            StopAllCoroutines();
            if (Finished != null) Finished(this);
        }

        protected abstract void OnBegin();
        protected virtual void OnSkip() { }
        protected virtual void OnFinished() { }

        /// <summary>Report missing references (used by Level1Director > Validate Setup).</summary>
        public virtual void Validate(ValidationReport report) { }

        protected void Say(string message, float seconds = 0f)
        {
            if (Instructions != null) Instructions.Show(message, seconds);
        }

        protected void Award(int points, string reason)
        {
            if (Courage != null && points > 0) Courage.Award(points, reason);
        }
    }
}
