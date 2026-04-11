using UnityEngine;

namespace Entiland_VR_DAM_AJEI_2026_G_1
{
    /// <summary>
    /// Controla el tambor del revólver, su posicion y cuando esta cargado o no
    /// </summary>
    public class RevolverCylinder : MonoBehaviour
    {
        [Header("Visual Bullets")]
        [SerializeField] private GameObject[] visualBullets = new GameObject[6];

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

        private readonly bool[] loadedChambers = new bool[6];

        private bool isOpenRequested = false;
        private bool isFullyOpen = false;
        private bool isFullyClosed = true;

        private float targetSpinLocalY = 0f;
        private bool isSpinInProgress = false;

        private int currentChamberIndex = 0;

        /// <summary>
        /// Indica si tambor abierto
        /// </summary>
        public bool IsOpen
        {
            get { return isFullyOpen; }
        }

        /// <summary>
        /// Indica si tambor cerrado
        /// </summary>
        public bool IsClosed
        {
            get { return isFullyClosed; }
        }

        public bool IsBusy
        {
            get { return !isFullyOpen && !isFullyClosed; }
        }

        /// <summary>
        /// Cantidad actual de balas cargadas
        /// </summary>
        public int LoadedCount
        {
            get
            {
                int count = 0;

                for (int i = 0; i < loadedChambers.Length; i++)
                {
                    if (loadedChambers[i])
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        private void Awake()
        {
            InitializeChambers();
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
            UpdateOpenPose();
            UpdateSpinRotation();
        }

        /// <summary>
        /// Devuelve true si el revolver puede disparar
        /// </summary>
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

            return loadedChambers[currentChamberIndex];
        }

        /// <summary>
        /// Consume una bala si el revolver puede disparar
        /// </summary>
        public bool TryConsumeRoundForShot()
        {
            if (!CanFire())
            {
                return false;
            }

            loadedChambers[currentChamberIndex] = false;
            RefreshVisualBullets();

            AdvanceSpin();
            AdvanceChamberIndex();

            if (LoadedCount <= 0)
            {
                OpenCylinder();
            }

            return true;
        }

        /// <summary>
        /// Cargar balas
        /// </summary>
        public void TryInsertRoundFromTrigger(Collider other)
        {
            if (!isFullyOpen)
            {
                return;
            }

            if (other == null)
            {
                return;
            }

            RevolverAmmoRound ammoRound = other.GetComponent<RevolverAmmoRound>();
            if (ammoRound == null)
            {
                return;
            }

            if (ammoRound.IsConsumed)
            {
                return;
            }

            TryInsertRound(ammoRound);
        }

        /// <summary>
        /// Apertura del tambor
        /// </summary>
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

        /// <summary>
        /// Cerrar tambor, solo cierra si hay al menos una bala cargada
        /// </summary>
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

        /// <summary>
        /// Toggle entre abierto y cerrado
        /// </summary>
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

        private void InitializeChambers()
        {
            for (int i = 0; i < loadedChambers.Length; i++)
            {
                loadedChambers[i] = startFull;
            }
        }

        private void RefreshVisualBullets()
        {
            if (visualBullets == null)
            {
                return;
            }

            int count = visualBullets.Length;
            if (count > loadedChambers.Length)
            {
                count = loadedChambers.Length;
            }

            for (int i = 0; i < count; i++)
            {
                if (visualBullets[i] != null)
                {
                    visualBullets[i].SetActive(loadedChambers[i]);
                }
            }
        }

        private void UpdateOpenPose()
        {
            Transform targetPose = isOpenRequested ? openPose : closedPose;
            if (targetPose == null)
            {
                return;
            }

            Vector3 currentPosition = transform.localPosition;
            Quaternion currentRotation = transform.localRotation;

            Vector3 nextPosition = Vector3.MoveTowards(
                currentPosition,
                targetPose.localPosition,
                poseMoveSpeed * Time.fixedDeltaTime);

            Quaternion nextRotation = Quaternion.RotateTowards(
                currentRotation,
                targetPose.localRotation,
                poseRotateSpeed * Time.fixedDeltaTime);

            transform.localPosition = nextPosition;
            transform.localRotation = nextRotation;

            bool reachedPosition = Vector3.Distance(nextPosition, targetPose.localPosition) <= 0.0005f;
            bool reachedRotation = Quaternion.Angle(nextRotation, targetPose.localRotation) <= 0.05f;

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

            if (reachedTarget)
            {
                SetCurrentSpinLocalYRotation(targetSpinLocalY);
                isSpinInProgress = false;
            }
            else
            {
                isSpinInProgress = true;
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

            if (currentChamberIndex >= loadedChambers.Length)
            {
                currentChamberIndex = 0;
            }
        }

        private bool TryInsertRound(RevolverAmmoRound ammoRound)
        {
            for (int offset = 0; offset < loadedChambers.Length; offset++)
            {
                int chamberIndex = currentChamberIndex + offset;
                if (chamberIndex >= loadedChambers.Length)
                {
                    chamberIndex -= loadedChambers.Length;
                }

                if (loadedChambers[chamberIndex])
                {
                    continue;
                }

                loadedChambers[chamberIndex] = true;
                ammoRound.Consume();
                RefreshVisualBullets();

                return true;
            }

            return false;
        }

        private void SetReloadTriggerState(bool enabledState)
        {
            if (reloadTrigger == null)
            {
                return;
            }

            reloadTrigger.enabled = enabledState;
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