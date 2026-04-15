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

            if (Base.AutoStart)
                StartTicking();
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
                _hubs.Add(hub);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent<ReferenceHub>(out ReferenceHub hub))
                _hubs.Remove(hub);
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

                if (!Active || Base.DamageAmount <= 0f) continue;

                foreach (ReferenceHub hub in _hubs.ToList())
                {
                    if (hub == null) { _hubs.Remove(hub); continue; }

                    Player player = Player.Get(hub);
                    if (player == null || !player.IsAlive) { _hubs.Remove(hub); continue; }

                    float current = player.Health;
                    if (current <= Base.MinimumHealth) continue;

                    float dmg = Base.MinimumHealth <= 0f
                        ? Base.DamageAmount
                        : Mathf.Min(Base.DamageAmount, current - Base.MinimumHealth);

                    hub.playerStats.DealDamage(new CustomReasonDamageHandler("DamageTrigger", dmg));
                }
            }
        }
    }
}
