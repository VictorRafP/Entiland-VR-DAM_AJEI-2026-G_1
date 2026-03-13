using UnityEngine;

namespace Entiland_VR_DAM_AJEI_2026_G_1
{
    /// <summary>
    /// Trigger de recarga que reenvía al tambor las balas que entran en la zona de trigger
    /// </summary>
    public class RevolverReloadTrigger : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RevolverCylinder revolverCylinder;

        private void Awake()
        {
            if (revolverCylinder == null)
            {
                revolverCylinder = GetComponentInParent<RevolverCylinder>();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (revolverCylinder == null)
            {
                return;
            }

            revolverCylinder.TryInsertRoundFromTrigger(other);
        }
    }
}