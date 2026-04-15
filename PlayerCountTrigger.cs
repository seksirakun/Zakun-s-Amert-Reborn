using LabApi.Features.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Log = ZAMERT.ZAMERTLogger;

namespace ZAMERT
{
    public class PlayerCountTrigger : ZAMERTInteractable
    {
        public new PCTDTO Base { get; set; }

        protected readonly HashSet<ReferenceHub> _hubs = new HashSet<ReferenceHub>();
        protected int _lastCount;
        protected float _lastFireTime = -999f;

        private static readonly Dictionary<string, Func<object[], string>> Formatter = InteractableObject.Formatter;

        protected void Start()
        {
            Base = base.Base as PCTDTO;
            Log.Debug("Registering PlayerCountTrigger: " + gameObject.name + " (" + OSchematic.Name + ")");

            if (!ZAMERTPlugin.Singleton.PlayerCountTriggers.Contains(this))
                ZAMERTPlugin.Singleton.PlayerCountTriggers.Add(this);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            ZAMERTPlugin.Singleton?.PlayerCountTriggers?.Remove(this);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!Active) return;
            if (!other.TryGetComponent<ReferenceHub>(out ReferenceHub hub)) return;
            _hubs.Add(hub);
            CheckThreshold(hub);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.TryGetComponent<ReferenceHub>(out ReferenceHub hub)) return;
            _hubs.Remove(hub);
            if (Active) CheckThreshold(hub);
        }

        protected virtual void CheckThreshold(ReferenceHub triggerHub)
        {
            if (!Base.AutoStart) return;

            int count = _hubs.Count;
            bool wasAbove = _lastCount >= Base.TriggerThreshold;
            bool isAbove  = count    >= Base.TriggerThreshold;
            int prev = _lastCount;
            _lastCount = count;

            bool shouldFire = false;
            if (Base.TriggerMode.HasFlag(PlayerCountTriggerMode.OnReachThreshold)    && !wasAbove && isAbove)  shouldFire = true;
            if (Base.TriggerMode.HasFlag(PlayerCountTriggerMode.OnDropBelowThreshold) && wasAbove && !isAbove) shouldFire = true;
            if (!shouldFire) return;

            float now = UnityEngine.Time.time;
            if (now - _lastFireTime < Base.Cooldown) return;
            _lastFireTime = now;

            Player player = Player.Get(triggerHub);
            if (player == null) return;

            Log.Debug("PlayerCountTrigger: fired on " + gameObject.name + " (count " + prev + " -> " + count + ", threshold " + Base.TriggerThreshold + ")");
            FireActions(player);
        }

        protected virtual void FireActions(Player player)
        {
            ModuleGeneralArguments args = new ModuleGeneralArguments()
            {
                Interpolations = Formatter,
                InterpolationsList = new object[] { player },
                Player = player,
                Schematic = OSchematic,
                Transform = this.transform,
                TargetCalculated = false,
            };

            var executors = new Dictionary<IPActionType, Action>
            {
                { IPActionType.Disappear,          () => Destroy(this.gameObject, 0.1f) },
                { IPActionType.Explode,             () => ExplodeModule.Execute(Base.ExplodeModules, args) },
                { IPActionType.PlayAnimation,       () => AnimationDTO.Execute(Base.AnimationModules, args) },
                { IPActionType.Warhead,             () => AlphaWarhead(Base.warheadActionType) },
                { IPActionType.SendMessage,         () => MessageModule.Execute(Base.MessageModules, args) },
                { IPActionType.DropItems,           () => DropItem.Execute(Base.dropItems, args) },
                { IPActionType.SendCommand,         () => Commanding.Execute(Base.commandings, args) },
                { IPActionType.GiveEffect,          () => EffectGivingModule.Execute(Base.effectGivingModules, args) },
                { IPActionType.PlayAudio,           () => AudioModule.Execute(Base.AudioModules, args) },
                { IPActionType.CallGroovieNoise,    () => CGNModule.Execute(Base.GroovieNoiseToCall, args) },
                { IPActionType.CallFunction,        () => CFEModule.Execute(Base.FunctionToCall, args) },
                { IPActionType.ModifyPrimitive,     () => PrimitiveModifyModule.Execute(Base.PrimitiveModifyModules, args) },
                { IPActionType.ControlSpeaker,      () => LoopSpeakerControlModule.Execute(Base.LoopSpeakerModules, args) },
                { IPActionType.ControlItemSpawner,  () => ItemSpawnerControlModule.Execute(Base.ItemSpawnerModules, args) },
            };

            foreach (IPActionType type in Enum.GetValues(typeof(IPActionType)))
            {
                if (Base.ActionType.HasFlag(type) && executors.TryGetValue(type, out var execute))
                {
                    Log.Debug("- PCT: executing action: " + type);
                    execute();
                }
            }
        }
    }

    public class FPlayerCountTrigger : PlayerCountTrigger
    {
        public new FPCTDTO Base { get; set; }

        protected new void Start()
        {
            Base = ((ZAMERTInteractable)this).Base as FPCTDTO;
            Log.Debug("Registering FPlayerCountTrigger: " + gameObject.name + " (" + OSchematic.Name + ")");

            if (!ZAMERTPlugin.Singleton.PlayerCountTriggers.Contains(this))
                ZAMERTPlugin.Singleton.PlayerCountTriggers.Add(this);
        }

        protected override void CheckThreshold(ReferenceHub triggerHub)
        {
            if (!Base.AutoStart) return;

            int count = _hubs.Count;
            bool wasAbove = _lastCount >= Base.TriggerThreshold;
            bool isAbove  = count    >= Base.TriggerThreshold;
            int prev = _lastCount;
            _lastCount = count;

            bool shouldFire = false;
            if (Base.TriggerMode.HasFlag(PlayerCountTriggerMode.OnReachThreshold)    && !wasAbove && isAbove)  shouldFire = true;
            if (Base.TriggerMode.HasFlag(PlayerCountTriggerMode.OnDropBelowThreshold) && wasAbove && !isAbove) shouldFire = true;
            if (!shouldFire) return;

            float now = UnityEngine.Time.time;
            float cooldown = Base.Cooldown.GetValue(new FunctionArgument(this), 1f);
            if (now - _lastFireTime < cooldown) return;
            _lastFireTime = now;

            Player player = Player.Get(triggerHub);
            if (player == null) return;

            Log.Debug("FPlayerCountTrigger: fired on " + gameObject.name + " (count " + prev + " -> " + count + ")");
            FireActions(player);
        }

        protected override void FireActions(Player player)
        {
            FunctionArgument args = new FunctionArgument(this, player);

            var executors = new Dictionary<IPActionType, Action>
            {
                { IPActionType.Disappear,          () => Destroy(this.gameObject, 0.1f) },
                { IPActionType.Explode,             () => FExplodeModule.Execute(Base.ExplodeModules, args) },
                { IPActionType.PlayAnimation,       () => FAnimationDTO.Execute(Base.AnimationModules, args) },
                { IPActionType.Warhead,             () => AlphaWarhead(Base.warheadActionType.GetValue<WarheadActionType>(args, 0)) },
                { IPActionType.SendMessage,         () => FMessageModule.Execute(Base.MessageModules, args) },
                { IPActionType.DropItems,           () => FDropItem.Execute(Base.dropItems, args) },
                { IPActionType.SendCommand,         () => FCommanding.Execute(Base.commandings, args) },
                { IPActionType.GiveEffect,          () => FEffectGivingModule.Execute(Base.effectGivingModules, args) },
                { IPActionType.PlayAudio,           () => FAudioModule.Execute(Base.AudioModules, args) },
                { IPActionType.CallGroovieNoise,    () => FCGNModule.Execute(Base.GroovieNoiseToCall, args) },
                { IPActionType.CallFunction,        () => FCFEModule.Execute(Base.FunctionToCall, args) },
                { IPActionType.ModifyPrimitive,     () => FPrimitiveModifyModule.Execute(Base.PrimitiveModifyModules, args) },
                { IPActionType.ControlSpeaker,      () => FLoopSpeakerControlModule.Execute(Base.LoopSpeakerModules, args) },
                { IPActionType.ControlItemSpawner,  () => FItemSpawnerControlModule.Execute(Base.ItemSpawnerModules, args) },
            };

            foreach (IPActionType type in Enum.GetValues(typeof(IPActionType)))
            {
                if (Base.ActionType.HasFlag(type) && executors.TryGetValue(type, out var execute))
                    execute();
            }
        }
    }
}
