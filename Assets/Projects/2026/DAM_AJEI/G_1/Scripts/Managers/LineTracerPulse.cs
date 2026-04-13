using UnityEngine;

namespace EntilandVR.DosCuatro.DAM_AJEI.G_Uno
{
    /// <summary>
    /// Muestra las lineas de disparo
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class LineTracerPulse : MonoBehaviour
    {
        [SerializeField] private float visibleTime = 0.05f;

        private LineRenderer lineRenderer;
        private float visibleTimer = 0f;

        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.enabled = false;
            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = true;
            lineRenderer.alignment = LineAlignment.View;
            lineRenderer.textureMode = LineTextureMode.Stretch;
        }

        private void Update()
        {
            if (!lineRenderer.enabled)
            {
                return;
            }

            visibleTimer -= Time.deltaTime;
            if (visibleTimer <= 0f)
            {
                lineRenderer.enabled = false;
            }
        }

        public void ShowTracer(Vector3 start, Vector3 end)
        {
            if (lineRenderer == null)
            {
                return;
            }

            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);
            lineRenderer.enabled = true;
            visibleTimer = visibleTime;
        }
    }
}