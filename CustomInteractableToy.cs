using AdminToys;
using Interactables.Interobjects.DoorUtils;
using LabApi.Features.Wrappers;
using MEC;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ZAMERT
{
    public class CustomInteractableToy : ZAMERTInteractable
    {
        public new CITDTO Base { get; set; }

        protected float _lastRunTime = -999f;

        private readonly List<KeyValuePair<InteractableToy, Action<Player>>> _boundToys =
            new List<KeyValuePair<InteractableToy, Action<Player>>>();

        protected virtual void Start()
        {
            Base = base.Base as CITDTO;
            Register();
            LuaScriptService.ExecuteEvent(this, LuaEventType.Spawned.ToString().ToLowerInvariant());
        }

        protected virtual void Register()
        {
            if (!ZAMERTPlugin.Singleton.CustomInteractableToys.Contains(this))
                ZAMERTPlugin.Singleton.CustomInteractableToys.Add(this);

            AdminToys.InvisibleInteractableToy rootBase = GetComponent<AdminToys.InvisibleInteractableToy>();
            if (rootBase != null && Base.IncludeRootPrimitive)
                BindToy(InteractableToy.Get(rootBase));

            if (!Base.IncludeChildPrimitives)
                return;

            foreach (AdminToys.InvisibleInteractableToy child in GetComponentsInChildren<AdminToys.InvisibleInteractableToy>())
            {
                if (child == rootBase)
                    continue;

                AdminToys.InvisibleInteractableToy cached = child;
                Timing.CallDelayed(0.5f, () => BindToy(InteractableToy.Get(cached)));
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            foreach (KeyValuePair<InteractableToy, Action<Player>> pair in _boundToys)
            {
                if (pair.Key != null)
                    pair.Key.OnInteracted -= pair.Value;
            }
            _boundToys.Clear();

            ZAMERTPlugin.Singleton?.CustomInteractableToys?.Remove(this);
        }

        protected void BindToy(InteractableToy toy)
        {
            if (toy == null)
                return;

            string toyId = toy.Base != null ? toy.Base.name : string.Empty;
            Action<Player> handler = player => RunToyInteractionGroup(player, toyId);
            toy.OnInteracted += handler;
            _boundToys.Add(new KeyValuePair<InteractableToy, Action<Player>>(toy, handler));
        }

        protected void RunToyInteractionGroup(Player player, string toyId)
        {
            foreach (CustomInteractableToy interactable in GetComponents<CustomInteractableToy>())
            {
                if (interactable == null)
                    continue;

                interactable.RunProcess(player, toyId);
            }
        }

        protected void ExecuteDenyActions(Player player, CITDTO dto)
        {
            ModuleGeneralArguments denyArgs = new ModuleGeneralArguments
            {
                Interpolations = InteractableObject.Formatter,
                InterpolationsList = new object[] { player },
                Player = player,
                Schematic = OSchematic,
                Transform = transform,
                TargetCalculated = false,
            };

            if (dto.DenyActionType.HasFlag(IPActionType.SendMessage)) MessageModule.Execute(dto.DenyMessageModules, denyArgs);
            if (dto.DenyActionType.HasFlag(IPActionType.PlayAudio)) AudioModule.Execute(dto.DenyAudioModules, denyArgs);
            if (dto.DenyActionType.HasFlag(IPActionType.CallGroovieNoise)) CGNModule.Execute(dto.DenyGroovieNoiseToCall, denyArgs);
            if (dto.DenyActionType.HasFlag(IPActionType.CallFunction)) CFEModule.Execute(dto.DenyFunctionToCall, denyArgs);
            if (dto.DenyActionType.HasFlag(IPActionType.PlayAnimation)) AnimationDTO.Execute(dto.DenyAnimationModules, denyArgs);
            if (dto.DenyActionType.HasFlag(IPActionType.Warhead)) AlphaWarhead(dto.DenyWarheadActionType);
            if (dto.DenyActionType.HasFlag(IPActionType.DropItems)) DropItem.Execute(dto.DenyDropItems, denyArgs);
            if (dto.DenyActionType.HasFlag(IPActionType.SendCommand)) Commanding.Execute(dto.DenyCommandings, denyArgs);
            if (dto.DenyActionType.HasFlag(IPActionType.Explode)) ExplodeModule.Execute(dto.DenyExplodeModules, denyArgs);
            if (dto.DenyActionType.HasFlag(IPActionType.GiveEffect)) EffectGivingModule.Execute(dto.DenyEffectGivingModules, denyArgs);
            if (dto.DenyActionType.HasFlag(IPActionType.ModifyPrimitive)) PrimitiveModifyModule.Execute(dto.DenyPrimitiveModifyModules, denyArgs);
            if (dto.DenyActionType.HasFlag(IPActionType.ControlSpeaker)) LoopSpeakerControlModule.Execute(dto.DenyLoopSpeakerModules, denyArgs);
            if (dto.DenyActionType.HasFlag(IPActionType.ControlItemSpawner)) ItemSpawnerControlModule.Execute(dto.DenyItemSpawnerModules, denyArgs);
        }

        protected void ExecuteFDenyActions(Player player, FCITDTO dto)
        {
            FunctionArgument denyArgs = new FunctionArgument(this, player);
            if (dto.DenyActionType.HasFlag(IPActionType.SendMessage)) FMessageModule.Execute(dto.DenyMessageModules, denyArgs);
            if (dto.DenyActionType.HasFlag(IPActionType.PlayAudio)) FAudioModule.Execute(dto.DenyAudioModules, denyArgs);
            if (dto.DenyActionType.HasFlag(IPActionType.CallGroovieNoise)) FCGNModule.Execute(dto.DenyGroovieNoiseToCall, denyArgs);
            if (dto.DenyActionType.HasFlag(IPActionType.CallFunction)) FCFEModule.Execute(dto.DenyFunctionToCall, denyArgs);
            if (dto.DenyActionType.HasFlag(IPActionType.PlayAnimation)) FAnimationDTO.Execute(dto.DenyAnimationModules, denyArgs);
            if (dto.DenyActionType.HasFlag(IPActionType.Warhead)) AlphaWarhead(dto.DenyWarheadActionType.GetValue(denyArgs, WarheadActionType.Start));
            if (dto.DenyActionType.HasFlag(IPActionType.DropItems)) FDropItem.Execute(dto.DenyDropItems, denyArgs);
            if (dto.DenyActionType.HasFlag(IPActionType.SendCommand)) FCommanding.Execute(dto.DenyCommandings, denyArgs);
            if (dto.DenyActionType.HasFlag(IPActionType.Explode)) FExplodeModule.Execute(dto.DenyExplodeModules, denyArgs);
            if (dto.DenyActionType.HasFlag(IPActionType.GiveEffect)) FEffectGivingModule.Execute(dto.DenyEffectGivingModules, denyArgs);
            if (dto.DenyActionType.HasFlag(IPActionType.ModifyPrimitive)) FPrimitiveModifyModule.Execute(dto.DenyPrimitiveModifyModules, denyArgs);
            if (dto.DenyActionType.HasFlag(IPActionType.ControlSpeaker)) FLoopSpeakerControlModule.Execute(dto.DenyLoopSpeakerModules, denyArgs);
            if (dto.DenyActionType.HasFlag(IPActionType.ControlItemSpawner)) FItemSpawnerControlModule.Execute(dto.DenyItemSpawnerModules, denyArgs);
        }

        public virtual void RunProcess(Player player, string toyId)
        {
            if (!Active || player == null)
                return;

            float now = Time.time;
            if (now - _lastRunTime < 0.2f)
                return;
            _lastRunTime = now;

            if (!InteractableObject.HasKeycardAccess(player, Base.KeycardPermissions, Base.RequireAllPermissions))
            {
                ExecuteDenyActions(player, Base);
                LuaScriptService.ExecuteEvent(this, LuaEventType.Denied.ToString().ToLowerInvariant(), new LuaExecutionContext
                {
                    Player = player,
                    ToyId = toyId,
                    Detail = "keycard_denied",
                });
                return;
            }

            ModuleGeneralArguments args = new ModuleGeneralArguments
            {
                Interpolations = InteractableObject.Formatter,
                InterpolationsList = new object[] { player },
                Player = player,
                Schematic = OSchematic,
                Transform = transform,
                TargetCalculated = false,
            };

            Dictionary<IPActionType, Action> executors = new Dictionary<IPActionType, Action>
            {
                { IPActionType.Disappear, () => Destroy(gameObject, 0.1f) },
                { IPActionType.Explode, () => ExplodeModule.Execute(Base.ExplodeModules, args) },
                { IPActionType.PlayAnimation, () => AnimationDTO.Execute(Base.AnimationModules, args) },
                { IPActionType.Warhead, () => AlphaWarhead(Base.warheadActionType) },
                { IPActionType.SendMessage, () => MessageModule.Execute(Base.MessageModules, args) },
                { IPActionType.DropItems, () => DropItem.Execute(Base.dropItems, args) },
                { IPActionType.SendCommand, () => Commanding.Execute(Base.commandings, args) },
                { IPActionType.GiveEffect, () => EffectGivingModule.Execute(Base.effectGivingModules, args) },
                { IPActionType.PlayAudio, () => AudioModule.Execute(Base.AudioModules, args) },
                { IPActionType.CallGroovieNoise, () => CGNModule.Execute(Base.GroovieNoiseToCall, args) },
                { IPActionType.CallFunction, () => CFEModule.Execute(Base.FunctionToCall, args) },
                { IPActionType.ModifyPrimitive, () => PrimitiveModifyModule.Execute(Base.PrimitiveModifyModules, args) },
                { IPActionType.ControlSpeaker, () => LoopSpeakerControlModule.Execute(Base.LoopSpeakerModules, args) },
                { IPActionType.ControlItemSpawner, () => ItemSpawnerControlModule.Execute(Base.ItemSpawnerModules, args) },
                {
                    IPActionType.UpgradeItem, () =>
                    {
                        if (!player.GameObject.TryGetComponent<Collider>(out Collider col))
                            return;

                        List<int> modes = Enumerable.Range(0, 5).Where(i => Base.Scp914Mode.HasFlag((Scp914Mode)i)).ToList();
                        if (modes.Count == 0)
                            return;

                        Scp914.Scp914Upgrader.Upgrade(new[] { col }, Scp914.Scp914Mode.Held, (Scp914.Scp914KnobSetting)modes.RandomItem());
                    }
                },
            };

            foreach (IPActionType type in Enum.GetValues(typeof(IPActionType)))
            {
                if (Base.ActionType.HasFlag(type) && executors.TryGetValue(type, out Action execute))
                    execute();
            }

            LuaScriptService.ExecuteEvent(this, LuaEventType.Interacted.ToString().ToLowerInvariant(), new LuaExecutionContext
            {
                Player = player,
                ToyId = toyId,
            });
        }
    }

    public class FCustomInteractableToy : CustomInteractableToy
    {
        public new FCITDTO Base { get; set; }

        protected override void Start()
        {
            Base = ((ZAMERTInteractable)this).Base as FCITDTO;
            Register();
            LuaScriptService.ExecuteEvent(this, LuaEventType.Spawned.ToString().ToLowerInvariant());
        }

        public override void RunProcess(Player player, string toyId)
        {
            if (!Active || player == null)
                return;

            float now = Time.time;
            if (now - _lastRunTime < 0.2f)
                return;
            _lastRunTime = now;

            if (!InteractableObject.HasKeycardAccess(player, Base.KeycardPermissions, Base.RequireAllPermissions))
            {
                ExecuteFDenyActions(player, Base);
                LuaScriptService.ExecuteEvent(this, LuaEventType.Denied.ToString().ToLowerInvariant(), new LuaExecutionContext
                {
                    Player = player,
                    ToyId = toyId,
                    Detail = "keycard_denied",
                });
                return;
            }

            FunctionArgument args = new FunctionArgument(this, player);
            Dictionary<IPActionType, Action> executors = new Dictionary<IPActionType, Action>
            {
                { IPActionType.Disappear, () => Destroy(gameObject, 0.1f) },
                { IPActionType.Explode, () => FExplodeModule.Execute(Base.ExplodeModules, args) },
                { IPActionType.PlayAnimation, () => FAnimationDTO.Execute(Base.AnimationModules, args) },
                { IPActionType.Warhead, () => AlphaWarhead(Base.warheadActionType.GetValue<WarheadActionType>(args, 0)) },
                { IPActionType.SendMessage, () => FMessageModule.Execute(Base.MessageModules, args) },
                { IPActionType.DropItems, () => FDropItem.Execute(Base.dropItems, args) },
                { IPActionType.SendCommand, () => FCommanding.Execute(Base.commandings, args) },
                { IPActionType.GiveEffect, () => FEffectGivingModule.Execute(Base.effectGivingModules, args) },
                { IPActionType.PlayAudio, () => FAudioModule.Execute(Base.AudioModules, args) },
                { IPActionType.CallGroovieNoise, () => FCGNModule.Execute(Base.GroovieNoiseToCall, args) },
                { IPActionType.CallFunction, () => FCFEModule.Execute(Base.FunctionToCall, args) },
                { IPActionType.ModifyPrimitive, () => FPrimitiveModifyModule.Execute(Base.PrimitiveModifyModules, args) },
                { IPActionType.ControlSpeaker, () => FLoopSpeakerControlModule.Execute(Base.LoopSpeakerModules, args) },
                { IPActionType.ControlItemSpawner, () => FItemSpawnerControlModule.Execute(Base.ItemSpawnerModules, args) },
                {
                    IPActionType.UpgradeItem, () =>
                    {
                        if (!player.GameObject.TryGetComponent<Collider>(out Collider col))
                            return;

                        Scp914Mode mode = Base.Scp914Mode.GetValue<Scp914Mode>(args, 0);
                        List<int> modes = Enumerable.Range(0, 5).Where(i => mode.HasFlag((Scp914Mode)i)).ToList();
                        if (modes.Count == 0)
                            return;

                        Scp914.Scp914Upgrader.Upgrade(new[] { col }, Scp914.Scp914Mode.Held, (Scp914.Scp914KnobSetting)modes.RandomItem());
                    }
                },
            };

            foreach (IPActionType type in Enum.GetValues(typeof(IPActionType)))
            {
                if (Base.ActionType.HasFlag(type) && executors.TryGetValue(type, out Action execute))
                    execute();
            }

            LuaScriptService.ExecuteEvent(this, LuaEventType.Interacted.ToString().ToLowerInvariant(), new LuaExecutionContext
            {
                Player = player,
                ToyId = toyId,
            });
        }
    }
}
