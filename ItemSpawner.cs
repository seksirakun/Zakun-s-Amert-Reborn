using InventorySystem.Items.Pickups;
using LabApi.Features.Wrappers;
using MEC;
using System.Collections.Generic;
using UnityEngine;
using Log = ZAMERT.ZAMERTLogger;

namespace ZAMERT
{
    public class ItemSpawner : ZAMERTInteractable
    {
        public new ISDTO Base { get; set; }

        private readonly List<ItemPickupBase> _spawnedBases = new List<ItemPickupBase>();
        private CoroutineHandle _watcherHandle;

        protected void Start()
        {
            Base = base.Base as ISDTO;
            Log.Debug("Registering ItemSpawner: " + gameObject.name + " (" + OSchematic.Name + ")");

            if (!ZAMERTPlugin.Singleton.ItemSpawners.Contains(this))
                ZAMERTPlugin.Singleton.ItemSpawners.Add(this);

            if (Base.AutoStart)
                Timing.CallDelayed(Base.SpawnDelay > 0f ? Base.SpawnDelay : 0.5f, () => SpawnItems());
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Stop();
            ZAMERTPlugin.Singleton?.ItemSpawners?.Remove(this);
        }

        public void SpawnItems()
        {
            if (Base.ItemType == ItemType.None) return;

            _spawnedBases.Clear();
            Vector3 worldPos = transform.TransformPoint(Base.LocalPosition);
            int count = Mathf.Max(1, Base.Count);

            for (int i = 0; i < count; i++)
            {
                Pickup pickup = Pickup.Create(Base.ItemType, worldPos);
                pickup.Spawn();
                if (pickup.Base != null)
                    _spawnedBases.Add(pickup.Base);
            }

            Log.Debug("ItemSpawner: spawned " + count + "x " + Base.ItemType + " on " + gameObject.name);

            if (Base.RespawnTime > 0f)
            {
                if (_watcherHandle.IsRunning)
                    Timing.KillCoroutines(_watcherHandle);
                _watcherHandle = Timing.RunCoroutine(RespawnWatcher());
            }
        }

        public void Stop()
        {
            if (_watcherHandle.IsRunning)
                Timing.KillCoroutines(_watcherHandle);
            _spawnedBases.Clear();
        }

        public void Reset()
        {
            Stop();
            SpawnItems();
        }

        private IEnumerator<float> RespawnWatcher()
        {

            while (true)
            {
                _spawnedBases.RemoveAll(b => b == null);
                if (_spawnedBases.Count == 0) break;
                yield return Timing.WaitForSeconds(0.5f);
            }

            yield return Timing.WaitForSeconds(Base.RespawnTime);
            if (this != null)
                SpawnItems();
        }
    }
}
