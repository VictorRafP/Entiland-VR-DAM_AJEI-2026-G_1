using UnityEngine;

namespace EntilandVR.DosCuatro.DAM_AJEI.G_Uno
{
    /// <summary>
    /// Controla la munición del soporte.
    /// Si una bala desaparece o sale del radio, vuelve a generar otra en ese slot.
    /// También asigna el tipo de bala según probabilidades.
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

        [Header("Ammo Types")]
        [SerializeField] private bool useRandomAmmoTypes = true;
        [SerializeField] private RevolverAmmoRound.AmmoType fixedAmmoType = RevolverAmmoRound.AmmoType.Normal;
        [SerializeField] private float normalChance = 70f;
        [SerializeField] private float explosiveChance = 15f;
        [SerializeField] private float tripleChance = 15f;

        private GameObject[] spawnedAmmo;
        private float checkTimer = 0f;

        private void Awake()
        {
            spawnedAmmo = new GameObject[spawnPoints.Length];
        }

        private void Start()
        {
            if (spawnOnStart)
            {
                for (int i = 0; i < spawnPoints.Length; i++)
                {
                    SpawnAmmoInSlot(i);
                }
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

            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (SlotNeedsAmmo(i))
                {
                    SpawnAmmoInSlot(i);
                }
            }
        }

        private bool SlotNeedsAmmo(int slotIndex)
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

            Vector3 center = detectionCenter != null ? detectionCenter.position : transform.position;
            Vector3 delta = currentAmmo.transform.position - center;

            return delta.sqrMagnitude > detectionRadius * detectionRadius;
        }

        private void SpawnAmmoInSlot(int slotIndex)
        {
            if (ammoPrefab == null)
            {
                return;
            }

            if (slotIndex < 0 || slotIndex >= spawnPoints.Length)
            {
                return;
            }

            Transform spawnPoint = spawnPoints[slotIndex];
            if (spawnPoint == null)
            {
                return;
            }

            GameObject newAmmo = Instantiate(ammoPrefab, spawnPoint.position, spawnPoint.rotation);
            spawnedAmmo[slotIndex] = newAmmo;

            Rigidbody ammoBody = newAmmo.GetComponent<Rigidbody>();
            if (ammoBody != null)
            {
                ammoBody.linearVelocity = Vector3.zero;
                ammoBody.angularVelocity = Vector3.zero;
                ammoBody.isKinematic = false;
                ammoBody.useGravity = true;
            }

            RevolverAmmoRound ammoRound = newAmmo.GetComponent<RevolverAmmoRound>();
            if (ammoRound != null)
            {
                ammoRound.ResetRound();
                ammoRound.ConfigureAmmoType(GetAmmoTypeToSpawn());
            }
        }

        private RevolverAmmoRound.AmmoType GetAmmoTypeToSpawn()
        {
            if (!useRandomAmmoTypes)
            {
                return fixedAmmoType;
            }

            float totalChance = normalChance + explosiveChance + tripleChance;
            if (totalChance <= 0f)
            {
                return RevolverAmmoRound.AmmoType.Normal;
            }

            float roll = Random.Range(0f, totalChance);

            if (roll < normalChance)
            {
                return RevolverAmmoRound.AmmoType.Normal;
            }

            roll -= normalChance;

            if (roll < explosiveChance)
            {
                return RevolverAmmoRound.AmmoType.Explosive;
            }

            return RevolverAmmoRound.AmmoType.Triple;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;

            Vector3 center = detectionCenter != null ? detectionCenter.position : transform.position;
            Gizmos.DrawWireSphere(center, detectionRadius);
        }
    }
}