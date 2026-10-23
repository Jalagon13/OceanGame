using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace SketchLand
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private EventInstance _surfaceAmbienceInstance;
        private Bus _masterBus;

        private void Awake() 
        {
            Instance = this;
            _masterBus = RuntimeManager.GetBus("bus:/");
        }

        private void OnDestroy()
        {
            StopSurfaceAmbience();
        }

        // Play a sound one time at a specific world position
        public void PlayOneShot(EventReference sound, Vector3 worldPos)
        {
            RuntimeManager.PlayOneShot(sound, worldPos);
        }

        public void StartSurfaceAmbience(EventReference ambience)
        {
            if (_surfaceAmbienceInstance.isValid()) return;

            _surfaceAmbienceInstance = RuntimeManager.CreateInstance(ambience);
            _surfaceAmbienceInstance.start();
        }

        public void StopSurfaceAmbience()
        {
            if (!_surfaceAmbienceInstance.isValid()) return;

            _surfaceAmbienceInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            _surfaceAmbienceInstance.release();
            _surfaceAmbienceInstance = default;
        }

        // connected to pause menu volume slider
        public void SetMasterVolume(float volume) 
        {
            _masterBus.setVolume(volume);
        }
    }
}
