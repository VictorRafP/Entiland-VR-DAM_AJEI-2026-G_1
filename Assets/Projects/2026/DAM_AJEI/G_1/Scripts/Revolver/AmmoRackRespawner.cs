using UnityEngine;

namespace Entiland_VR_DAM_AJEI_2026_G_1
{
    /// <summary>
    /// Controla la municion para que vaya spawneando todo el rato, si las balas salen de la zona, spawnea mas
    /// </summary>
    public class AmmoRackRespawner : MonoBehaviour
    {
        [Header("Ammo Setup")]
        [SerializeField] private GameObject ammoPrefab;
        [SerializeField] private Transform[] spawnPoints = new Transform[6];

        [Header("Detection")]
        [SerializeField] private Transform detectionCenter;
        [SerializeField] private float detectionRadius = 0.35f;
        [SerializeField] private float checkInterval = 0.25f;

        [Header("Start")]
        [SerializeField] private bool spawnOnStart = true;

        private GameObject[] spawnedAmmo;
        private float checkTimer = 0f;

        private void Awake()
        {
            EnsureArraySize();
        }

        private void Start()
        {
            if (spawnOnStart)
            {
                SpawnMissingAmmo();
            }
        }

        private void Update()
        {
            if (ShootingGalleryGameManager.Instance != null &&
                !ShootingGalleryGameManager.Instance.IsGameplayRunning)
            {
                return;
            }

            checkTimer -= Time.deltaTime;
            if (checkTimer > 0f)
            {
                return;
            }

            checkTimer = checkInterval;
            SpawnMissingAmmo();
        }

        private void SpawnMissingAmmo()
        {
            if (ammoPrefab == null || spawnPoints == null || spawnPoints.Length == 0)
            {
                return;
            }

            EnsureArraySize();

            for (int i = 0; i < spawnPoints.Length; i++)
            {
                Transform currentSpawnPoint = spawnPoints[i];
                if (currentSpawnPoint == null)
                {
                    continue;
                }

                if (!IsSlotMissingAmmo(i))
                {
                    continue;
                }

                SpawnAmmoForSlot(i);
            }
        }

        private bool IsSlotMissingAmmo(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= spawnedAmmo.Length)
            {
                return false;
            }

            GameObject currentAmmo = spawnedAmmo[slotIndex];

            if (currentAmmo == null)
            {
                return true;
            }

            if (!currentAmmo.activeInHierarchy)
            {
                return true;
            }

            Vector3 centerPosition = GetDetectionCenterPosition();
            Vector3 delta = currentAmmo.transform.position - centerPosition;
            float radiusSqr = detectionRadius * detectionRadius;

            if (delta.sqrMagnitude > radiusSqr)
            {
                return true;
            }

            return false;
        }

        private void SpawnAmmoForSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= spawnPoints.Length)
            {
                return;
            }

            Transform currentSpawnPoint = spawnPoints[slotIndex];
            if (currentSpawnPoint == null)
            {
                return;
            }

            GameObject newAmmo = Instantiate(
                ammoPrefab,
                currentSpawnPoint.position,
                currentSpawnPoint.rotation);

            spawnedAmmo[slotIndex] = newAmmo;
            ResetAmmoInstance(newAmmo);
        }

        private void ResetAmmoInstance(GameObject ammoInstance)
        {
            if (ammoInstance == null)
            {
                return;
            }

            Rigidbody ammoBody = ammoInstance.GetComponent<Rigidbody>();
            if (ammoBody != null)
            {
                ammoBody.linearVelocity = Vector3.zero;
                ammoBody.angularVelocity = Vector3.zero;
                ammoBody.isKinematic = false;
                ammoBody.useGravity = true;
            }

            RevolverAmmoRound ammoRound = ammoInstance.GetComponent<RevolverAmmoRound>();
            if (ammoRound != null)
            {
                ammoRound.ResetRound();
            }

            ammoInstance.SetActive(true);
        }

        private Vector3 GetDetectionCenterPosition()
        {
            if (detectionCenter != null)
            {
                return detectionCenter.position;
            }

            return transform.position;
        }

        private void EnsureArraySize()
        {
            int targetSize = spawnPoints != null ? spawnPoints.Length : 0;

            if (spawnedAmmo != null && spawnedAmmo.Length == targetSize)
            {
                return;
            }

            GameObject[] newArray = new GameObject[targetSize];

            if (spawnedAmmo != null)
            {
                int copyLength = spawnedAmmo.Length;
                if (copyLength > newArray.Length)
                {
                    copyLength = newArray.Length;
                }

                for (int i = 0; i < copyLength; i++)
                {
                    newArray[i] = spawnedAmmo[i];
                }
            }

            spawnedAmmo = newArray;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(GetDetectionCenterPosition(), detectionRadius);
        }
    }
}