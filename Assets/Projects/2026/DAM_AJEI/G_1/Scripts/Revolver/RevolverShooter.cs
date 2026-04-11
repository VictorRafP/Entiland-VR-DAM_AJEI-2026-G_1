using UnityEngine;

namespace Entiland_VR_DAM_AJEI_2026_G_1
{
    /// <summary>
    /// Se encarga de gestionar el disparo y sus condiciones, cuando puede disparar y cuando puede recargar
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class RevolverShooter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Rigidbody revolverBody;
        [SerializeField] private RevolverCylinder revolverCylinder;
        [SerializeField] private Transform barrelTip;
        [SerializeField] private RevolverShotTracer shotTracer;

        [Header("Shot")]
        [SerializeField] private float range = 100f;
        [SerializeField] private float hitPower = 8f;
        [SerializeField] private float recoilPower = 1.25f;
        [SerializeField] private LayerMask hitMask = ~0;

        [Header("Audio")]
        [SerializeField] private AudioSource shootAudioSource;
        [SerializeField] private AudioSource emptyAudioSource;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = false;

        private bool shootRequested = false;
        private Collider[] ownColliders;

        private void Awake()
        {
            if (revolverBody == null)
            {
                revolverBody = GetComponent<Rigidbody>();
            }

            if (revolverCylinder == null)
            {
                revolverCylinder = GetComponentInChildren<RevolverCylinder>(true);
            }

            ownColliders = GetComponentsInChildren<Collider>(true);
        }

        private void FixedUpdate()
        {
            if (ShootingGalleryGameManager.Instance != null &&
                !ShootingGalleryGameManager.Instance.IsGameplayRunning)
            {
                shootRequested = false;
                return;
            }

            if (!shootRequested)
            {
                return;
            }

            shootRequested = false;
            ProcessShot();
        }

        public void Shoot()
        {
            if (ShootingGalleryGameManager.Instance != null &&
                !ShootingGalleryGameManager.Instance.IsGameplayRunning)
            {
                return;
            }

            if (revolverCylinder == null)
            {
                return;
            }

            if (revolverCylinder.IsOpen)
            {
                if (revolverCylinder.LoadedCount > 0)
                {
                    revolverCylinder.CloseCylinder();
                }
                else
                {
                    PlayEmptySound();
                }

                return;
            }

            shootRequested = true;
        }

        [ContextMenu("Debug Shoot")]
        public void DebugShoot()
        {
            Shoot();
        }

        private void ProcessShot()
        {
            if (revolverCylinder == null)
            {
                if (enableDebugLogs)
                {
                    Debug.LogWarning("RevolverShooter -> RevolverCylinder es null", this);
                }

                return;
            }

            bool consumedRound = revolverCylinder.TryConsumeRoundForShot();

            if (enableDebugLogs)
            {
                Debug.Log("RevolverShooter -> TryConsumeRoundForShot(): " + consumedRound, this);
            }

            if (!consumedRound)
            {
                PlayEmptySound();
                return;
            }

            PlayShootSound();
            PerformRaycast();
            ApplyRecoil();
        }

        private void PerformRaycast()
        {
            if (barrelTip == null)
            {
                if (enableDebugLogs)
                {
                    Debug.LogWarning("RevolverShooter -> BarrelTip es null", this);
                }

                return;
            }

            Vector3 origin = barrelTip.position;
            Vector3 direction = barrelTip.forward;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
            {
                if (IsOwnCollider(hit.collider))
                {
                    if (enableDebugLogs)
                    {
                        Debug.LogWarning("El raycast esta golpeando el propio revolver");
                    }

                    Vector3 fallbackEnd = origin + direction * range;
                    shotTracer?.ShowTracer(origin, fallbackEnd);
                    Debug.DrawRay(origin, direction * range, Color.yellow, 1f);
                    return;
                }

                shotTracer?.ShowTracer(origin, hit.point);
                Debug.DrawRay(origin, hit.point - origin, Color.green, 1f);

                if (hit.rigidbody != null)
                {
                    hit.rigidbody.AddForceAtPosition(direction * hitPower, hit.point, ForceMode.Impulse);
                }

                TryHandleTargetHit(hit.collider);
            }
            else
            {
                Vector3 endPoint = origin + direction * range;
                shotTracer?.ShowTracer(origin, endPoint);
                Debug.DrawRay(origin, direction * range, Color.red, 1f);
            }
        }

        private void TryHandleTargetHit(Collider hitCollider)
        {
            if (hitCollider == null)
            {
                return;
            }

            RailTargetHitReaction railTarget = hitCollider.GetComponent<RailTargetHitReaction>();
            if (railTarget == null)
            {
                railTarget = hitCollider.GetComponentInParent<RailTargetHitReaction>();
            }

            if (railTarget != null)
            {
                railTarget.HitTarget();
                return;
            }

            BanditTarget banditTarget = hitCollider.GetComponent<BanditTarget>();
            if (banditTarget == null)
            {
                banditTarget = hitCollider.GetComponentInParent<BanditTarget>();
            }

            if (banditTarget != null)
            {
                banditTarget.HitBandit();
            }
        }

        private bool IsOwnCollider(Collider candidate)
        {
            if (candidate == null || ownColliders == null)
            {
                return false;
            }

            for (int i = 0; i < ownColliders.Length; i++)
            {
                if (ownColliders[i] == candidate)
                {
                    return true;
                }
            }

            return false;
        }

        private void ApplyRecoil()
        {
            if (revolverBody == null || barrelTip == null)
            {
                return;
            }

            revolverBody.AddForceAtPosition(barrelTip.up * recoilPower, barrelTip.position, ForceMode.Impulse);
        }

        private void PlayShootSound()
        {
            if (shootAudioSource == null || shootAudioSource.clip == null)
            {
                return;
            }

            shootAudioSource.PlayOneShot(shootAudioSource.clip);
        }

        private void PlayEmptySound()
        {
            if (emptyAudioSource == null || emptyAudioSource.clip == null)
            {
                return;
            }

            emptyAudioSource.PlayOneShot(emptyAudioSource.clip);
        }
    }
}