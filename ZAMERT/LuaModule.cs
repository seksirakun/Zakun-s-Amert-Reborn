using LabApi.Features.Wrappers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZAMERT
{
    [Serializable]
    public class LMDTO : ZAMERTDTO
    {
        public string InlineScript { get; set; }
        public string ScriptPath { get; set; }
        public int InputKeyCode { get; set; }
        public float InteractionMaxRange { get; set; } = 5f;
        public bool ListenEnter { get; set; }
        public bool ListenStay { get; set; }
        public bool ListenExit { get; set; }
        public float TickInterval { get; set; }
    }

    public class LuaModule : ZAMERTInteractable
    {
        private static readonly Dictionary<int, List<LuaModule>> ByKey = new Dictionary<int, List<LuaModule>>();

        public new LMDTO Base { get; set; }
        internal LuaExecutionContext Context;
        internal MoonSharp.Interpreter.Script Script;

        private readonly Dictionary<int, float> _stayAt = new Dictionary<int, float>();
        private float _tickAt;

        public static bool HasKey(int key)
        {
            List<LuaModule> list;
            return ByKey.TryGetValue(key, out list) && list != null && list.Count > 0;
        }

        public static void TryInteract(Player player, int key, Vector3 origin, Vector3 direction)
        {
            List<LuaModule> list;
            if (player == null || !ByKey.TryGetValue(key, out list) || list == null)
                return;

            RaycastHit[] hits = Physics.RaycastAll(origin, direction, 1000f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            if (hits != null && hits.Length > 0)
            {
                Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].collider == null)
                        continue;

                    LuaModule[] modules = hits[i].collider.GetComponentsInParent<LuaModule>();
                    for (int m = 0; m < modules.Length; m++)
                    {
                        LuaModule module = modules[m];
                        if (module == null || !module.Active || module.Base == null || module.Base.InputKeyCode != key)
                            continue;
                        if (hits[i].distance > module.Range)
                            continue;

                        module.Fire("on_interact", player);
                        return;
                    }

                    if (!hits[i].collider.isTrigger)
                        break;
                }
            }

            LuaModule best = null;
            float bestScore = float.MaxValue;
            for (int i = 0; i < list.Count; i++)
            {
                LuaModule module = list[i];
                if (module == null || !module.Active)
                    continue;

                float range = module.Range;
                Vector3 to = module.transform.position - origin;
                float distance = to.magnitude;
                if (distance > range)
                    continue;

                float angle = distance <= 0.01f ? 0f : Vector3.Angle(direction, to);
                if (angle > 35f)
                    continue;

                float score = angle * 8f + distance;
                if (score >= bestScore)
                    continue;

                bestScore = score;
                best = module;
            }

            if (best != null)
                best.Fire("on_interact", player);
        }

        public float Range
        {
            get { return Base != null && Base.InteractionMaxRange > 0f ? Base.InteractionMaxRange : 5f; }
        }

        protected void Start()
        {
            Base = base.Base as LMDTO;
            if (Base == null)
                return;

            Context = new LuaExecutionContext
            {
                Interactable = this,
                EventName = "spawn",
            };

            string fullPath;
            string source = LuaScriptService.ReadModuleSource(this, Base.InlineScript, Base.ScriptPath, out fullPath);
            if (string.IsNullOrWhiteSpace(source))
            {
                ZAMERTLogger.Warn("Lua module on " + gameObject.name + " has no script.");
                return;
            }

            PrepareTriggerBody();
            RegisterKey();

            try
            {
                LuaRuntime.Begin(this, Context, source, fullPath);
            }
            catch (Exception ex)
            {
                ZAMERTLogger.Error("Lua module failed to start on " + gameObject.name + ": " + ex.Message);
            }
        }

        protected void Update()
        {
            if (Base == null || Script == null || Base.TickInterval < 0.1f)
                return;
            if (Time.time < _tickAt)
                return;

            _tickAt = Time.time + Base.TickInterval;
            Fire("on_tick", null);
        }

        protected void OnTriggerEnter(Collider other)
        {
            if (Base == null || !Base.ListenEnter)
                return;

            Player player = PlayerFromCollider(other);
            if (player != null)
                Fire("on_enter", player);
        }

        protected void OnTriggerStay(Collider other)
        {
            if (Base == null || !Base.ListenStay || other == null)
                return;

            int id = other.GetInstanceID();
            float last;
            if (_stayAt.TryGetValue(id, out last) && Time.time - last < 0.25f)
                return;

            _stayAt[id] = Time.time;
            Player player = PlayerFromCollider(other);
            if (player != null)
                Fire("on_stay", player);
        }

        protected void OnTriggerExit(Collider other)
        {
            if (Base == null || !Base.ListenExit)
                return;

            if (other != null)
                _stayAt.Remove(other.GetInstanceID());

            Player player = PlayerFromCollider(other);
            if (player != null)
                Fire("on_exit", player);
        }

        protected override void OnDestroy()
        {
            UnregisterKey();
            LuaRuntime.Kill(this);
            base.OnDestroy();
        }

        internal void Fire(string functionName, Player player)
        {
            LuaRuntime.Call(this, functionName, player);
        }

        private void RegisterKey()
        {
            if (Base.InputKeyCode == 0)
                return;

            List<LuaModule> list;
            if (!ByKey.TryGetValue(Base.InputKeyCode, out list))
            {
                list = new List<LuaModule>();
                ByKey[Base.InputKeyCode] = list;
                ServerSettings.RegisterKeybind((KeyCode)Base.InputKeyCode);
            }

            if (!list.Contains(this))
                list.Add(this);
        }

        private void UnregisterKey()
        {
            if (Base == null || Base.InputKeyCode == 0)
                return;

            List<LuaModule> list;
            if (!ByKey.TryGetValue(Base.InputKeyCode, out list) || list == null)
                return;

            list.Remove(this);
            if (list.Count == 0)
                ByKey.Remove(Base.InputKeyCode);
        }

        private void PrepareTriggerBody()
        {
            if (!Base.ListenEnter && !Base.ListenStay && !Base.ListenExit)
                return;

            Collider collider = GetComponent<Collider>();
            if (collider == null || !collider.isTrigger || GetComponent<Rigidbody>() != null)
                return;

            Rigidbody body = gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
        }

        private static Player PlayerFromCollider(Collider other)
        {
            if (other == null)
                return null;

            ReferenceHub hub = other.GetComponentInParent<ReferenceHub>();
            return hub != null ? Player.Get(hub) : null;
        }
    }
}
