using System.Collections.Generic;
using UnityEngine;

namespace ZAMERT
{
    internal static class InteractionGate
    {
        public const float MinInterval = 0.2f;

        private static readonly Dictionary<int, float> LastInteractionByKey = new Dictionary<int, float>();

        public static bool TryPass(int key, float cooldown)
        {
            float effective = Mathf.Max(MinInterval, cooldown);
            float now = Time.time;

            if (LastInteractionByKey.TryGetValue(key, out float last))
            {
                // Components on the same object fire as one group within a single
                // frame (multi-component fan-out), so a same-frame pass is free.
                if (now == last)
                    return true;

                if (now - last < effective)
                    return false;
            }

            LastInteractionByKey[key] = now;
            return true;
        }

        public static void Clear()
        {
            LastInteractionByKey.Clear();
        }
    }
}
