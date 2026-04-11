using UnityEngine;

namespace Entiland_VR_DAM_AJEI_2026_G_1
{
    /// <summary>
    /// Hit para el player, lo llamamos con el script de los bandidos al disparar
    /// </summary>
    public class PlayerHitReceiver : MonoBehaviour
    {
        [SerializeField] private int defaultDamage = 1;

        public void ReceiveBanditHit()
        {
            ReceiveBanditHit(defaultDamage);
        }

        public void ReceiveBanditHit(int damage)
        {
            if (ShootingGalleryGameManager.Instance == null)
            {
                return;
            }

            ShootingGalleryGameManager.Instance.DamagePlayer(damage);
        }
    }
}