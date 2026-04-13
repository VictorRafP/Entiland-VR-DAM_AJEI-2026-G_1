using System.Collections.Generic;
using UnityEngine;

namespace EntilandVR.DosCuatro.DAM_AJEI.G_Uno
{
    /// <summary>
    /// Define el pathing que recorreran las dianas, la velocidad a la que se moveran y cuantas habran
    /// </summary>
    [ExecuteAlways]
    public class RailLoopPath : MonoBehaviour
    {
        [System.Serializable]
        public class ManagedTarget
        {
            public Transform targetRoot;
        }

        [Header("Segment Points")]
        [SerializeField] private Transform topA;
        [SerializeField] private Transform topB;
        [SerializeField] private Transform rightA;
        [SerializeField] private Transform rightB;
        [SerializeField] private Transform bottomA;
        [SerializeField] private Transform bottomB;
        [SerializeField] private Transform leftA;
        [SerializeField] private Transform leftB;

        [Header("Corner Handle Lengths")]
        [SerializeField] private float topRightHandleLength = 0.25f;
        [SerializeField] private float bottomRightHandleLength = 0.25f;
        [SerializeField] private float bottomLeftHandleLength = 0.25f;
        [SerializeField] private float topLeftHandleLength = 0.25f;

        [Header("Path Shape")]
        [SerializeField] private float insetAmount = 0f;

        [Header("Sampling")]
        [SerializeField] private int straightSamplesPerSegment = 8;
        [SerializeField] private int curveSamplesPerCorner = 12;

        [Header("Movement")]
        [SerializeField] private float speed = 1.5f;
        [SerializeField] private bool reverseDirection = false;

        [Header("Managed Targets")]
        [SerializeField] private List<ManagedTarget> managedTargets = new List<ManagedTarget>();

        [Header("Gizmos")]
        [SerializeField] private Color straightColor = Color.cyan;
        [SerializeField] private Color curveColor = Color.yellow;
        [SerializeField] private Color sampleColor = Color.white;
        [SerializeField] private bool drawSamples = false;
        [SerializeField] private float sampleSphereRadius = 0.02f;

        private Vector3[] sampledPoints;
        private float[] cumulativeDistances;
        private float totalLength = 0f;
        private float globalDistance = 0f;

        private void Awake()
        {
            RebuildPathCache();
            SnapManagedTargetsToRail();
        }

        private void OnValidate()
        {
            RebuildPathCache();
            SnapManagedTargetsToRail();
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (ShootingGalleryGameManager.Instance != null &&
                !ShootingGalleryGameManager.Instance.IsGameplayRunning)
            {
                return;
            }

            if (sampledPoints == null || sampledPoints.Length < 2 || totalLength <= 0f)
            {
                return;
            }

            if (managedTargets == null || managedTargets.Count == 0)
            {
                return;
            }

            float directionMultiplier = reverseDirection ? -1f : 1f;
            globalDistance += speed * directionMultiplier * Time.deltaTime;

            UpdateManagedTargets();
        }

        public void SetPathSpeed(float newSpeed)
        {
            speed = Mathf.Max(0f, newSpeed);
        }

