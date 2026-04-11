using UnityEngine;

namespace Entiland_VR_DAM_AJEI_2026_G_1
{
    /// <summary>
    /// Informa al GameManager de puntos y cambio de vidas, y controla las reacciones de los hits en las dianas
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
        [SerializeField] private AudioSource hitAudioSource;
        [SerializeField] private AudioClip hitSound;

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

        private float originalLocalX = 0f;
        private float originalLocalY = 0f;
        private float originalLocalZ = 0f;

        private float reactionStartZ = 0f;
        private float reactionTargetZ = 0f;
        private float recoverStartZ = 0f;
        private float recoverTargetZ = 0f;

        private Material[][] originalMaterials;
        private Color[][] originalColors;

        private bool isBroken = false;

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
            RestoreOriginalStateImmediate();
        }

        private void Update()
        {
            if (currentState == TargetState.Reacting)
            {
                UpdateReacting();
                return;
            }

            if (currentState == TargetState.Holding)
            {
                UpdateHolding();
                return;
            }

            if (currentState == TargetState.Recovering)
            {
                UpdateRecovering();
                return;
            }

            if (currentState == TargetState.Idle && isBroken && recoverFromBrokenAfterDelay)
            {
                brokenTimer -= Time.deltaTime;
                if (brokenTimer <= 0f)
                {
                    StartRecoveringFromBroken();
                }
            }
        }

        public void HitTarget()
        {
            if (currentState != TargetState.Idle)
            {
                return;
            }

            if (isBroken)
            {
                return;
            }

            if (ShootingGalleryGameManager.Instance != null)
            {
                ShootingGalleryGameManager.Instance.RegisterTargetHit(scoreOnHit, lifeDeltaOnHit);
            }

            PlayHitSound();
            ApplyHitVisuals();

            reactionStartZ = originalLocalZ;
            reactionTargetZ = GetReactionTargetZ();

            currentState = TargetState.Reacting;
            stateTimer = 0f;
        }

        public void ResetTarget()
        {
            currentState = TargetState.Idle;
            stateTimer = 0f;
            brokenTimer = 0f;
            isBroken = false;
            RestoreOriginalStateImmediate();
        }

        [ContextMenu("Debug Hit Target")]
        public void DebugHitTarget()
        {
            HitTarget();
        }

        [ContextMenu("Debug Reset Target")]
        public void DebugResetTarget()
        {
            ResetTarget();
        }

        private void UpdateReacting()
        {
            stateTimer += Time.deltaTime;

            float duration = Mathf.Max(0.0001f, reactDuration);
            float t = Mathf.Clamp01(stateTimer / duration);

            float currentZ = Mathf.Lerp(reactionStartZ, reactionTargetZ, t);
            SetMeshLocalRotation(originalLocalX, originalLocalY, currentZ);

            if (t >= 1f)
            {
                SetMeshLocalRotation(originalLocalX, originalLocalY, reactionTargetZ);
                currentState = TargetState.Holding;
                stateTimer = 0f;
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
                ApplyBrokenVisuals();
                SetMeshLocalRotation(originalLocalX, originalLocalY, originalLocalZ + hitLocalZ);

                isBroken = true;
                currentState = TargetState.Idle;

                if (recoverFromBrokenAfterDelay)
                {
                    brokenTimer = Mathf.Max(0f, brokenRecoverDelay);
                }

                return;
            }

            RestoreOriginalMaterials();

            recoverStartZ = originalLocalZ + hitLocalZ;
            recoverTargetZ = originalLocalZ;

            currentState = TargetState.Recovering;
            stateTimer = 0f;
        }

        private void UpdateRecovering()
        {
            stateTimer += Time.deltaTime;

            float duration = Mathf.Max(0.0001f, recoverDuration);
            float t = Mathf.Clamp01(stateTimer / duration);

            float currentZ = Mathf.Lerp(recoverStartZ, recoverTargetZ, t);

            SetMeshLocalRotation(originalLocalX, originalLocalY, currentZ);
            LerpColorsBackToOriginal(t);

            if (t >= 1f)
            {
                RestoreOriginalStateImmediate();
                currentState = TargetState.Idle;
                stateTimer = 0f;
                brokenTimer = 0f;
                isBroken = false;
            }
        }

        private void StartRecoveringFromBroken()
        {
            RestoreOriginalMaterials();

            recoverStartZ = originalLocalZ + hitLocalZ;
            recoverTargetZ = originalLocalZ;

            currentState = TargetState.Recovering;
            stateTimer = 0f;
            brokenTimer = 0f;
            isBroken = false;
        }

        private float GetReactionTargetZ()
        {
            float directionalFinalOffset = GetDirectionalFinalOffset(hitLocalZ);
            float spinOffset = spinTurns * 360f * GetDirectionSign();

            return originalLocalZ + spinOffset + directionalFinalOffset;
        }

        private float GetDirectionalFinalOffset(float finalOffset)
        {
            if (clockwise)
            {
                if (finalOffset > 0f)
                {
                    return finalOffset - 360f;
                }

                return finalOffset;
            }

            if (finalOffset < 0f)
            {
                return finalOffset + 360f;
            }

            return finalOffset;
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
                    Material currentMaterial = rendererMaterials[materialIndex];
                    originalColors[rendererIndex][materialIndex] = GetMaterialColor(currentMaterial);
                }
            }
        }

        private void ApplyHitVisuals()
        {
            ApplyVisualSet(hitColor, hitOverrideMaterial);
        }

        private void ApplyBrokenVisuals()
        {
            ApplyVisualSet(brokenColor, brokenOverrideMaterial);
        }

        private void ApplyVisualSet(Color targetColor, Material overrideMaterial)
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
                Material[] replacementMaterials = new Material[currentMaterials.Length];

                for (int materialIndex = 0; materialIndex < currentMaterials.Length; materialIndex++)
                {
                    Material baseMaterial = currentMaterials[materialIndex];

                    if (overrideMaterial != null)
                    {
                        replacementMaterials[materialIndex] = overrideMaterial;
                    }
                    else
                    {
                        replacementMaterials[materialIndex] = baseMaterial;
                    }
                }

                currentRenderer.materials = replacementMaterials;

                Material[] activeMaterials = currentRenderer.materials;
                for (int materialIndex = 0; materialIndex < activeMaterials.Length; materialIndex++)
                {
                    SetMaterialColor(activeMaterials[materialIndex], targetColor);
                }
            }
        }

        private void RestoreOriginalMaterials()
        {
            if (targetRenderers == null || originalMaterials == null)
            {
                return;
            }

            int rendererCount = Mathf.Min(targetRenderers.Length, originalMaterials.Length);

            for (int rendererIndex = 0; rendererIndex < rendererCount; rendererIndex++)
            {
                Renderer currentRenderer = targetRenderers[rendererIndex];
                Material[] cachedMaterials = originalMaterials[rendererIndex];

                if (currentRenderer == null || cachedMaterials == null)
                {
                    continue;
                }

                currentRenderer.materials = cachedMaterials;
            }
        }

        private void LerpColorsBackToOriginal(float t)
        {
            if (targetRenderers == null || originalColors == null)
            {
                return;
            }

            int rendererCount = Mathf.Min(targetRenderers.Length, originalColors.Length);

            for (int rendererIndex = 0; rendererIndex < rendererCount; rendererIndex++)
            {
                Renderer currentRenderer = targetRenderers[rendererIndex];
                Color[] cachedColors = originalColors[rendererIndex];

                if (currentRenderer == null || cachedColors == null)
                {
                    continue;
                }

                Material[] currentMaterials = currentRenderer.materials;
                int materialCount = Mathf.Min(currentMaterials.Length, cachedColors.Length);

                for (int materialIndex = 0; materialIndex < materialCount; materialIndex++)
                {
                    Material currentMaterial = currentMaterials[materialIndex];
                    Color originalColor = cachedColors[materialIndex];
                    Color currentColor = GetMaterialColor(currentMaterial);
                    Color lerpedColor = Color.Lerp(currentColor, originalColor, t);

                    SetMaterialColor(currentMaterial, lerpedColor);
                }
            }
        }

        private void RestoreOriginalStateImmediate()
        {
            RestoreOriginalMaterials();

            if (targetRenderers != null && originalColors != null)
            {
                int rendererCount = Mathf.Min(targetRenderers.Length, originalColors.Length);

                for (int rendererIndex = 0; rendererIndex < rendererCount; rendererIndex++)
                {
                    Renderer currentRenderer = targetRenderers[rendererIndex];
                    Color[] cachedColors = originalColors[rendererIndex];

                    if (currentRenderer == null || cachedColors == null)
                    {
                        continue;
                    }

                    Material[] currentMaterials = currentRenderer.materials;
                    int materialCount = Mathf.Min(currentMaterials.Length, cachedColors.Length);

                    for (int materialIndex = 0; materialIndex < materialCount; materialIndex++)
                    {
                        SetMaterialColor(currentMaterials[materialIndex], cachedColors[materialIndex]);
                    }
                }
            }

            SetMeshLocalRotation(originalLocalX, originalLocalY, originalLocalZ);
        }

        private void PlayHitSound()
        {
            if (hitAudioSource == null || hitSound == null)
            {
                return;
            }

            hitAudioSource.PlayOneShot(hitSound);
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