using MapGeneration.RoomConnectors;
using Mirror;
using ProjectMER.Features.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ZAMERT
{
    public enum ClutterPrefabType
    {
        SimpleBoxes = 0,
        PipesShort = 1,
        BoxesLadder = 2,
        TankSupportedShelf = 3,
        AngledFences = 4,
        HugeOrangePipes = 5,
        PipesLongOpen = 6,
    }

    internal static class ClutterPrefabRegistry
    {
        private static readonly Dictionary<ClutterPrefabType, GameObject> _prefabs = new Dictionary<ClutterPrefabType, GameObject>();
        private static bool _registered;

        public static GameObject Get(ClutterPrefabType type)
        {
            EnsureRegistered();
            _prefabs.TryGetValue(type, out GameObject prefab);
            return prefab;
        }

        public static void EnsureRegistered()
        {
            if (_registered) return;
            _registered = true;

            int total = 0;
            foreach (GameObject value in NetworkClient.prefabs.Values.ToArray())
            {
                if (value == null || value.GetComponent<SpawnableClutterConnector>() == null) continue;

                switch (value.name)
                {
                    case "Simple Boxes Open Connector": _prefabs[ClutterPrefabType.SimpleBoxes] = value; total++; break;
                    case "Pipes Short Open Connector": _prefabs[ClutterPrefabType.PipesShort] = value; total++; break;
                    case "Boxes Ladder Open Connector": _prefabs[ClutterPrefabType.BoxesLadder] = value; total++; break;
                    case "Tank-Supported Shelf Open Connector": _prefabs[ClutterPrefabType.TankSupportedShelf] = value; total++; break;
                    case "Angled Fences Open Connector": _prefabs[ClutterPrefabType.AngledFences] = value; total++; break;
                    case "Huge Orange Pipes Open Connector": _prefabs[ClutterPrefabType.HugeOrangePipes] = value; total++; break;
                    case "Pipes Long Open Connector": _prefabs[ClutterPrefabType.PipesLongOpen] = value; total++; break;
                }
            }

            ZAMERTLogger.Info("[Clutter] Registered " + total + " built-in clutter prefabs");
        }

        public static void Reset() => _registered = false;
    }

    [Serializable]
    public class CLDTO : ZAMERTDTO
    {
        public ClutterPrefabType ClutterType = ClutterPrefabType.SimpleBoxes;
        public float SpawnChance = 100f;
    }

    public class Clutter : ZAMERTInteractable
    {
        public new CLDTO Base { get; set; }

        private GameObject _spawned;

        protected virtual void Start()
        {
            Base = base.Base as CLDTO;
            if (Base == null || !Active) return;

            ClutterPrefabRegistry.EnsureRegistered();

            if (UnityEngine.Random.Range(0f, 100f) > Base.SpawnChance)
                return;

            GameObject prefab = ClutterPrefabRegistry.Get(Base.ClutterType);
            if (prefab == null)
            {
                ZAMERTLogger.Warn("[Clutter] prefab '" + Base.ClutterType + "' not registered. Server might not be in a round.");
                return;
            }

            try
            {
                _spawned = UnityEngine.Object.Instantiate(prefab);
                NetworkServer.UnSpawn(_spawned);
                _spawned.transform.SetPositionAndRotation(transform.position, transform.rotation);
                NetworkServer.Spawn(_spawned);
            }
            catch (Exception ex)
            {
                ZAMERTLogger.Error("[Clutter] spawn failed: " + ex);
            }
        }

        protected override void OnDestroy()
        {
            try
            {
                if (_spawned != null) NetworkServer.Destroy(_spawned);
            }
            catch { }
            base.OnDestroy();
        }
    }
}
