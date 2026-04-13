using UnityEngine;

namespace EntilandVR.DosCuatro.DAM_AJEI.G_Uno
{
    /// <summary>
    /// Controla el rail, velocidad y config de los bandidos segun el nivel.
    /// </summary>
    public class LaneController : MonoBehaviour
    {
        [Header("Rail Paths")]
        [SerializeField] private RailLoopPath[] railPaths;

        [Header("Bandits")]
        [SerializeField] private BanditTarget[] banditTargets;

        [Header("Rail Difficulty")]
        [SerializeField] private float baseRailSpeed = 1.5f;
        [SerializeField] private float railSpeedPerLevel = 0.25f;

        [Header("Bandit Respawn Difficulty")]
        [SerializeField] private float baseBanditRespawnDelay = 2.0f;
        [SerializeField] private float banditRespawnDelayReductionPerLevel = 0.15f;
        [SerializeField] private float minimumBanditRespawnDelay = 0.5f;

        [Header("Bandit Shot Difficulty")]
        [SerializeField] private float baseBanditShotDelay = 3.0f;
        [SerializeField] private float banditShotDelayReductionPerLevel = 0.2f;
        [SerializeField] private float minimumBanditShotDelay = 0.75f;

        /// <summary>
        /// Aplica la config de dificultad segun el nivel.
        /// </summary>
        public void ApplyLevel(int level)
        {
            int clampedLevel = Mathf.Max(0, level);

            float appliedRailSpeed = baseRailSpeed + railSpeedPerLevel * clampedLevel;

            float appliedRespawnDelay =
                baseBanditRespawnDelay - banditRespawnDelayReductionPerLevel * clampedLevel;

            appliedRespawnDelay = Mathf.Max(minimumBanditRespawnDelay, appliedRespawnDelay);

            float appliedShotDelay =
                baseBanditShotDelay - banditShotDelayReductionPerLevel * clampedLevel;

            appliedShotDelay = Mathf.Max(minimumBanditShotDelay, appliedShotDelay);

            ApplyRailSpeed(appliedRailSpeed);
            ApplyBanditDelays(appliedRespawnDelay, appliedShotDelay);
        }

        private void ApplyRailSpeed(float newSpeed)
        {
            if (railPaths == null)
            {
                return;
            }

            for (int i = 0; i < railPaths.Length; i++)
            {
                RailLoopPath currentRail = railPaths[i];
                if (currentRail != null)
                {
                    currentRail.SetPathSpeed(newSpeed);
                }
            }
        }

        private void ApplyBanditDelays(float respawnDelay, float shotDelay)
        {
            if (banditTargets == null)
            {
                return;
            }

            for (int i = 0; i < banditTargets.Length; i++)
            {
                BanditTarget currentBandit = banditTargets[i];
                if (currentBandit != null)
                {
                    currentBandit.SetRespawnDelay(respawnDelay);
                    currentBandit.SetTimeBeforePlayerShot(shotDelay);
                }
            }
        }
    }
}