        public void RebuildPathCache()
        {
            if (!HasValidSetup())
            {
                sampledPoints = null;
                cumulativeDistances = null;
                totalLength = 0f;
                return;
            }

            Vector3 topMid = (topA.position + topB.position) * 0.5f;
            Vector3 bottomMid = (bottomA.position + bottomB.position) * 0.5f;
            Vector3 leftMid = (leftA.position + leftB.position) * 0.5f;
            Vector3 rightMid = (rightA.position + rightB.position) * 0.5f;

            Vector3 topInsetDirection = GetDirection(topMid, bottomMid);
            Vector3 bottomInsetDirection = GetDirection(bottomMid, topMid);
            Vector3 leftInsetDirection = GetDirection(leftMid, rightMid);
            Vector3 rightInsetDirection = GetDirection(rightMid, leftMid);

            Vector3 adjustedTopA = topA.position + topInsetDirection * insetAmount;
            Vector3 adjustedTopB = topB.position + topInsetDirection * insetAmount;

            Vector3 adjustedRightA = rightA.position + rightInsetDirection * insetAmount;
            Vector3 adjustedRightB = rightB.position + rightInsetDirection * insetAmount;

            Vector3 adjustedBottomA = bottomA.position + bottomInsetDirection * insetAmount;
            Vector3 adjustedBottomB = bottomB.position + bottomInsetDirection * insetAmount;

            Vector3 adjustedLeftA = leftA.position + leftInsetDirection * insetAmount;
            Vector3 adjustedLeftB = leftB.position + leftInsetDirection * insetAmount;

            int straightSamples = Mathf.Max(2, straightSamplesPerSegment);
            int curveSamples = Mathf.Max(2, curveSamplesPerCorner);

            int totalSampleCount =
                straightSamples +
                curveSamples +
                straightSamples +
                curveSamples +
                straightSamples +
                curveSamples +
                straightSamples +
                curveSamples;

            Vector3[] tempPoints = new Vector3[totalSampleCount];
            int writeIndex = 0;

            AppendStraightSamples(tempPoints, ref writeIndex, adjustedTopA, adjustedTopB, straightSamples);
            AppendCurveSamples(
                tempPoints,
                ref writeIndex,
                adjustedTopB,
                adjustedRightA,
                GetDirection(adjustedTopA, adjustedTopB),
                GetDirection(adjustedRightA, adjustedRightB),
                topRightHandleLength,
                curveSamples);

            AppendStraightSamples(tempPoints, ref writeIndex, adjustedRightA, adjustedRightB, straightSamples);
            AppendCurveSamples(
                tempPoints,
                ref writeIndex,
                adjustedRightB,
                adjustedBottomA,
                GetDirection(adjustedRightA, adjustedRightB),
                GetDirection(adjustedBottomA, adjustedBottomB),
                bottomRightHandleLength,
                curveSamples);

            AppendStraightSamples(tempPoints, ref writeIndex, adjustedBottomA, adjustedBottomB, straightSamples);
            AppendCurveSamples(
                tempPoints,
                ref writeIndex,
                adjustedBottomB,
                adjustedLeftA,
                GetDirection(adjustedBottomA, adjustedBottomB),
                GetDirection(adjustedLeftA, adjustedLeftB),
                bottomLeftHandleLength,
                curveSamples);

            AppendStraightSamples(tempPoints, ref writeIndex, adjustedLeftA, adjustedLeftB, straightSamples);
            AppendCurveSamples(
                tempPoints,
                ref writeIndex,
                adjustedLeftB,
                adjustedTopA,
                GetDirection(adjustedLeftA, adjustedLeftB),
                GetDirection(adjustedTopA, adjustedTopB),
                topLeftHandleLength,
                curveSamples);

            sampledPoints = tempPoints;
            BuildDistanceCache();
        }

        private void SnapManagedTargetsToRail()
        {
            if (sampledPoints == null || sampledPoints.Length < 2 || totalLength <= 0f)
            {
                return;
            }

            if (managedTargets == null || managedTargets.Count == 0)
            {
                return;
            }

            UpdateManagedTargets();
        }

        private void UpdateManagedTargets()
        {
            int validTargetCount = GetValidTargetCount();
            if (validTargetCount <= 0)
            {
                return;
            }

            float spacing = totalLength / validTargetCount;
            int validIndex = 0;

            for (int i = 0; i < managedTargets.Count; i++)
            {
                ManagedTarget target = managedTargets[i];
                if (target == null || target.targetRoot == null)
                {
                    continue;
                }

                float targetDistance = globalDistance + spacing * validIndex;

                EvaluateAtDistance(targetDistance, out Vector3 railPosition);
                target.targetRoot.position = railPosition;

                validIndex++;
            }
        }

