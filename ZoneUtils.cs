using UnityEngine;

namespace ZAMERT
{
    internal static class ZoneUtils
    {
        private const float Tolerance = 0.04f;
        private const float PlayerHeight = 1.7f;

        public static bool IsInside(Collider collider, Vector3 worldPoint)
        {
            if (collider == null) return false;

            if (IsPointInside(collider, worldPoint))
                return true;

            return IsPointInside(collider, worldPoint + Vector3.up * PlayerHeight);
        }

        private static bool IsPointInside(Collider collider, Vector3 worldPoint)
        {
            Vector3 closest = collider.ClosestPoint(worldPoint);
            return (closest - worldPoint).sqrMagnitude <= Tolerance;
        }
    }
}
