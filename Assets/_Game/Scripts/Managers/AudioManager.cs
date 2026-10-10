using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace OceanGame
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private EventInstance _oceanAmbienceInstance;
        private Bus _masterBus;

        private void Awake() 
        {
            Instance = this;
            _masterBus = RuntimeManager.GetBus("bus:/");
        }

        private void OnDestroy()
        {
            StopOceanAmbience();
        }

        // Play a sound one time at a specific world position
        public void PlayOneShot(EventReference sound, Vector3 worldPos)
        {
            RuntimeManager.PlayOneShot(sound, worldPos);
        }

        public void StartOceanAmbience(EventReference ambience)
        {
            if (_oceanAmbienceInstance.isValid()) return;

            _oceanAmbienceInstance = RuntimeManager.CreateInstance(ambience);
            _oceanAmbienceInstance.start();
        }

        public void StopOceanAmbience()
        {
            if (!_oceanAmbienceInstance.isValid()) return;

            _oceanAmbienceInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            _oceanAmbienceInstance.release();
            _oceanAmbienceInstance = default;
        }

        // connected to pause menu volume slider
        public void SetMasterVolume(float volume) 
        {
            _masterBus.setVolume(volume);
        }
    }
}
