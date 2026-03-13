using UnityEngine;

namespace Entiland_VR_DAM_AJEI_2026_G_1
{
    /// <summary>
    /// Revólver simple estilo demo:
    /// Auto Hand debe llamar directamente a Shoot().
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class RevolverShooter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Rigidbody revolverBody;
        [SerializeField] private RevolverCylinder revolverCylinder;
        [SerializeField] private Transform barrelTip;

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

        /// <summary>
        /// Método público que debe llamar Auto Hand al pulsar el gatillo/uso.
        /// </summary>
        public void Shoot()
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

        [ContextMenu("Debug Shoot")]
        public void DebugShoot()
        {
            Shoot();
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

            RaycastHit[] hits = Physics.RaycastAll(
                barrelTip.position,
                barrelTip.forward,
                range,
                hitMask,
                QueryTriggerInteraction.Ignore);

            if (hits == null || hits.Length == 0)
            {
                Debug.DrawRay(barrelTip.position, barrelTip.forward * range, Color.red, 1f);
                return;
            }

            int validHitIndex = GetClosestValidHitIndex(hits);
            if (validHitIndex < 0)
            {
                Debug.DrawRay(barrelTip.position, barrelTip.forward * range, Color.yellow, 1f);
                return;
            }

            RaycastHit hit = hits[validHitIndex];
            Debug.DrawRay(barrelTip.position, hit.point - barrelTip.position, Color.green, 1f);

            if (hit.rigidbody != null)
            {
                Vector3 shotDirection = barrelTip.forward;
                hit.rigidbody.AddForceAtPosition(shotDirection * hitPower, hit.point, ForceMode.Impulse);
            }

            // Si luego quieres, aquí conectamos puntuación/vidas.
        }

        private int GetClosestValidHitIndex(RaycastHit[] hits)
        {
            int closestIndex = -1;
            float closestDistanceSqr = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider hitCollider = hits[i].collider;
                if (hitCollider == null)
                {
                    continue;
                }

                if (IsOwnCollider(hitCollider))
                {
                    continue;
                }

                Vector3 delta = hits[i].point - barrelTip.position;
                float distanceSqr = delta.sqrMagnitude;

                if (distanceSqr < closestDistanceSqr)
                {
                    closestDistanceSqr = distanceSqr;
                    closestIndex = i;
                }
            }

            return closestIndex;
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