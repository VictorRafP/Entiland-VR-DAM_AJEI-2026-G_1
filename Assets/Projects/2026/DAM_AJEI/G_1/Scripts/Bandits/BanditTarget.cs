using System.Collections.Generic;
using UnityEngine;

namespace EntilandVR.DosCuatro.DAM_AJEI.G_Uno
{
    /// <summary>
    /// Controla un bandido: aparecer, esperar, disparar al jugador y ocultarse.
    /// </summary>
    public class BanditTarget : MonoBehaviour
    {
        private enum BanditState
        {
            Hidden,
            Rising,
            Visible,
            Falling
        }

        public enum VariantSelectionMode
        {
            Fixed,
            Random,
            Sequential
        }

        public enum BanditVoiceType
        {
            Red,
            Blue,
            Green,
            Skull
        }

        [Header("Bandit Variants")]
        [SerializeField] private List<GameObject> banditVariants = new List<GameObject>();
        [SerializeField] private VariantSelectionMode selectionMode = VariantSelectionMode.Sequential;
        [SerializeField] private int defaultVariantIndex = 0;

        [Header("Bandit Audio Type")]
        [SerializeField] private BanditVoiceType banditVoiceType = BanditVoiceType.Red;

        [Header("Score")]
        [SerializeField] private int scoreOnHit = 100;

        [Header("Standing Rotation")]
        [SerializeField] private float standingLocalY = -90f;
        [SerializeField] private float standingLocalZ = 0f;

        [Header("Hidden Rotation")]
        [SerializeField] private float hiddenLocalY = -90f;
        [SerializeField] private float hiddenLocalZ = 90f;

        [Header("Animation Speeds")]
        [SerializeField] private float riseSpeedDegreesPerSecond = 240f;
        [SerializeField] private float fallSpeedDegreesPerSecond = 300f;

        [Header("Behaviour")]
        [SerializeField] private bool startHidden = true;
        [SerializeField] private bool riseOnStart = true;
        [SerializeField] private float startRiseDelay = 0f;
        [SerializeField] private bool respawnAfterFall = true;
        [SerializeField] private float respawnDelay = 1.5f;

        [Header("Bandit Shooting")]
        [SerializeField] private bool shootPlayerIfStillVisible = true;
        [SerializeField] private float timeBeforePlayerShot = 3f;
        [SerializeField] private int shootDamageToPlayer = 1;
        [SerializeField] private Transform shotOrigin;
        [SerializeField] private Transform playerTarget;
        [SerializeField] private PlayerHitReceiver playerHitReceiver;
        [SerializeField] private LineTracerPulse shotTracer;

        [Header("Particles")]
        [SerializeField] private ParticleSystem appearParticles;
        [SerializeField] private ParticleSystem shootParticles;

        private BanditState currentState = BanditState.Hidden;
        private float hiddenTimer = 0f;
        private float visibleShotTimer = 0f;
        private float cachedLocalX = 0f;
        private int nextSequentialIndex = 0;
        private bool shouldRespawn = false;

        public bool IsVisible
        {
            get { return currentState == BanditState.Visible; }
        }

        private void Awake()
        {
            cachedLocalX = transform.localEulerAngles.x;
            DisableAllVariants();

            nextSequentialIndex = Mathf.Clamp(defaultVariantIndex, 0, Mathf.Max(0, banditVariants.Count - 1));

            if (banditVariants.Count > 0)
            {
                ActivateVariant(GetVariantIndexForShow());
            }

            if (startHidden)
            {
                currentState = BanditState.Hidden;
                SetLocalRotation(hiddenLocalY, hiddenLocalZ);

                if (riseOnStart)
                {
                    shouldRespawn = true;
                    hiddenTimer = Mathf.Max(0f, startRiseDelay);
                }
            }
            else
            {
                currentState = BanditState.Visible;
                SetLocalRotation(standingLocalY, standingLocalZ);
                visibleShotTimer = Mathf.Max(0f, timeBeforePlayerShot);
            }
        }

