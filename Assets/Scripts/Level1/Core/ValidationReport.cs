using System.Collections.Generic;

namespace FearExposure.Level1
{
    /// <summary>
    /// Collects setup problems so Level1Director can print ONE clear checklist
    /// (Level1Director > right-click > Validate Setup).
    /// </summary>
    public sealed class ValidationReport
    {
        private readonly List<string> _errors = new List<string>();
        private readonly List<string> _warnings = new List<string>();

        public IReadOnlyList<string> Errors { get { return _errors; } }
        public IReadOnlyList<string> Warnings { get { return _warnings; } }

        public void Require(UnityEngine.Object reference, string fieldName, UnityEngine.Object owner)
        {
            if (reference == null)
                _errors.Add(Describe(owner) + " -> '" + fieldName + "' is not assigned.");
        }

        public void Error(string message, UnityEngine.Object owner)
        {
            _errors.Add(Describe(owner) + " -> " + message);
        }

        public void Warn(string message, UnityEngine.Object owner)
        {
            _warnings.Add(Describe(owner) + " -> " + message);
        }

        private static string Describe(UnityEngine.Object owner)
        {
            return owner == null ? "(unknown)" : owner.name + " [" + owner.GetType().Name + "]";
        }
    }
}
