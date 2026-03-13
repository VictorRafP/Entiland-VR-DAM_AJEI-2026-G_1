using UnityEngine;
using Autohand;

namespace Entiland_VR_DAM_AJEI_2026_G_1
{
    /// <summary>
    /// Controla el tambor del revólver:
    /// estado de munición, balas visuales, apertura/cierre, recarga por trigger
    /// y giro del tambor por disparo sobre CylinderSpin.
    /// </summary>
    public class RevolverCylinder : MonoBehaviour
    {
        [Header("Visual Bullets")]
        [SerializeField] private GameObject[] visualBullets = new GameObject[6];

        [Header("Reload")]
        [SerializeField] private Collider reloadTrigger;

        [Header("Cylinder Open Rotation")]
        [SerializeField] private float closedLocalY = 0f;
        [SerializeField] private float openedLocalY = -16f;
        [SerializeField] private float openRotationSpeed = 180f;

        [Header("Cylinder Spin")]
        [SerializeField] private Transform cylinderSpin;
        [SerializeField] private float spinStepAngle = 58.8f;
        [SerializeField] private float spinRotationSpeed = 360f;

        [Header("Ammo")]
        [SerializeField] private bool startFull = true;

        private readonly bool[] loadedChambers = new bool[6];

        private Rigidbody[] visualBulletBodies;
        private Collider[][] visualBulletColliders;
        private Grabbable[] visualBulletGrabbables;

        private bool isOpenRequested = false;
        private bool isFullyOpen = false;
        private bool isFullyClosed = true;

        private float currentSpinLocalY = 0f;
        private float targetSpinLocalY = 0f;
        private bool isSpinInProgress = false;

        private int currentChamberIndex = 0;

        public bool IsOpen
        {
            get { return isFullyOpen; }
        }

        public bool IsClosed
        {
            get { return isFullyClosed; }
        }

        public bool IsInTransition
        {
            get { return !isFullyOpen && !isFullyClosed; }
        }

        public bool IsSpinInProgress
        {
            get { return isSpinInProgress; }
        }

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
            CacheVisualBulletComponents();
            InitializeChambers();
            RefreshVisualBullets();

            SetReloadTriggerState(false);
            SetCurrentOpenLocalYRotation(closedLocalY);

            currentSpinLocalY = NormalizeAngle(GetSpinLocalY());
            targetSpinLocalY = currentSpinLocalY;
            SetCurrentSpinLocalYRotation(currentSpinLocalY);

            isOpenRequested = false;
            isFullyOpen = false;
            isFullyClosed = true;
            isSpinInProgress = false;
            currentChamberIndex = 0;
        }

        private void FixedUpdate()
        {
            UpdateCylinderOpenRotation();
            UpdateCylinderSpinRotation();
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

            return loadedChambers[currentChamberIndex];
        }

        public bool TryConsumeRoundForShot()
        {
            Debug.Log("RevolverCylinder -> TryConsumeRoundForShot() START | LoadedCount: " + LoadedCount + " | currentChamberIndex: " + currentChamberIndex, this);

            if (!CanFire())
            {
                Debug.Log("RevolverCylinder -> CanFire() FALSE", this);
                return false;
            }

            loadedChambers[currentChamberIndex] = false;

            Debug.Log("RevolverCylinder -> Chamber consumed: " + currentChamberIndex, this);

            RefreshVisualBullets();

            AdvanceCylinderSpin();
            AdvanceChamberIndex();

            Debug.Log("RevolverCylinder -> LoadedCount after consume: " + LoadedCount + " | next chamber: " + currentChamberIndex, this);

            if (LoadedCount <= 0)
            {
                Debug.Log("RevolverCylinder -> No ammo left, opening cylinder", this);
                OpenCylinder();
            }

            return true;
        }

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

        public void OpenCylinder()
        {
            isOpenRequested = true;
            isFullyClosed = false;
            SetReloadTriggerState(false);
        }

        public void CloseCylinder()
        {
            isOpenRequested = false;
            isFullyOpen = false;
            SetReloadTriggerState(false);
        }

        public void ToggleCylinder()
        {
            if (isOpenRequested || isFullyOpen)
            {
                CloseCylinder();
            }
            else
            {
                OpenCylinder();
            }
        }