        private int GetValidTargetCount()
        {
            if (managedTargets == null)
            {
                return 0;
            }

            int count = 0;

            for (int i = 0; i < managedTargets.Count; i++)
            {
                ManagedTarget target = managedTargets[i];
                if (target != null && target.targetRoot != null)
                {
                    count++;
                }
            }

            return count;
        }

        public void EvaluateAtDistance(float distance, out Vector3 position)
        {
            position = Vector3.zero;

            if (sampledPoints == null || sampledPoints.Length < 2 || cumulativeDistances == null || totalLength <= 0f)
            {
                return;
            }

            float wrappedDistance = Mathf.Repeat(distance, totalLength);

            int lastIndex = cumulativeDistances.Length - 1;
            int segmentIndex = lastIndex;

            for (int i = 0; i < lastIndex; i++)
            {
                if (wrappedDistance <= cumulativeDistances[i + 1])
                {
                    segmentIndex = i;
                    break;
                }
            }

            int nextIndex = segmentIndex + 1;
            if (nextIndex >= sampledPoints.Length)
            {
                nextIndex = 0;
            }

            float startDistance = cumulativeDistances[segmentIndex];
            float endDistance = nextIndex == 0 ? totalLength : cumulativeDistances[nextIndex];

            float segmentLength = endDistance - startDistance;
            float t = 0f;

            if (segmentLength > 0.0001f)
            {
                t = (wrappedDistance - startDistance) / segmentLength;
            }

            Vector3 startPoint = sampledPoints[segmentIndex];
            Vector3 endPoint = sampledPoints[nextIndex];

            position = Vector3.Lerp(startPoint, endPoint, t);
        }

        private void BuildDistanceCache()
        {
            if (sampledPoints == null || sampledPoints.Length < 2)
            {
                cumulativeDistances = null;
                totalLength = 0f;
                return;
            }

            cumulativeDistances = new float[sampledPoints.Length];
            cumulativeDistances[0] = 0f;

            float accumulated = 0f;

            for (int i = 1; i < sampledPoints.Length; i++)
            {
                accumulated += Vector3.Distance(sampledPoints[i - 1], sampledPoints[i]);
                cumulativeDistances[i] = accumulated;
            }

            accumulated += Vector3.Distance(sampledPoints[sampledPoints.Length - 1], sampledPoints[0]);
            totalLength = accumulated;
        }

        private bool HasValidSetup()
        {
            return
                topA != null &&
                topB != null &&
                rightA != null &&
                rightB != null &&
                bottomA != null &&
                bottomB != null &&
                leftA != null &&
                leftB != null;
        }

        private void AppendStraightSamples(
            Vector3[] buffer,
            ref int writeIndex,
            Vector3 start,
            Vector3 end,
            int sampleCount)
        {
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                buffer[writeIndex] = Vector3.Lerp(start, end, t);
                writeIndex++;
            }
        }

