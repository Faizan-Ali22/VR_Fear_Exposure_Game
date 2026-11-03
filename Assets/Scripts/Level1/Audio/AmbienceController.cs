using System;
using System.Collections.Generic;
using UnityEngine;

namespace FearExposure.Level1
{
    /// <summary>
    /// Three looping sound beds (room tone, city + birds, soft music) that blend between presets.
    /// A step's "Ambience Preset" field names the preset to blend to when that step starts.
    /// </summary>
    public class AmbienceController : MonoBehaviour
    {
        [Serializable]
        public class Preset
        {
            public string name = "Preset";
            [UnityEngine.Range(0f, 1f)] public float room;
            [UnityEngine.Range(0f, 1f)] public float city;
            [UnityEngine.Range(0f, 1f)] public float music;

            public Preset() { }

            public Preset(string presetName, float roomVolume, float cityVolume, float musicVolume)
            {
                name = presetName;
                room = roomVolume;
                city = cityVolume;
                music = musicVolume;
            }
        }

        [Header("Looping audio sources (Loop ON, Play On Awake OFF)")]
        [SerializeField] private AudioSource roomTone;
        [SerializeField] private AudioSource cityBed;
        [SerializeField] private AudioSource music;

        [SerializeField] private float blendSeconds = 3f;

        [SerializeField]
        private List<Preset> presets = new List<Preset>
        {
            new Preset("Interior", 0.45f, 0.10f, 0.15f),
            new Preset("Elevator", 0.20f, 0.00f, 0.10f),
            new Preset("Rooftop", 0.00f, 0.50f, 0.25f),
            new Preset("Finale", 0.00f, 0.40f, 0.35f)
        };

        private float _targetRoom;
        private float _targetCity;
        private float _targetMusic;

        private void Awake()
        {
            Prepare(roomTone);
            Prepare(cityBed);
            Prepare(music);
        }

        private static void Prepare(AudioSource source)
        {
            if (source == null) return;
            source.loop = true;
            source.playOnAwake = false;
            source.volume = 0f;
        }

        /// <summary>Blend to the preset with this name. Unknown or empty names change nothing.</summary>
        public void Apply(string presetName)
        {
            if (string.IsNullOrEmpty(presetName)) return;
            for (int i = 0; i < presets.Count; i++)
            {
                if (!string.Equals(presets[i].name, presetName, StringComparison.OrdinalIgnoreCase)) continue;
                _targetRoom = presets[i].room;
                _targetCity = presets[i].city;
                _targetMusic = presets[i].music;
                return;
            }
            Debug.LogWarning("[Level1] AmbienceController has no preset named '" + presetName + "'.", this);
        }

        private void Update()
        {
            float step = Time.deltaTime / Mathf.Max(0.1f, blendSeconds);
            Blend(roomTone, _targetRoom, step);
            Blend(cityBed, _targetCity, step);
            Blend(music, _targetMusic, step);
        }

        private static void Blend(AudioSource source, float target, float step)
        {
            if (source == null) return;
            source.volume = Mathf.MoveTowards(source.volume, target, step);
            if (target > 0.001f && !source.isPlaying && source.clip != null) source.Play();
            else if (target <= 0.001f && source.volume <= 0.001f && source.isPlaying) source.Pause();
        }
    }
}
