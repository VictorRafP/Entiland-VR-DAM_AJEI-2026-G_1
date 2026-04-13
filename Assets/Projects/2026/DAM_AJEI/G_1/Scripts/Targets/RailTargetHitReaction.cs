using UnityEngine;

namespace EntilandVR.DosCuatro.DAM_AJEI.G_Uno
{
    /// <summary>
    /// Controla la reacción visual y de puntuación de una diana al recibir un hit.
    /// </summary>
    public class RailTargetHitReaction : MonoBehaviour
    {
        private enum TargetState
        {
            Idle,
            Reacting,
            Holding,
            Recovering
        }

        [Header("References")]
        [SerializeField] private Transform meshTransform;
        [SerializeField] private Renderer[] targetRenderers;

        [Header("Gameplay")]
        [SerializeField] private int scoreOnHit = 100;
        [SerializeField] private int lifeDeltaOnHit = 0;

        [Header("Rotation")]
        [SerializeField] private int spinTurns = 2;
        [SerializeField] private float hitLocalZ = -90f;
        [SerializeField] private bool clockwise = true;
        [SerializeField] private float reactDuration = 0.25f;
        [SerializeField] private float holdDuration = 0.35f;
        [SerializeField] private float recoverDuration = 0.25f;

        [Header("Visual Hit")]
        [SerializeField] private Color hitColor = Color.red;
        [SerializeField] private Material hitOverrideMaterial;

        [Header("Broken State")]
        [SerializeField] private bool keepBrokenStateAfterHit = true;
        [SerializeField] private Color brokenColor = Color.gray;
        [SerializeField] private Material brokenOverrideMaterial;

        [Header("Optional Recovery")]
        [SerializeField] private bool recoverFromBrokenAfterDelay = false;
        [SerializeField] private float brokenRecoverDelay = 2f;

        private TargetState currentState = TargetState.Idle;
        private float stateTimer = 0f;
        private float brokenTimer = 0f;
        private bool isBroken = false;

        private float originalLocalX = 0f;
        private float originalLocalY = 0f;
        private float originalLocalZ = 0f;

        private float reactionStartZ = 0f;
        private float reactionTargetZ = 0f;
        private float recoverStartZ = 0f;
        private float recoverTargetZ = 0f;

        private Material[][] originalMaterials;
        private Color[][] originalColors;

        private void Awake()
        {
            if (meshTransform == null)
            {
                meshTransform = FindBestMeshTransform();
            }

            if ((targetRenderers == null || targetRenderers.Length == 0) && meshTransform != null)
            {
                targetRenderers = meshTransform.GetComponentsInChildren<Renderer>(true);
            }

            CacheOriginalTransform();
            CacheOriginalVisuals();
            RestoreOriginalVisuals();
            SetMeshLocalRotation(originalLocalX, originalLocalY, originalLocalZ);
        }

        private void Update()
        {
            switch (currentState)
            {
                case TargetState.Reacting:
                    UpdateReacting();
                    break;

                case TargetState.Holding:
                    UpdateHolding();
                    break;

                case TargetState.Recovering:
                    UpdateRecovering();
                    break;

                case TargetState.Idle:
                    UpdateIdleBrokenRecovery();
                    break;
            }
        }

        public void HitTarget()
        {
            if (currentState != TargetState.Idle || isBroken)
            {
                return;
            }

            if (ShootingGalleryGameManager.Instance != null)
            {
                ShootingGalleryGameManager.Instance.RegisterTargetHit(scoreOnHit, lifeDeltaOnHit);
            }

            PlayHitSound();
            ApplyVisuals(hitColor, hitOverrideMaterial);

            reactionStartZ = originalLocalZ;
            reactionTargetZ = GetReactionTargetZ();

            stateTimer = 0f;
            currentState = TargetState.Reacting;
        }

        public void ResetTarget()
        {
            currentState = TargetState.Idle;
            stateTimer = 0f;
            brokenTimer = 0f;
            isBroken = false;

            RestoreOriginalVisuals();
            SetMeshLocalRotation(originalLocalX, originalLocalY, originalLocalZ);
        }

        private void UpdateReacting()
        {
            stateTimer += Time.deltaTime;

            float t = Mathf.Clamp01(stateTimer / Mathf.Max(0.0001f, reactDuration));
            float currentZ = Mathf.Lerp(reactionStartZ, reactionTargetZ, t);

            SetMeshLocalRotation(originalLocalX, originalLocalY, currentZ);

            if (t >= 1f)
            {
                SetMeshLocalRotation(originalLocalX, originalLocalY, reactionTargetZ);
                stateTimer = 0f;
                currentState = TargetState.Holding;
            }
        }

        private void UpdateHolding()
        {
            stateTimer += Time.deltaTime;

            if (stateTimer < holdDuration)
            {
                return;
            }

            if (keepBrokenStateAfterHit)
            {
                ApplyVisuals(brokenColor, brokenOverrideMaterial);
                SetMeshLocalRotation(originalLocalX, originalLocalY, originalLocalZ + hitLocalZ);

                isBroken = true;
                stateTimer = 0f;
                currentState = TargetState.Idle;

                if (recoverFromBrokenAfterDelay)
                {
                    brokenTimer = Mathf.Max(0f, brokenRecoverDelay);
                }

                return;
            }

            StartRecovering();
        }

        private void UpdateRecovering()
        {
            stateTimer += Time.deltaTime;

            float t = Mathf.Clamp01(stateTimer / Mathf.Max(0.0001f, recoverDuration));
            float currentZ = Mathf.Lerp(recoverStartZ, recoverTargetZ, t);

            SetMeshLocalRotation(originalLocalX, originalLocalY, currentZ);

            if (t >= 1f)
            {
                RestoreOriginalVisuals();
                SetMeshLocalRotation(originalLocalX, originalLocalY, originalLocalZ);

                stateTimer = 0f;
                brokenTimer = 0f;
                isBroken = false;
                currentState = TargetState.Idle;
            }
        }

