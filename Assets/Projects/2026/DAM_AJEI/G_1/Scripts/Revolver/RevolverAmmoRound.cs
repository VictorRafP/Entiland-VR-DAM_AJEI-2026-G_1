using UnityEngine;

namespace EntilandVR.DosCuatro.DAM_AJEI.G_Uno
{
    /// <summary>
    /// Gestiona el estado y el tipo de las balas físicas del revólver
    /// </summary>
    public class RevolverAmmoRound : MonoBehaviour
    {
        public enum AmmoType
        {
            Normal,
            Explosive,
            Triple
        }

        [Header("State")]
        [SerializeField] private bool isConsumed = false;
        [SerializeField] private AmmoType currentAmmoType = AmmoType.Normal;

        [Header("Visuals")]
        [SerializeField] private Renderer[] ammoRenderers;
        [SerializeField] private Material normalAmmoMaterial;
        [SerializeField] private Material explosiveAmmoMaterial;
        [SerializeField] private Material tripleAmmoMaterial;

        public bool IsConsumed
        {
            get { return isConsumed; }
        }

        public AmmoType CurrentAmmoType
        {
            get { return currentAmmoType; }
        }

        private void Awake()
        {
            if (ammoRenderers == null || ammoRenderers.Length == 0)
            {
                ammoRenderers = GetComponentsInChildren<Renderer>(true);
            }

            ApplyAmmoTypeVisual(currentAmmoType);
        }

        public void Consume()
        {
            if (isConsumed)
            {
                return;
            }

            isConsumed = true;
            gameObject.SetActive(false);
        }

        public void ResetRound()
        {
            isConsumed = false;
            gameObject.SetActive(true);
        }

        public void ConfigureAmmoType(AmmoType ammoType)
        {
            currentAmmoType = ammoType;
            ApplyAmmoTypeVisual(currentAmmoType);
        }

        private void ApplyAmmoTypeVisual(AmmoType ammoType)
        {
            if (ammoRenderers == null)
            {
                return;
            }

            Material targetMaterial = GetMaterialForAmmoType(ammoType);
            if (targetMaterial == null)
            {
                return;
            }

            for (int i = 0; i < ammoRenderers.Length; i++)
            {
                Renderer currentRenderer = ammoRenderers[i];
                if (currentRenderer == null)
                {
                    continue;
                }

                currentRenderer.material = targetMaterial;
            }
        }

        private Material GetMaterialForAmmoType(AmmoType ammoType)
        {
            switch (ammoType)
            {
                case AmmoType.Explosive:
                    return explosiveAmmoMaterial;

                case AmmoType.Triple:
                    return tripleAmmoMaterial;

                default:
                    return normalAmmoMaterial;
            }
        }
    }
}