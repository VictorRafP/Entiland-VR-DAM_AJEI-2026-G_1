using UnityEngine;

namespace EntilandVR.DosCuatro.DAM_AJEI.G_Uno
{
    public class FreezeParticleAtTime : MonoBehaviour
    {
        [SerializeField] private ParticleSystem targetParticles;
        [SerializeField] private float freezeTime = 0.03f;

        private void Start()
        {
            if (targetParticles == null)
            {
                return;
            }

            targetParticles.Simulate(freezeTime, true, true);
            targetParticles.Pause();
        }
    }
}