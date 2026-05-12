using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ZAMERT
{
    public static class ZamertPrefabRegistry
    {
        private static readonly Dictionary<string, GameObject> Prefabs = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyCollection<string> PrefabNames => Prefabs.Keys.ToList().AsReadOnly();

        public static void Refresh()
        {
            Prefabs.Clear();

            foreach (GameObject prefab in NetworkClient.prefabs.Values.ToArray())
            {
                if (prefab == null || string.IsNullOrWhiteSpace(prefab.name))
                    continue;

                if (!Prefabs.ContainsKey(prefab.name))
                    Prefabs[prefab.name] = prefab;
            }
        }

        public static bool TryGetPrefab(string prefabName, out GameObject prefab)
        {
            if (Prefabs.Count == 0 || !Prefabs.ContainsKey(prefabName))
                Refresh();

            return Prefabs.TryGetValue(prefabName, out prefab);
        }

        public static GameObject SpawnPrefab(string prefabName, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (!TryGetPrefab(prefabName, out GameObject prefab) || prefab == null)
                return null;

            GameObject instance = UnityEngine.Object.Instantiate(prefab, position, rotation);
            instance.transform.localScale = scale;

            if (instance.TryGetComponent(out NetworkIdentity identity))
            {
                if (!identity.isServer)
                    NetworkServer.Spawn(instance);
            }

            return instance;
        }
    }
}
