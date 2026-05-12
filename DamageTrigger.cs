using LabApi.Features.Wrappers;
using MEC;
using PlayerStatsSystem;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Log = ZAMERT.ZAMERTLogger;

namespace ZAMERT
{
    public class DamageTrigger : ZAMERTInteractable
    {
        public new DTTDTO Base { get; set; }

        private readonly HashSet<ReferenceHub> _hubs = new HashSet<ReferenceHub>();
        private CoroutineHandle _tickHandle;

        protected void Start()
        {
            Base = base.Base as DTTDTO;
            Log.Debug("Registering DamageTrigger: " + gameObject.name + " (" + OSchematic.Name + ")");

            if (!ZAMERTPlugin.Singleton.DamageTriggers.Contains(this))
                ZAMERTPlugin.Singleton.DamageTriggers.Add(this);

            if (Base.AutoStart && Base.TriggerMode.HasFlag(DamageTriggerMode.OnStay))
                StartTicking();

            LuaScriptService.ExecuteEvent(this, LuaEventType.Spawned.ToString().ToLowerInvariant());
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            StopTicking();
            ZAMERTPlugin.Singleton?.DamageTriggers?.Remove(this);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!Active) return;
            if (other.TryGetComponent<ReferenceHub>(out ReferenceHub hub))
            {
                _hubs.Add(hub);
                if (Base.TriggerMode.HasFlag(DamageTriggerMode.OnEnter))
                    ApplyToHub(hub, LuaEventType.Entered.ToString().ToLowerInvariant());
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent<ReferenceHub>(out ReferenceHub hub))
            {
                if (Active && Base.TriggerMode.HasFlag(DamageTriggerMode.OnExit))
                    ApplyToHub(hub, LuaEventType.Exited.ToString().ToLowerInvariant());
                _hubs.Remove(hub);
            }
        }

        public void StartTicking()
        {
            if (_tickHandle.IsRunning) return;
            _tickHandle = Timing.RunCoroutine(DamageTick());
            Log.Debug("DamageTrigger: started on " + gameObject.name);
        }

        public void StopTicking()
        {
            if (_tickHandle.IsRunning)
                Timing.KillCoroutines(_tickHandle);
            _hubs.Clear();
            Log.Debug("DamageTrigger: stopped on " + gameObject.name);
        }

        private IEnumerator<float> DamageTick()
        {
            float interval = Mathf.Max(0.1f, Base.DamageInterval);
            while (true)
            {
                yield return Timing.WaitForSeconds(interval);

                if (!Active || !Base.TriggerMode.HasFlag(DamageTriggerMode.OnStay)) continue;

                foreach (ReferenceHub hub in _hubs.ToList())
                {
                    ApplyToHub(hub, LuaEventType.Tick.ToString().ToLowerInvariant(), removeDead: true);
                }
            }
        }

        private void ApplyToHub(ReferenceHub hub, string eventName, bool removeDead = false)
        {
            if (hub == null)
            {
                if (removeDead)
                    _hubs.Remove(hub);
                return;
            }

            Player player = Player.Get(hub);
            if (player == null || !player.IsAlive)
            {
                if (removeDead)
                    _hubs.Remove(hub);
                return;
            }

            if (!ShouldAffect(player))
                return;

            float current = player.Health;
            float predictedHealth = Base.KillInstant
                ? 0f
                : Base.MinimumHealth <= 0f
                    ? Mathf.Max(current - Base.DamageAmount, 0f)
                    : Mathf.Max(current - Mathf.Min(Base.DamageAmount, current - Base.MinimumHealth), Base.MinimumHealth);

            LuaExecutionContext luaContext = LuaScriptService.ExecuteEvent(this, eventName, new LuaExecutionContext
            {
                Player = player,
                PreviousHealth = current,
                CurrentHealth = predictedHealth,
                Damage = Base.KillInstant ? current : Base.DamageAmount,
            });

            if (luaContext != null && luaContext.Cancelled)
                return;

            if (Base.KillInstant)
            {
                hub.playerStats.DealDamage(new CustomReasonDamageHandler("ZAMERT KillPart", Mathf.Max(current + 500f, 9999f)));
                return;
            }

            if (Base.DamageAmount <= 0f || current <= Base.MinimumHealth)
                return;

            float damage = Base.MinimumHealth <= 0f
                ? Base.DamageAmount
                : Mathf.Min(Base.DamageAmount, current - Base.MinimumHealth);

            if (damage > 0f)
                hub.playerStats.DealDamage(new CustomReasonDamageHandler("ZAMERT KillPart", damage));
        }

        private bool ShouldAffect(Player player)
        {
            if (player == null)
                return false;

            if (player.IsSCP)
                return Base.AffectScps;

            if (player.IsHuman)
                return Base.AffectHumans;

            return Base.AffectHumans || Base.AffectScps;
        }
    }
}