        private void AppendCurveSamples(
            Vector3[] buffer,
            ref int writeIndex,
            Vector3 start,
            Vector3 end,
            Vector3 startDirection,
            Vector3 endDirection,
            float handleLength,
            int sampleCount)
        {
            Vector3 control1 = start + startDirection * handleLength;
            Vector3 control2 = end - endDirection * handleLength;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                buffer[writeIndex] = EvaluateCubicBezier(start, control1, control2, end, t);
                writeIndex++;
            }
        }

        private Vector3 GetDirection(Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            if (delta.sqrMagnitude <= 0.000001f)
            {
                return Vector3.forward;
            }

            return delta.normalized;
        }

        private Vector3 EvaluateCubicBezier(
            Vector3 p0,
            Vector3 p1,
            Vector3 p2,
            Vector3 p3,
            float t)
        {
            float oneMinusT = 1f - t;

            float a = oneMinusT * oneMinusT * oneMinusT;
            float b = 3f * oneMinusT * oneMinusT * t;
            float c = 3f * oneMinusT * t * t;
            float d = t * t * t;

            return
                a * p0 +
                b * p1 +
                c * p2 +
                d * p3;
        }

        private void OnDrawGizmos()
        {
            if (!HasValidSetup())
            {
                return;
            }

            Vector3 topMid = (topA.position + topB.position) * 0.5f;
            Vector3 bottomMid = (bottomA.position + bottomB.position) * 0.5f;
            Vector3 leftMid = (leftA.position + leftB.position) * 0.5f;
            Vector3 rightMid = (rightA.position + rightB.position) * 0.5f;

            Vector3 topInsetDirection = GetDirection(topMid, bottomMid);
            Vector3 bottomInsetDirection = GetDirection(bottomMid, topMid);
            Vector3 leftInsetDirection = GetDirection(leftMid, rightMid);
            Vector3 rightInsetDirection = GetDirection(rightMid, leftMid);

            Vector3 adjustedTopA = topA.position + topInsetDirection * insetAmount;
            Vector3 adjustedTopB = topB.position + topInsetDirection * insetAmount;

            Vector3 adjustedRightA = rightA.position + rightInsetDirection * insetAmount;
            Vector3 adjustedRightB = rightB.position + rightInsetDirection * insetAmount;

            Vector3 adjustedBottomA = bottomA.position + bottomInsetDirection * insetAmount;
            Vector3 adjustedBottomB = bottomB.position + bottomInsetDirection * insetAmount;

            Vector3 adjustedLeftA = leftA.position + leftInsetDirection * insetAmount;
            Vector3 adjustedLeftB = leftB.position + leftInsetDirection * insetAmount;

            DrawStraight(adjustedTopA, adjustedTopB);
            DrawCurve(
                adjustedTopB,
                adjustedRightA,
                GetDirection(adjustedTopA, adjustedTopB),
                GetDirection(adjustedRightA, adjustedRightB),
                topRightHandleLength);

            DrawStraight(adjustedRightA, adjustedRightB);
            DrawCurve(
                adjustedRightB,
                adjustedBottomA,
                GetDirection(adjustedRightA, adjustedRightB),
                GetDirection(adjustedBottomA, adjustedBottomB),
                bottomRightHandleLength);

            DrawStraight(adjustedBottomA, adjustedBottomB);
            DrawCurve(
                adjustedBottomB,
                adjustedLeftA,
                GetDirection(adjustedBottomA, adjustedBottomB),
                GetDirection(adjustedLeftA, adjustedLeftB),
                bottomLeftHandleLength);

            DrawStraight(adjustedLeftA, adjustedLeftB);
            DrawCurve(
                adjustedLeftB,
                adjustedTopA,
                GetDirection(adjustedLeftA, adjustedLeftB),
                GetDirection(adjustedTopA, adjustedTopB),
                topLeftHandleLength);

            if (drawSamples && sampledPoints != null)
            {
                Gizmos.color = sampleColor;

                for (int i = 0; i < sampledPoints.Length; i++)
                {
                    Gizmos.DrawSphere(sampledPoints[i], sampleSphereRadius);
                }
            }
        }

        private void DrawStraight(Vector3 start, Vector3 end)
        {
            Gizmos.color = straightColor;
            Gizmos.DrawLine(start, end);
        }

        private void DrawCurve(
            Vector3 start,
            Vector3 end,
            Vector3 startDirection,
            Vector3 endDirection,
            float handleLength)
        {
            Gizmos.color = curveColor;

            Vector3 control1 = start + startDirection * handleLength;
            Vector3 control2 = end - endDirection * handleLength;

            int steps = Mathf.Max(4, curveSamplesPerCorner);
            Vector3 previous = start;

            for (int i = 1; i <= steps; i++)
            {
                float t = (float)i / steps;
                Vector3 current = EvaluateCubicBezier(start, control1, control2, end, t);
                Gizmos.DrawLine(previous, current);
                previous = current;
            }
        }
    }
}