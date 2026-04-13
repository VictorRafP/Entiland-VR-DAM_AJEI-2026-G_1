using UnityEngine;

namespace EntilandVR.DosCuatro.DAM_AJEI.G_Uno
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