using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace EntilandVR.DosCuatro.DAM_AJEI.G_Uno
{
    public class AudioManager : MonoBehaviour
    {
        public enum BGM_Songs {WESTERN, FAIR, LAST}

        public enum SFX_Sounds
        {
            SHOT,
            RELOAD,
            NO_BULLETS,
            WOOD_IMPACT,
            EXPLOSION,
            NEXT_LEVEL,
            WIN,
            LOSE,
            BANDIT_RED,
            BANDIT_BLUE,
            BANDIT_GREEN,
            BANDIT_SKULL,
            LAST
        }

        public static AudioManager Instance { get; private set; }

        [Header("Clips")]
        public List<AudioClip> bgmClipList;
        public List<AudioClip> sfxClipList;

        [Header("Mixers")]
        public AudioMixerGroup bgmMixer;
        public AudioMixerGroup sfxMixer;

        [Header("SFX Settings")]
        public int maxSFX = 4;
        public float pitchMin = -0.15f;
        public float pitchMax = 0.15f;

        private AudioSource bgmSource;
        private List<AudioSource> sfxSources = new List<AudioSource>();
        private int currentSFX;
        private BGM_Songs currentBGM = BGM_Songs.LAST;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.outputAudioMixerGroup = bgmMixer;

            for (int i = 0; i < maxSFX; i++)
            {
                AudioSource sfx = gameObject.AddComponent<AudioSource>();
                sfx.outputAudioMixerGroup = sfxMixer;
                sfxSources.Add(sfx);
            }
        }

        private void Update()
        {
            HandleSceneMusic();
        }

        private void HandleSceneMusic()
        {
            string scene = SceneManager.GetActiveScene().name;

            if (scene.Contains("G_1"))
            {
                PlayBGM(BGM_Songs.WESTERN);
            }
            else
            {
                StopBGM();
            }
        }

        public void PlayBGM(BGM_Songs song)
        {
            if (song == currentBGM || song == BGM_Songs.LAST)
            {
                return;
            }

            currentBGM = song;
            bgmSource.clip = bgmClipList[(int)song];
            bgmSource.Play();
        }

        public void StopBGM()
        {
            bgmSource.Stop();
            currentBGM = BGM_Songs.LAST;
        }

        public void PlaySFX(SFX_Sounds sound)
        {
            int index = (int)sound;
            if (index < 0 || index >= sfxClipList.Count)
            {
                return;
            }

            currentSFX = (currentSFX + 1) % maxSFX;
            AudioSource source = sfxSources[currentSFX];

            source.clip = sfxClipList[index];
            source.pitch = 1f + Random.Range(pitchMin, pitchMax);
            source.Play();
        }

        public void SetBGMVolume(float value)
        {
            float safeValue = Mathf.Clamp(value, 0.0001f, 1f);
            bgmMixer.audioMixer.SetFloat("BGM", Mathf.Log10(safeValue) * 10f);
        }

        public void SetSFXVolume(float value)
        {
            float safeValue = Mathf.Clamp(value, 0.0001f, 1f);
            sfxMixer.audioMixer.SetFloat("SFX", Mathf.Log10(safeValue) * 10f);
        }
    }
}