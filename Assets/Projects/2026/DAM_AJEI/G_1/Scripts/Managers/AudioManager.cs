namespace EntilandVR.DosCuatro.DAM_AJEI.G_Uno
{
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.Audio;
	using UnityEngine.SceneManagement;

	public class AudioManager : MonoBehaviour
	{
		public enum BGM_Songs { MENU, LEVEL, WIN, LAST }
		public enum SFX_Sounds
		{
			JUMP, FOOTSTEPS, DIVISION, UNION, // Player
			CHECKPOINT, DEAD, // Live
			LEVER, TRAPDOOR, PILLOW, COLECT_MAGIC_ESSENCE, // Mechanics
			LIGHTS, RETURN_BUTTON_CLICK, BUTTON_CLICK, // Extras
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
		public int maxSFX = 5;
		public float pitchMin = -0.15f;
		public float pitchMax = 0.15f;

		private AudioSource bgmSource;
		private List<AudioSource> sfxSources = new();
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

		// ---------------- BGM ----------------

		void HandleSceneMusic()
		{
			string scene = SceneManager.GetActiveScene().name;

			if (scene.Contains("Menu"))
				PlayBGM(BGM_Songs.MENU);
			else if (scene.Contains("LVL"))
				PlayBGM(BGM_Songs.LEVEL);
			else
				StopBGM();
		}

		public void PlayBGM(BGM_Songs song)
		{
			if (song == currentBGM || song == BGM_Songs.LAST) return;

			currentBGM = song;
			bgmSource.clip = bgmClipList[(int)song];
			bgmSource.Play();
		}

		public void StopBGM()
		{
			bgmSource.Stop();
			currentBGM = BGM_Songs.LAST;
		}

		// ---------------- SFX ----------------

		public void PlaySFX(SFX_Sounds sound)
		{
			int index = (int)sound;
			if (index < 0 || index >= sfxClipList.Count) return;

			currentSFX = (currentSFX + 1) % maxSFX;
			AudioSource source = sfxSources[currentSFX];

			source.clip = sfxClipList[index];
			source.pitch = 1f + Random.Range(pitchMin, pitchMax);
			source.Play();
		}

		// ---------------- Volume ----------------

		public void SetBGMVolume(float value)
		{
			bgmMixer.audioMixer.SetFloat("BGM", Mathf.Log10(value) * 10);
		}

		public void SetSFXVolume(float value)
		{
			sfxMixer.audioMixer.SetFloat("SFX", Mathf.Log10(value) * 10);
		}
	}
}