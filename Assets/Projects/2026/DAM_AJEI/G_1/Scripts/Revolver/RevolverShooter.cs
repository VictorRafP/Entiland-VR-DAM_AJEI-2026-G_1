using UnityEngine;

namespace EntilandVR.DosCuatro.DAM_AJEI.G_Uno
{
    /// <summary>
    /// Gestiona el disparo del revólver y el comportamiento según el tipo de bala.
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

        [Header("Triple Shot")]
        [SerializeField] private float tripleSpreadAngle = 12f;

        [Header("Explosive Shot")]
        [SerializeField] private float explosionRadius = 2f;
        [SerializeField] private float explosionForce = 12f;
        [SerializeField] private ParticleSystem explosionImpactParticles;

        [Header("Muzzle Particles")]
        [SerializeField] private ParticleSystem normalMuzzleParticles;
        [SerializeField] private ParticleSystem explosiveMuzzleParticles;
        [SerializeField] private ParticleSystem tripleMuzzleParticles;

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
                else if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX(AudioManager.SFX_Sounds.NO_BULLETS);
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

            RevolverAmmoRound.AmmoType ammoType;
            bool consumedRound = revolverCylinder.TryConsumeRoundForShot(out ammoType);

            if (!consumedRound)
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX(AudioManager.SFX_Sounds.NO_BULLETS);
                }

                return;
            }

            switch (ammoType)
            {
                case RevolverAmmoRound.AmmoType.Explosive:
                    ShootExplosive();
                    break;

                case RevolverAmmoRound.AmmoType.Triple:
                    ShootTriple();
                    break;

                default:
                    ShootNormal();
                    break;
            }

            ApplyRecoil();
        }

        private void ShootNormal()
        {
            Vector3 origin = barrelTip != null ? barrelTip.position : transform.position;
            Vector3 direction = barrelTip != null ? barrelTip.forward : transform.forward;
            Vector3 endPoint = origin + direction * range;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.SFX_Sounds.SHOT);
            }

            if (normalMuzzleParticles != null)
            {
                normalMuzzleParticles.Play();
            }

            RaycastHit hit;
            if (TryGetValidHit(origin, direction, out hit))
            {
                endPoint = hit.point;
                ApplyPhysicsImpact(hit, direction);

                if (TryHandleTargetHit(hit.collider) && AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX(AudioManager.SFX_Sounds.WOOD_IMPACT);
                }
            }

            if (shotTracer != null)
            {
                shotTracer.ShowTracer(origin, endPoint);
            }
        }

        private void ShootTriple()
        {
            Vector3 origin = barrelTip != null ? barrelTip.position : transform.position;
            Vector3 centerDirection = barrelTip != null ? barrelTip.forward : transform.forward;
            Vector3 upAxis = barrelTip != null ? barrelTip.up : transform.up;

            Vector3 leftDirection = Quaternion.AngleAxis(-tripleSpreadAngle, upAxis) * centerDirection;
            Vector3 rightDirection = Quaternion.AngleAxis(tripleSpreadAngle, upAxis) * centerDirection;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.SFX_Sounds.SHOT);
                AudioManager.Instance.PlaySFX(AudioManager.SFX_Sounds.SHOT);
                AudioManager.Instance.PlaySFX(AudioManager.SFX_Sounds.SHOT);
            }

            if (tripleMuzzleParticles != null)
            {
                tripleMuzzleParticles.Play();
            }

            Vector3 centerEnd = FireSingleRay(origin, centerDirection);
            Vector3 leftEnd = FireSingleRay(origin, leftDirection);
            Vector3 rightEnd = FireSingleRay(origin, rightDirection);

            if (shotTracer != null)
            {
                shotTracer.ShowTripleTracer(origin, centerEnd, leftEnd, rightEnd);
            }
        }

        private void ShootExplosive()
        {
            Vector3 origin = barrelTip != null ? barrelTip.position : transform.position;
            Vector3 direction = barrelTip != null ? barrelTip.forward : transform.forward;
            Vector3 endPoint = origin + direction * range;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.SFX_Sounds.SHOT);
            }

            if (explosiveMuzzleParticles != null)
            {
                explosiveMuzzleParticles.Play();
            }

            RaycastHit hit;
            if (TryGetValidHit(origin, direction, out hit))
            {
                endPoint = hit.point;
                ApplyPhysicsImpact(hit, direction);
                ExplodeAtPoint(hit.point);
            }

            if (shotTracer != null)
            {
                shotTracer.ShowTracer(origin, endPoint);
            }
        }

        private Vector3 FireSingleRay(Vector3 origin, Vector3 direction)
        {
            Vector3 endPoint = origin + direction * range;

            RaycastHit hit;
            if (TryGetValidHit(origin, direction, out hit))
            {
                endPoint = hit.point;
                ApplyPhysicsImpact(hit, direction);

                if (TryHandleTargetHit(hit.collider) && AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX(AudioManager.SFX_Sounds.WOOD_IMPACT);
                }
            }

            return endPoint;
        }

        private void ExplodeAtPoint(Vector3 point)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.SFX_Sounds.EXPLOSION);
            }

            if (explosionImpactParticles != null)
            {
                explosionImpactParticles.transform.position = point;
                explosionImpactParticles.Play();
            }

            Collider[] hitColliders = Physics.OverlapSphere(point, explosionRadius, hitMask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitColliders.Length; i++)
            {
                Collider currentCollider = hitColliders[i];
                if (currentCollider == null || IsOwnCollider(currentCollider))
                {
                    continue;
                }

                Rigidbody hitBody = currentCollider.attachedRigidbody;
                if (hitBody != null)
                {
                    hitBody.AddExplosionForce(explosionForce, point, explosionRadius, 0f, ForceMode.Impulse);
                }

                TryHandleTargetHit(currentCollider);
            }
        }

        private bool TryGetValidHit(Vector3 origin, Vector3 direction, out RaycastHit validHit)
        {
            validHit = new RaycastHit();

            RaycastHit[] hits = Physics.RaycastAll(origin, direction, range, hitMask, QueryTriggerInteraction.Ignore);
            float closestDistance = float.MaxValue;
            bool foundValidHit = false;

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider == null)
                {
                    continue;
                }

                if (IsOwnCollider(hits[i].collider))
                {
                    continue;
                }

                if (hits[i].distance < closestDistance)
                {
                    closestDistance = hits[i].distance;
                    validHit = hits[i];
                    foundValidHit = true;
                }
            }

            return foundValidHit;
        }

        private bool TryHandleTargetHit(Collider hitCollider)
        {
            if (hitCollider == null)
            {
                return false;
            }

            RailTargetHitReaction railTarget = hitCollider.GetComponent<RailTargetHitReaction>();
            if (railTarget == null)
            {
                railTarget = hitCollider.GetComponentInParent<RailTargetHitReaction>();
            }

            if (railTarget != null)
            {
                railTarget.HitTarget();
                return true;
            }

            BanditTarget banditTarget = hitCollider.GetComponent<BanditTarget>();
            if (banditTarget == null)
            {
                banditTarget = hitCollider.GetComponentInParent<BanditTarget>();
            }

            if (banditTarget != null)
            {
                banditTarget.HitBandit();
                return true;
            }

            return false;
        }

        private void ApplyPhysicsImpact(RaycastHit hit, Vector3 direction)
        {
            if (hit.rigidbody != null)
            {
                hit.rigidbody.AddForceAtPosition(direction * hitPower, hit.point, ForceMode.Impulse);
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

        private void OnDrawGizmosSelected()
        {
            if (barrelTip == null)
            {
                return;
            }

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(barrelTip.position + barrelTip.forward * 2f, explosionRadius);
        }
    }
}