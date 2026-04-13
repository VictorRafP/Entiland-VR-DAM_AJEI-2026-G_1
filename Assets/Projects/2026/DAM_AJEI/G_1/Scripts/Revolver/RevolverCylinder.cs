using UnityEngine;

namespace EntilandVR.DosCuatro.DAM_AJEI.G_Uno
{
    /// <summary>
    /// Controla el tambor del revólver, su posición, su estado y el tipo de bala cargada en cada recámara.
    /// </summary>
    public class RevolverCylinder : MonoBehaviour
    {
        private struct ChamberData
        {
            public bool loaded;
            public RevolverAmmoRound.AmmoType ammoType;
        }

        [Header("Visual Bullets")]
        [SerializeField] private GameObject[] visualBullets = new GameObject[6];
        [SerializeField] private Material normalVisualBulletMaterial;
        [SerializeField] private Material explosiveVisualBulletMaterial;
        [SerializeField] private Material tripleVisualBulletMaterial;

        [Header("Reload")]
        [SerializeField] private Collider reloadTrigger;

        [Header("Open / Closed Poses")]
        [SerializeField] private Transform closedPose;
        [SerializeField] private Transform openPose;
        [SerializeField] private float poseMoveSpeed = 8f;
        [SerializeField] private float poseRotateSpeed = 240f;

        [Header("Spin Rotation")]
        [SerializeField] private Transform cylinderSpin;
        [SerializeField] private float spinStepAngle = 58.8f;
        [SerializeField] private float spinRotationSpeed = 240f;

        [Header("Ammo")]
        [SerializeField] private bool startFull = true;

        private ChamberData[] chambers = new ChamberData[6];

        private bool isOpenRequested = false;
        private bool isFullyOpen = false;
        private bool isFullyClosed = true;
        private bool isSpinInProgress = false;

        private float targetSpinLocalY = 0f;
        private int currentChamberIndex = 0;

        public bool IsOpen
        {
            get { return isFullyOpen; }
        }

        public bool IsClosed
        {
            get { return isFullyClosed; }
        }

        public bool IsBusy
        {
            get { return !isFullyOpen && !isFullyClosed; }
        }

