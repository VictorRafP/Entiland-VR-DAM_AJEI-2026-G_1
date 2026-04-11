using System.Collections.Generic;
using UnityEngine;

namespace Entiland_VR_DAM_AJEI_2026_G_1
{
    /// <summary>
    /// Controla a los bandidos
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

        [Header("Bandit Variants")]
        [SerializeField] private List<GameObject> banditVariants = new List<GameObject>();
        [SerializeField] private VariantSelectionMode selectionMode = VariantSelectionMode.Sequential;
        [SerializeField] private int defaultVariantIndex = 0;

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

        [Header("Audio")]
        [SerializeField] private AudioSource hitAudioSource;
        [SerializeField] private AudioClip hitSound;
        [SerializeField] private AudioSource shootAudioSource;
        [SerializeField] private AudioClip shootSound;

        private BanditState currentState = BanditState.Hidden;
        private float hiddenTimer = 0f;
        private float visibleShotTimer = 0f;
        private float cachedLocalX = 0f;
        private int currentVariantIndex = -1;
        private int nextSequentialIndex = 0;
        private bool pendingShowFromHidden = false;

        public bool IsVisible
        {
            get { return currentState == BanditState.Visible; }
        }

        private void Awake()
        {
            cachedLocalX = transform.localEulerAngles.x;

            DisableAllVariants();

            if (banditVariants.Count > 0)
            {
                int startIndex = Mathf.Clamp(defaultVariantIndex, 0, banditVariants.Count - 1);
                currentVariantIndex = startIndex;
                nextSequentialIndex = startIndex;
                ActivateVariant(startIndex);
            }

            if (startHidden)
            {
                currentState = BanditState.Hidden;
                SetLocalRotation(hiddenLocalY, hiddenLocalZ);

                if (riseOnStart)
                {
                    pendingShowFromHidden = true;
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

            UpdateStateAnimation();
            UpdateStateLogic();
        }

        public void ShowBandit()
        {
            if (ShootingGalleryGameManager.Instance != null &&
                !ShootingGalleryGameManager.Instance.IsGameplayRunning)
            {
                return;
            }

            if (currentState == BanditState.Visible || currentState == BanditState.Rising)
            {
                return;
            }

            SelectVariantForShow();
            pendingShowFromHidden = false;
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
            if (ShootingGalleryGameManager.Instance != null &&
                !ShootingGalleryGameManager.Instance.IsGameplayRunning)
            {
                return;
            }

            if (!IsVisible)
            {
                return;
            }

            if (ShootingGalleryGameManager.Instance != null)
            {
                ShootingGalleryGameManager.Instance.RegisterBanditHit(scoreOnHit);
            }

            PlayHitSound();

            currentState = BanditState.Falling;
            pendingShowFromHidden = respawnAfterFall;
        }

        public void SetRespawnDelay(float newRespawnDelay)
        {
            respawnDelay = Mathf.Max(0f, newRespawnDelay);
        }

        public void SetTimeBeforePlayerShot(float newShotDelay)
        {
            timeBeforePlayerShot = Mathf.Max(0.1f, newShotDelay);
        }

        [ContextMenu("Debug Show Bandit")]
        public void DebugShowBandit()
        {
            ShowBandit();
        }

        [ContextMenu("Debug Hide Bandit")]
        public void DebugHideBandit()
        {
            HideBandit();
        }

        [ContextMenu("Debug Hit Bandit")]
        public void DebugHitBandit()
        {
            HitBandit();
        }

        private void UpdateStateAnimation()
        {
            if (currentState == BanditState.Rising)
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

                if (reachedY && reachedZ)
                {
                    SetLocalRotation(standingLocalY, standingLocalZ);
                    currentState = BanditState.Visible;
                    visibleShotTimer = Mathf.Max(0f, timeBeforePlayerShot);
                }
            }
            else if (currentState == BanditState.Falling)
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

                if (reachedY && reachedZ)
                {
                    SetLocalRotation(hiddenLocalY, hiddenLocalZ);
                    currentState = BanditState.Hidden;

                    if (pendingShowFromHidden)
                    {
                        hiddenTimer = Mathf.Max(0f, respawnDelay);
                    }
                    else
                    {
                        hiddenTimer = 0f;
                    }
                }
            }
        }

        private void UpdateStateLogic()
        {
            if (currentState == BanditState.Hidden)
            {
                if (!pendingShowFromHidden)
                {
                    return;
                }

                hiddenTimer -= Time.deltaTime;
                if (hiddenTimer <= 0f)
                {
                    ShowBandit();
                }

                return;
            }

            if (currentState == BanditState.Visible)
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
        }

        private void ShootPlayer()
        {
            PlayShootSound();

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
            pendingShowFromHidden = respawnAfterFall;
        }

        private void SelectVariantForShow()
        {
            if (banditVariants == null || banditVariants.Count == 0)
            {
                return;
            }

            if (selectionMode == VariantSelectionMode.Fixed)
            {
                int fixedIndex = Mathf.Clamp(defaultVariantIndex, 0, banditVariants.Count - 1);
                ActivateVariant(fixedIndex);
                return;
            }

            if (selectionMode == VariantSelectionMode.Random)
            {
                int randomIndex = Random.Range(0, banditVariants.Count);
                ActivateVariant(randomIndex);
                return;
            }

            if (selectionMode == VariantSelectionMode.Sequential)
            {
                if (nextSequentialIndex < 0 || nextSequentialIndex >= banditVariants.Count)
                {
                    nextSequentialIndex = 0;
                }

                ActivateVariant(nextSequentialIndex);

                nextSequentialIndex++;
                if (nextSequentialIndex >= banditVariants.Count)
                {
                    nextSequentialIndex = 0;
                }
            }
        }

        private void ActivateVariant(int variantIndex)
        {
            DisableAllVariants();

            if (variantIndex < 0 || variantIndex >= banditVariants.Count)
            {
                currentVariantIndex = -1;
                return;
            }

            GameObject selected = banditVariants[variantIndex];
            if (selected != null)
            {
                selected.SetActive(true);
                currentVariantIndex = variantIndex;
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

        private void PlayHitSound()
        {
            if (hitAudioSource == null || hitSound == null)
            {
                return;
            }

            hitAudioSource.PlayOneShot(hitSound);
        }

        private void PlayShootSound()
        {
            if (shootAudioSource == null || shootSound == null)
            {
                return;
            }

            shootAudioSource.PlayOneShot(shootSound);
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