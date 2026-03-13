using UnityEngine;

namespace Entiland_VR_DAM_AJEI_2026_G_1
{
    /// <summary>
    /// Identifica una bala física válida para la recarga del revólver
    /// Se marca como consumida cuando entra en el tambor
    /// </summary>
    public class RevolverAmmoRound : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] private bool isConsumed = false;

        /// <summary>
        /// Indica si esta bala ya fue usada por el sistema de recarga
        /// </summary>
        public bool IsConsumed
        {
            get { return isConsumed; }
        }

        /// <summary>
        /// Marca la bala como consumida y la desactiva
        /// </summary>
        public void Consume()
        {
            if (isConsumed)
            {
                return;
            }

            isConsumed = true;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Reinicia la bala
        /// </summary>
        public void ResetRound()
        {
            isConsumed = false;
            gameObject.SetActive(true);
        }
    }
}