using UnityEngine;

namespace EntilandVR.DosCuatro.DAM_AJEI.G_Uno
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