        private void Update()
        {
            if (ShootingGalleryGameManager.Instance != null &&
                !ShootingGalleryGameManager.Instance.IsGameplayRunning)
            {
                return;
            }

            switch (currentState)
            {
                case BanditState.Hidden:
                    UpdateHidden();
                    break;

                case BanditState.Rising:
                    UpdateRising();
                    break;

                case BanditState.Visible:
                    UpdateVisible();
                    break;

                case BanditState.Falling:
                    UpdateFalling();
                    break;
            }
        }

        public void ShowBandit()
        {
            if (currentState == BanditState.Visible || currentState == BanditState.Rising)
            {
                return;
            }

            if (banditVariants.Count > 0)
            {
                ActivateVariant(GetVariantIndexForShow());
            }

            shouldRespawn = false;
            currentState = BanditState.Rising;
        }

        public void HideBandit()
        {
            if (currentState == BanditState.Hidden || currentState == BanditState.Falling)
            {
                return;
            }

            currentState = BanditState.Falling;
        }

        public void HitBandit()
        {
            if (!IsVisible)
            {
                return;
            }

            if (ShootingGalleryGameManager.Instance != null)
            {
                ShootingGalleryGameManager.Instance.RegisterBanditHit(scoreOnHit);
            }

            PlayImpactSound();

            currentState = BanditState.Falling;
            shouldRespawn = respawnAfterFall;
        }

        public void SetRespawnDelay(float newRespawnDelay)
        {
            respawnDelay = Mathf.Max(0f, newRespawnDelay);
        }

        public void SetTimeBeforePlayerShot(float newShotDelay)
        {
            timeBeforePlayerShot = Mathf.Max(0.1f, newShotDelay);
        }

        private void UpdateHidden()
        {
            if (!shouldRespawn)
            {
                return;
            }

            hiddenTimer -= Time.deltaTime;
            if (hiddenTimer <= 0f)
            {
                ShowBandit();
            }
        }

        private void UpdateRising()
        {
            float nextY = Mathf.MoveTowardsAngle(
                transform.localEulerAngles.y,
                standingLocalY,
                riseSpeedDegreesPerSecond * Time.deltaTime);

            float nextZ = Mathf.MoveTowardsAngle(
                transform.localEulerAngles.z,
                standingLocalZ,
                riseSpeedDegreesPerSecond * Time.deltaTime);

            SetLocalRotation(nextY, nextZ);

            bool reachedY = Mathf.Abs(Mathf.DeltaAngle(nextY, standingLocalY)) <= 0.05f;
            bool reachedZ = Mathf.Abs(Mathf.DeltaAngle(nextZ, standingLocalZ)) <= 0.05f;

            if (!reachedY || !reachedZ)
            {
                return;
            }

            SetLocalRotation(standingLocalY, standingLocalZ);
            currentState = BanditState.Visible;
            visibleShotTimer = Mathf.Max(0f, timeBeforePlayerShot);

            if (appearParticles != null)
            {
                appearParticles.Play();
            }

            PlayBanditVoice();
        }

        private void UpdateVisible()
        {
            if (!shootPlayerIfStillVisible)
            {
                return;
            }

            visibleShotTimer -= Time.deltaTime;
            if (visibleShotTimer <= 0f)
            {
                ShootPlayer();
            }
        }

        private void UpdateFalling()
        {
            float nextY = Mathf.MoveTowardsAngle(
                transform.localEulerAngles.y,
                hiddenLocalY,
                fallSpeedDegreesPerSecond * Time.deltaTime);

            float nextZ = Mathf.MoveTowardsAngle(
                transform.localEulerAngles.z,
                hiddenLocalZ,
                fallSpeedDegreesPerSecond * Time.deltaTime);

            SetLocalRotation(nextY, nextZ);

            bool reachedY = Mathf.Abs(Mathf.DeltaAngle(nextY, hiddenLocalY)) <= 0.05f;
            bool reachedZ = Mathf.Abs(Mathf.DeltaAngle(nextZ, hiddenLocalZ)) <= 0.05f;

            if (!reachedY || !reachedZ)
            {
                return;
            }

            SetLocalRotation(hiddenLocalY, hiddenLocalZ);
            currentState = BanditState.Hidden;

            if (shouldRespawn)
            {
                hiddenTimer = Mathf.Max(0f, respawnDelay);
            }
            else
            {
                hiddenTimer = 0f;
            }
        }