        public int LoadedCount
        {
            get
            {
                int count = 0;

                for (int i = 0; i < chambers.Length; i++)
                {
                    if (chambers[i].loaded)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        private void Awake()
        {
            for (int i = 0; i < chambers.Length; i++)
            {
                chambers[i].loaded = startFull;
                chambers[i].ammoType = RevolverAmmoRound.AmmoType.Normal;
            }

            RefreshVisualBullets();
            SetReloadTriggerState(false);

            if (closedPose != null)
            {
                transform.localPosition = closedPose.localPosition;
                transform.localRotation = closedPose.localRotation;
            }

            if (cylinderSpin != null)
            {
                targetSpinLocalY = NormalizeAngle(cylinderSpin.localEulerAngles.y);
                SetCurrentSpinLocalYRotation(targetSpinLocalY);
            }

            isOpenRequested = false;
            isFullyOpen = false;
            isFullyClosed = true;
            isSpinInProgress = false;
            currentChamberIndex = 0;
        }

        private void FixedUpdate()
        {
            if (ShootingGalleryGameManager.Instance != null &&
                !ShootingGalleryGameManager.Instance.IsGameplayRunning)
            {
                return;
            }

            UpdateOpenPose();
            UpdateSpinRotation();
        }

        public bool CanFire()
        {
            if (!isFullyClosed)
            {
                return false;
            }

            if (isSpinInProgress)
            {
                return false;
            }

            if (LoadedCount <= 0)
            {
                return false;
            }

            return chambers[currentChamberIndex].loaded;
        }

        public bool TryConsumeRoundForShot(out RevolverAmmoRound.AmmoType firedAmmoType)
        {
            firedAmmoType = RevolverAmmoRound.AmmoType.Normal;

            if (!CanFire())
            {
                return false;
            }

            firedAmmoType = chambers[currentChamberIndex].ammoType;
            chambers[currentChamberIndex].loaded = false;
            chambers[currentChamberIndex].ammoType = RevolverAmmoRound.AmmoType.Normal;

            RefreshVisualBullets();
            AdvanceSpin();
            AdvanceChamberIndex();

            if (LoadedCount <= 0)
            {
                OpenCylinder();
            }

            return true;
        }

        public void TryInsertRoundFromTrigger(Collider other)
        {
            if (!isFullyOpen || other == null)
            {
                return;
            }

            RevolverAmmoRound ammoRound = other.GetComponent<RevolverAmmoRound>();
            if (ammoRound == null || ammoRound.IsConsumed)
            {
                return;
            }

            if (TryInsertRound(ammoRound) && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.SFX_Sounds.RELOAD);
            }
        }

        public void OpenCylinder()
        {
            if (IsBusy)
            {
                return;
            }

            isOpenRequested = true;
            isFullyClosed = false;
            SetReloadTriggerState(false);
        }

        public void CloseCylinder()
        {
            if (IsBusy)
            {
                return;
            }

            if (LoadedCount <= 0)
            {
                return;
            }

            isOpenRequested = false;
            isFullyOpen = false;
            SetReloadTriggerState(false);
        }

        public void ToggleCylinder()
        {
            if (IsBusy)
            {
                return;
            }

            if (isFullyOpen)
            {
                CloseCylinder();
            }
            else if (isFullyClosed)
            {
                OpenCylinder();
            }
        }

        private bool TryInsertRound(RevolverAmmoRound ammoRound)
        {
            for (int i = 0; i < chambers.Length; i++)
            {
                int slotIndex = currentChamberIndex + i;
                if (slotIndex >= chambers.Length)
                {
                    slotIndex -= chambers.Length;
                }

                if (chambers[slotIndex].loaded)
                {
                    continue;
                }

                chambers[slotIndex].loaded = true;
                chambers[slotIndex].ammoType = ammoRound.CurrentAmmoType;

                ammoRound.Consume();
                RefreshVisualBullets();
                return true;
            }

            return false;
        }

        private void RefreshVisualBullets()
        {
            int count = visualBullets.Length;
            if (count > chambers.Length)
            {
                count = chambers.Length;
            }

            for (int i = 0; i < count; i++)
            {
                GameObject currentBullet = visualBullets[i];
                if (currentBullet == null)
                {
                    continue;
                }

                currentBullet.SetActive(chambers[i].loaded);

                if (!chambers[i].loaded)
                {
                    continue;
                }

                Renderer currentRenderer = currentBullet.GetComponentInChildren<Renderer>(true);
                if (currentRenderer == null)
                {
                    continue;
                }

                Material targetMaterial = GetVisualMaterial(chambers[i].ammoType);
                if (targetMaterial != null)
                {
                    currentRenderer.material = targetMaterial;
                }
            }
        }

        private Material GetVisualMaterial(RevolverAmmoRound.AmmoType ammoType)
        {
            switch (ammoType)
            {
                case RevolverAmmoRound.AmmoType.Explosive:
                    return explosiveVisualBulletMaterial;

                case RevolverAmmoRound.AmmoType.Triple:
                    return tripleVisualBulletMaterial;

                default:
                    return normalVisualBulletMaterial;
            }
        }

        private void UpdateOpenPose()
        {
            Transform targetPose = isOpenRequested ? openPose : closedPose;
            if (targetPose == null)
            {
                return;
            }

            transform.localPosition = Vector3.MoveTowards(
                transform.localPosition,
                targetPose.localPosition,
                poseMoveSpeed * Time.fixedDeltaTime);

            transform.localRotation = Quaternion.RotateTowards(
                transform.localRotation,
                targetPose.localRotation,
                poseRotateSpeed * Time.fixedDeltaTime);

            bool reachedPosition = Vector3.Distance(transform.localPosition, targetPose.localPosition) <= 0.0005f;
            bool reachedRotation = Quaternion.Angle(transform.localRotation, targetPose.localRotation) <= 0.05f;

            if (reachedPosition && reachedRotation)
            {
                transform.localPosition = targetPose.localPosition;
                transform.localRotation = targetPose.localRotation;

                if (isOpenRequested)
                {
                    isFullyOpen = true;
                    isFullyClosed = false;
                    SetReloadTriggerState(true);
                }
                else
                {
                    isFullyOpen = false;
                    isFullyClosed = true;
                    SetReloadTriggerState(false);
                }
            }
            else
            {
                isFullyOpen = false;
                isFullyClosed = false;
                SetReloadTriggerState(false);
            }
        }

        private void UpdateSpinRotation()
        {
            if (cylinderSpin == null)
            {
                return;
            }

            float currentSpinY = NormalizeAngle(cylinderSpin.localEulerAngles.y);
            float nextSpinY = Mathf.MoveTowardsAngle(
                currentSpinY,
                targetSpinLocalY,
                spinRotationSpeed * Time.fixedDeltaTime);

            SetCurrentSpinLocalYRotation(nextSpinY);

            bool reachedTarget = Mathf.Abs(Mathf.DeltaAngle(nextSpinY, targetSpinLocalY)) <= 0.05f;
            isSpinInProgress = !reachedTarget;

            if (reachedTarget)
            {
                SetCurrentSpinLocalYRotation(targetSpinLocalY);
            }
        }

        private void AdvanceSpin()
        {
            if (cylinderSpin == null)
            {
                return;
            }

            targetSpinLocalY = NormalizeAngle(targetSpinLocalY + spinStepAngle);
            isSpinInProgress = true;
        }

        private void AdvanceChamberIndex()
        {
            currentChamberIndex++;

            if (currentChamberIndex >= chambers.Length)
            {
                currentChamberIndex = 0;
            }
        }

        private void SetReloadTriggerState(bool enabledState)
        {
            if (reloadTrigger != null)
            {
                reloadTrigger.enabled = enabledState;
            }
        }

        private void SetCurrentSpinLocalYRotation(float localY)
        {
            if (cylinderSpin == null)
            {
                return;
            }

            Vector3 localEuler = cylinderSpin.localEulerAngles;
            localEuler.y = localY;
            cylinderSpin.localEulerAngles = localEuler;
        }

        private float NormalizeAngle(float angle)
        {
            if (angle > 180f)
            {
                angle -= 360f;
            }

            return angle;
        }
    }
}