        public bool HasEmptyChamber()
        {
            for (int i = 0; i < loadedChambers.Length; i++)
            {
                if (!loadedChambers[i])
                {
                    return true;
                }
            }

            return false;
        }

        public void FillAllChambers()
        {
            for (int i = 0; i < loadedChambers.Length; i++)
            {
                loadedChambers[i] = true;
            }

            currentChamberIndex = 0;
            RefreshVisualBullets();
            CloseCylinder();
        }

        private void CacheVisualBulletComponents()
        {
            int bulletCount = visualBullets != null ? visualBullets.Length : 0;

            visualBulletBodies = new Rigidbody[bulletCount];
            visualBulletColliders = new Collider[bulletCount][];
            visualBulletGrabbables = new Grabbable[bulletCount];

            for (int i = 0; i < bulletCount; i++)
            {
                GameObject bullet = visualBullets[i];
                if (bullet == null)
                {
                    continue;
                }

                visualBulletBodies[i] = bullet.GetComponent<Rigidbody>();
                visualBulletColliders[i] = bullet.GetComponentsInChildren<Collider>(true);
                visualBulletGrabbables[i] = bullet.GetComponent<Grabbable>();
            }
        }

        private void InitializeChambers()
        {
            for (int i = 0; i < loadedChambers.Length; i++)
            {
                loadedChambers[i] = startFull;
            }
        }

        private void UpdateCylinderOpenRotation()
        {
            float currentY = NormalizeAngle(transform.localEulerAngles.y);
            float targetY = isOpenRequested ? openedLocalY : closedLocalY;
            float nextY = Mathf.MoveTowardsAngle(currentY, targetY, openRotationSpeed * Time.fixedDeltaTime);

            SetCurrentOpenLocalYRotation(nextY);

            bool reachedTarget = Mathf.Abs(Mathf.DeltaAngle(nextY, targetY)) <= 0.05f;

            if (reachedTarget)
            {
                SetCurrentOpenLocalYRotation(targetY);

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

        private void UpdateCylinderSpinRotation()
        {
            if (cylinderSpin == null)
            {
                return;
            }

            currentSpinLocalY = NormalizeAngle(GetSpinLocalY());
            float nextSpinY = Mathf.MoveTowardsAngle(
                currentSpinLocalY,
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

        private void AdvanceCylinderSpin()
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

                if (!HasEmptyChamber())
                {
                    CloseCylinder();
                }

                return true;
            }

            return false;
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
                GameObject bullet = visualBullets[i];
                if (bullet == null)
                {
                    continue;
                }

                bool isLoaded = loadedChambers[i];
                bullet.SetActive(isLoaded);

                Rigidbody bulletBody = visualBulletBodies != null && i < visualBulletBodies.Length ? visualBulletBodies[i] : null;
                if (bulletBody != null)
                {
                    bulletBody.isKinematic = true;
                    bulletBody.useGravity = false;
                    bulletBody.linearVelocity = Vector3.zero;
                    bulletBody.angularVelocity = Vector3.zero;
                }

                Grabbable bulletGrabbable = visualBulletGrabbables != null && i < visualBulletGrabbables.Length ? visualBulletGrabbables[i] : null;
                if (bulletGrabbable != null)
                {
                    bulletGrabbable.enabled = false;
                }

                Collider[] bulletColliderArray = visualBulletColliders != null && i < visualBulletColliders.Length ? visualBulletColliders[i] : null;
                if (bulletColliderArray != null)
                {
                    for (int colliderIndex = 0; colliderIndex < bulletColliderArray.Length; colliderIndex++)
                    {
                        if (bulletColliderArray[colliderIndex] != null)
                        {
                            bulletColliderArray[colliderIndex].enabled = false;
                        }
                    }
                }
            }
        }

        private void SetReloadTriggerState(bool enabledState)
        {
            if (reloadTrigger == null)
            {
                return;
            }

            reloadTrigger.enabled = enabledState;
        }

        private void SetCurrentOpenLocalYRotation(float localY)
        {
            Vector3 localEuler = transform.localEulerAngles;
            localEuler.y = localY;
            transform.localEulerAngles = localEuler;
        }

        private float GetSpinLocalY()
        {
            if (cylinderSpin == null)
            {
                return 0f;
            }

            return cylinderSpin.localEulerAngles.y;
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