        private void ShootPlayer()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.SFX_Sounds.SHOT);
            }

            if (shootParticles != null)
            {
                shootParticles.Play();
            }

            Vector3 startPosition = shotOrigin != null ? shotOrigin.position : transform.position;
            Vector3 endPosition = playerTarget != null ? playerTarget.position : startPosition + transform.forward * 2f;

            if (shotTracer != null)
            {
                shotTracer.ShowTracer(startPosition, endPosition);
            }

            if (playerHitReceiver != null)
            {
                playerHitReceiver.ReceiveBanditHit(shootDamageToPlayer);
            }
            else if (ShootingGalleryGameManager.Instance != null)
            {
                ShootingGalleryGameManager.Instance.DamagePlayer(shootDamageToPlayer);
            }

            currentState = BanditState.Falling;
            shouldRespawn = respawnAfterFall;
        }

        private int GetVariantIndexForShow()
        {
            if (banditVariants == null || banditVariants.Count == 0)
            {
                return -1;
            }

            if (selectionMode == VariantSelectionMode.Fixed)
            {
                return Mathf.Clamp(defaultVariantIndex, 0, banditVariants.Count - 1);
            }

            if (selectionMode == VariantSelectionMode.Random)
            {
                return Random.Range(0, banditVariants.Count);
            }

            int index = nextSequentialIndex;
            if (index < 0 || index >= banditVariants.Count)
            {
                index = 0;
            }

            nextSequentialIndex = index + 1;
            if (nextSequentialIndex >= banditVariants.Count)
            {
                nextSequentialIndex = 0;
            }

            return index;
        }

        private void ActivateVariant(int variantIndex)
        {
            DisableAllVariants();

            if (variantIndex < 0 || variantIndex >= banditVariants.Count)
            {
                return;
            }

            GameObject selected = banditVariants[variantIndex];
            if (selected != null)
            {
                selected.SetActive(true);
            }
        }

        private void DisableAllVariants()
        {
            if (banditVariants == null)
            {
                return;
            }

            for (int i = 0; i < banditVariants.Count; i++)
            {
                if (banditVariants[i] != null)
                {
                    banditVariants[i].SetActive(false);
                }
            }
        }

        private void PlayImpactSound()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.SFX_Sounds.WOOD_IMPACT);
            }
        }

        private void PlayBanditVoice()
        {
            if (AudioManager.Instance == null)
            {
                return;
            }

            switch (banditVoiceType)
            {
                case BanditVoiceType.Red:
                    AudioManager.Instance.PlaySFX(AudioManager.SFX_Sounds.BANDIT_RED);
                    break;

                case BanditVoiceType.Blue:
                    AudioManager.Instance.PlaySFX(AudioManager.SFX_Sounds.BANDIT_BLUE);
                    break;

                case BanditVoiceType.Green:
                    AudioManager.Instance.PlaySFX(AudioManager.SFX_Sounds.BANDIT_GREEN);
                    break;

                case BanditVoiceType.Skull:
                    AudioManager.Instance.PlaySFX(AudioManager.SFX_Sounds.BANDIT_SKULL);
                    break;
            }
        }

        private void SetLocalRotation(float localY, float localZ)
        {
            Vector3 localEuler = transform.localEulerAngles;
            localEuler.x = cachedLocalX;
            localEuler.y = localY;
            localEuler.z = localZ;
            transform.localEulerAngles = localEuler;
        }
    }
}