        private void UpdateIdleBrokenRecovery()
        {
            if (!isBroken || !recoverFromBrokenAfterDelay)
            {
                return;
            }

            brokenTimer -= Time.deltaTime;
            if (brokenTimer <= 0f)
            {
                StartRecovering();
            }
        }

        private void StartRecovering()
        {
            RestoreOriginalVisuals();

            recoverStartZ = meshTransform != null ? meshTransform.localEulerAngles.z : originalLocalZ + hitLocalZ;
            recoverTargetZ = originalLocalZ;

            stateTimer = 0f;
            brokenTimer = 0f;
            isBroken = false;
            currentState = TargetState.Recovering;
        }

        private float GetReactionTargetZ()
        {
            float finalOffset = GetDirectionalFinalOffset(hitLocalZ);
            float spinOffset = spinTurns * 360f * GetDirectionSign();
            return originalLocalZ + spinOffset + finalOffset;
        }

        private float GetDirectionalFinalOffset(float finalOffset)
        {
            if (clockwise)
            {
                return finalOffset > 0f ? finalOffset - 360f : finalOffset;
            }

            return finalOffset < 0f ? finalOffset + 360f : finalOffset;
        }

        private float GetDirectionSign()
        {
            return clockwise ? -1f : 1f;
        }

        private Transform FindBestMeshTransform()
        {
            Renderer[] allRenderers = GetComponentsInChildren<Renderer>(true);

            for (int i = 0; i < allRenderers.Length; i++)
            {
                if (allRenderers[i] != null && allRenderers[i].transform != transform)
                {
                    return allRenderers[i].transform;
                }
            }

            return transform;
        }

        private void CacheOriginalTransform()
        {
            if (meshTransform == null)
            {
                meshTransform = transform;
            }

            Vector3 localEuler = meshTransform.localEulerAngles;
            originalLocalX = localEuler.x;
            originalLocalY = localEuler.y;
            originalLocalZ = localEuler.z;
        }

        private void CacheOriginalVisuals()
        {
            int rendererCount = targetRenderers != null ? targetRenderers.Length : 0;

            originalMaterials = new Material[rendererCount][];
            originalColors = new Color[rendererCount][];

            for (int rendererIndex = 0; rendererIndex < rendererCount; rendererIndex++)
            {
                Renderer currentRenderer = targetRenderers[rendererIndex];
                if (currentRenderer == null)
                {
                    continue;
                }

                Material[] rendererMaterials = currentRenderer.materials;
                originalMaterials[rendererIndex] = rendererMaterials;
                originalColors[rendererIndex] = new Color[rendererMaterials.Length];

                for (int materialIndex = 0; materialIndex < rendererMaterials.Length; materialIndex++)
                {
                    originalColors[rendererIndex][materialIndex] = GetMaterialColor(rendererMaterials[materialIndex]);
                }
            }
        }

        private void ApplyVisuals(Color targetColor, Material overrideMaterial)
        {
            if (targetRenderers == null)
            {
                return;
            }

            for (int rendererIndex = 0; rendererIndex < targetRenderers.Length; rendererIndex++)
            {
                Renderer currentRenderer = targetRenderers[rendererIndex];
                if (currentRenderer == null)
                {
                    continue;
                }

                Material[] currentMaterials = currentRenderer.materials;

                if (overrideMaterial != null)
                {
                    for (int i = 0; i < currentMaterials.Length; i++)
                    {
                        currentMaterials[i] = overrideMaterial;
                    }

                    currentRenderer.materials = currentMaterials;
                    currentMaterials = currentRenderer.materials;
                }

                for (int materialIndex = 0; materialIndex < currentMaterials.Length; materialIndex++)
                {
                    SetMaterialColor(currentMaterials[materialIndex], targetColor);
                }
            }
        }

        private void RestoreOriginalVisuals()
        {
            if (targetRenderers == null || originalMaterials == null || originalColors == null)
            {
                return;
            }

            int rendererCount = Mathf.Min(targetRenderers.Length, originalMaterials.Length);

            for (int rendererIndex = 0; rendererIndex < rendererCount; rendererIndex++)
            {
                Renderer currentRenderer = targetRenderers[rendererIndex];
                Material[] cachedMaterials = originalMaterials[rendererIndex];
                Color[] cachedColors = originalColors[rendererIndex];

                if (currentRenderer == null || cachedMaterials == null || cachedColors == null)
                {
                    continue;
                }

                currentRenderer.materials = cachedMaterials;

                Material[] currentMaterials = currentRenderer.materials;
                int materialCount = Mathf.Min(currentMaterials.Length, cachedColors.Length);

                for (int materialIndex = 0; materialIndex < materialCount; materialIndex++)
                {
                    SetMaterialColor(currentMaterials[materialIndex], cachedColors[materialIndex]);
                }
            }
        }

        private void PlayHitSound()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.SFX_Sounds.WOOD_IMPACT);
            }
        }

        private Color GetMaterialColor(Material material)
        {
            if (material == null)
            {
                return Color.white;
            }

            if (material.HasProperty("_BaseColor"))
            {
                return material.GetColor("_BaseColor");
            }

            if (material.HasProperty("_Color"))
            {
                return material.GetColor("_Color");
            }

            return Color.white;
        }

        private void SetMaterialColor(Material material, Color color)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
        }

        private void SetMeshLocalRotation(float localX, float localY, float localZ)
        {
            if (meshTransform == null)
            {
                return;
            }

            meshTransform.localRotation = Quaternion.Euler(localX, localY, localZ);
        }
    }
}