using UnityEngine;

namespace WenXinJie.UI
{
    public sealed class GameAudio : MonoBehaviour
    {
        private const string MusicVolumeKey = "WenXinJie.Settings.MusicVolume";
        private const string AmbienceVolumeKey = "WenXinJie.Settings.AmbienceVolume";
        private AudioSource _ambience;
        private bool _ambienceActive;

        private AudioSource _music;

        public float MusicVolume { get; private set; }
        public float AmbienceVolume { get; private set; }

        private void Update()
        {
            if (_music == null || _ambience == null)
            {
                return;
            }

            var step = Time.unscaledDeltaTime * 0.35f;
            _music.volume = Mathf.MoveTowards(_music.volume, MusicVolume, step);
            _ambience.volume = Mathf.MoveTowards(_ambience.volume, _ambienceActive ? AmbienceVolume : 0f, step);
        }

        private void OnDestroy()
        {
            PlayerPrefs.Save();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                PlayerPrefs.Save();
            }
        }

        public void Initialize(AudioClip music, AudioClip ambience)
        {
            MusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, 0.35f));
            AmbienceVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(AmbienceVolumeKey, 0.28f));
            _music = CreateSource("Music", music);
            _ambience = CreateSource("Rain", ambience);
        }

        public void SetAmbienceActive(bool active)
        {
            _ambienceActive = active;
        }

        public void SetMusicVolume(float volume)
        {
            MusicVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
        }

        public void SetAmbienceVolume(float volume)
        {
            AmbienceVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(AmbienceVolumeKey, AmbienceVolume);
        }

        private AudioSource CreateSource(string sourceName, AudioClip clip)
        {
            var sourceObject = new GameObject(sourceName);
            sourceObject.transform.SetParent(transform, false);
            var source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.loop = true;
            source.volume = 0f;
            source.clip = clip;

            if (clip != null)
            {
                source.Play();
            }

            return source;
        }
    }
}