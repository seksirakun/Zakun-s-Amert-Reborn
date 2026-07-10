using System;
using UnityEngine;

namespace ZAMERT
{
    public enum AudioZoneShape
    {
        Sphere,
        Box,
    }

    public enum AudioZoneMode
    {
        SilenceInside,
        OnlyAudibleInside,
    }

    [Serializable]
    public class AudioZoneDef
    {
        public AudioZoneShape Shape { get; set; } = AudioZoneShape.Sphere;
        public AudioZoneMode Mode { get; set; } = AudioZoneMode.SilenceInside;
        public bool Attached { get; set; } = true;
        public SVector3 Center { get; set; } = new SVector3();
        public float Radius { get; set; } = 5f;
        public SVector3 HalfExtents { get; set; } = new SVector3 { x = 3f, y = 3f, z = 3f };

        public bool Contains(Vector3 worldCenter, Quaternion rotation, Vector3 worldPoint)
        {
            if (Shape == AudioZoneShape.Sphere)
            {
                float r = Mathf.Max(0f, Radius);
                return (worldPoint - worldCenter).sqrMagnitude <= r * r;
            }

            Vector3 local = Quaternion.Inverse(rotation) * (worldPoint - worldCenter);
            Vector3 he = HalfExtents;
            return Mathf.Abs(local.x) <= Mathf.Abs(he.x)
                && Mathf.Abs(local.y) <= Mathf.Abs(he.y)
                && Mathf.Abs(local.z) <= Mathf.Abs(he.z);
        }
    }
}
