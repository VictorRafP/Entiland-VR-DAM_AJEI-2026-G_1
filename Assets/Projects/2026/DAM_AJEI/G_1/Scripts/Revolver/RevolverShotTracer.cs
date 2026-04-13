using UnityEngine;

namespace EntilandVR.DosCuatro.DAM_AJEI.G_Uno
{
    /// <summary>
    /// Línea visual de disparo de la pistola.
    /// Usa LineRenderers asignados desde el inspector.
    /// </summary>
    public class RevolverShotTracer : MonoBehaviour
    {
        [Header("Line Renderers")]
        [SerializeField] private LineRenderer centerLine;
        [SerializeField] private LineRenderer leftLine;
        [SerializeField] private LineRenderer rightLine;

        [Header("Tracer")]
        [SerializeField] private float visibleTime = 0.03f;

        private float visibleTimer = 0f;

        private void Awake()
        {
            DisableAllLines();
        }

        private void Update()
        {
            if (!AnyLineVisible())
            {
                return;
            }

            visibleTimer -= Time.deltaTime;
            if (visibleTimer <= 0f)
            {
                DisableAllLines();
            }
        }

        public void ShowTracer(Vector3 start, Vector3 end)
        {
            DisableAllLines();

            if (centerLine != null)
            {
                centerLine.SetPosition(0, start);
                centerLine.SetPosition(1, end);
                centerLine.enabled = true;
            }

            visibleTimer = visibleTime;
        }

        public void ShowTripleTracer(Vector3 start, Vector3 centerEnd, Vector3 leftEnd, Vector3 rightEnd)
        {
            DisableAllLines();

            if (centerLine != null)
            {
                centerLine.SetPosition(0, start);
                centerLine.SetPosition(1, centerEnd);
                centerLine.enabled = true;
            }

            if (leftLine != null)
            {
                leftLine.SetPosition(0, start);
                leftLine.SetPosition(1, leftEnd);
                leftLine.enabled = true;
            }

            if (rightLine != null)
            {
                rightLine.SetPosition(0, start);
                rightLine.SetPosition(1, rightEnd);
                rightLine.enabled = true;
            }

            visibleTimer = visibleTime;
        }

        private void DisableAllLines()
        {
            if (centerLine != null)
            {
                centerLine.enabled = false;
            }

            if (leftLine != null)
            {
                leftLine.enabled = false;
            }

            if (rightLine != null)
            {
                rightLine.enabled = false;
            }
        }

        private bool AnyLineVisible()
        {
            if (centerLine != null && centerLine.enabled)
            {
                return true;
            }

            if (leftLine != null && leftLine.enabled)
            {
                return true;
            }

            if (rightLine != null && rightLine.enabled)
            {
                return true;
            }

            return false;
        }
    }
}