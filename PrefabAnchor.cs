using System.Collections.Generic;
using UnityEngine;

namespace ZAMERT
{
    public class PrefabAnchor : ZAMERTInteractable
    {
        public new PFADTO Base { get; set; }

        public GameObject SpawnedInstance { get; private set; }

        protected void Start()
        {
            Base = base.Base as PFADTO;

            if (!ZAMERTPlugin.Singleton.PrefabAnchors.Contains(this))
                ZAMERTPlugin.Singleton.PrefabAnchors.Add(this);

            if (Base.SpawnOnStart)
                SpawnPrefabNow();

            LuaScriptService.ExecuteEvent(this, LuaEventType.Spawned.ToString().ToLowerInvariant(), new LuaExecutionContext
            {
                Detail = Base.PrefabName,
            });
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            ZAMERTPlugin.Singleton?.PrefabAnchors?.Remove(this);
        }

        public GameObject SpawnPrefabNow()
        {
            if (!Active || Base == null || string.IsNullOrWhiteSpace(Base.PrefabName))
                return null;

            SpawnedInstance = ZamertPrefabRegistry.SpawnPrefab(Base.PrefabName, transform.position, transform.rotation, Base.MatchScale ? transform.localScale : Vector3.one);

            if (SpawnedInstance == null)
            {
                ZAMERTLogger.Warn("PrefabAnchor failed to spawn prefab: " + Base.PrefabName);
                return null;
            }

            if (Base.SpawnAsChild)
                SpawnedInstance.transform.SetParent(transform, true);

            if (Base.DisableAnchorRenderers)
            {
                foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
                    renderer.enabled = false;
            }

            if (Base.DestroyAnchorAfterSpawn)
                Destroy(gameObject, 0.1f);

            return SpawnedInstance;
        }
    }
}
