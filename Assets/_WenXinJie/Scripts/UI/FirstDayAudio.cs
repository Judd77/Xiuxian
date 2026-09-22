using UnityEngine;

namespace WenXinJie.UI
{
    public sealed class FirstDayAudio : MonoBehaviour
    {
        private const string MusicVolumeKey = "WenXinJie.Settings.MusicVolume";
        private const string AmbienceVolumeKey = "WenXinJie.Settings.AmbienceVolume";

        private AudioSource _music;
        private AudioSource _ambience;
        private bool _isMenu = true;

        public float MusicVolume { get; private set; }
        public float AmbienceVolume { get; private set; }

        public void Initialize(AudioClip music, AudioClip ambience)
        {
            MusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, 0.35f));
            AmbienceVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(AmbienceVolumeKey, 0.28f));
            _music = CreateSource("Music", music);
            _ambience = CreateSource("Rain", ambience);
        }

        public void SetMenu(bool isMenu)
        {
            _isMenu = isMenu;
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

        private void Update()
        {
            if (_music == null || _ambience == null)
            {
                return;
            }

            float step = Time.unscaledDeltaTime * 0.35f;
            _music.volume = Mathf.MoveTowards(_music.volume, MusicVolume, step);
            _ambience.volume = Mathf.MoveTowards(_ambience.volume, _isMenu ? 0f : AmbienceVolume, step);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                PlayerPrefs.Save();
            }
        }

        private void OnDestroy()
        {
            PlayerPrefs.Save();
        }
    }
}
