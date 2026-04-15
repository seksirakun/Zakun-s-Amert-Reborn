using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items.Pickups;
using LabApi.Features.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Log = ZAMERT.ZAMERTLogger;

namespace ZAMERT
{

    public class InteractablePickup : ZAMERTInteractable
    {
        public new IPDTO Base { get; set; }

        public Pickup Pickup { get; set; }

        public static readonly Dictionary<string, Func<object[], string>> Formatter = new Dictionary<string, Func<object[], string>>()
        {
            { "{p_i}", vs => vs[0] is Player p ? p.PlayerId.ToString() : "null" },
            { "{p_name}", vs => vs[0] is Player p ? p.Nickname : "null" },
            {
                "{p_pos}", vs =>
                {
                    if (!(vs[0] is Player p)) return "0 0 0";
                    Vector3 pos = p.Position;
                    return pos.x + " " + pos.y + " " + pos.z;
                }
            },
            { "{p_room}", vs => vs[0] is Player p && p.Room != null ? p.Room.Name.ToString() : "None" },
            { "{p_zone}", vs => vs[0] is Player p ? p.Zone.ToString() : "None" },
            { "{p_role}", vs => vs[0] is Player p ? p.Role.ToString() : "None" },
            { "{p_item}", vs => vs[0] is Player p && p.CurrentItem != null ? p.CurrentItem.Type.ToString() : "None" },
            {
                "{o_pos}", vs =>
                {
                    if (!(vs[1] is Pickup pk) || pk.Transform == null)
                        return "0 0 0";

                    Vector3 pos = pk.Transform.position;
                    return pos.x + " " + pos.y + " " + pos.z;
                }
            },
            { "{o_room}", vs => vs[1] is Pickup pk && pk.Room != null ? pk.Room.Name.ToString() : "None" },
            { "{o_zone}", vs => vs[1] is Pickup pk && pk.Room != null ? pk.Room.Zone.ToString() : "None" },
        };

        protected virtual void Start()
        {
            this.Base = base.Base as IPDTO;

            if (gameObject.TryGetComponent<ItemPickupBase>(out var pickupBase))
            {
                Pickup = Pickup.Get(pickupBase);
            }

            if (Pickup != null)
            {
                Log.Debug("Adding interactable pickup: " + gameObject.name + " (" + OSchematic.Name + ")");
                if (!ZAMERTPlugin.Singleton.InteractablePickups.Contains(this))
                    ZAMERTPlugin.Singleton.InteractablePickups.Add(this);
            }
            else
            {
                Destroy(this);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            ZAMERTPlugin.Singleton?.InteractablePickups?.Remove(this);
        }

        protected void ExecuteIPDenyActions(Player player, IPActionType denyType,
            List<MessageModule> messages, List<AudioModule> audio,
            List<CGNModule> groovie, List<CFEModule> functions)
        {
            ModuleGeneralArguments denyArgs = new ModuleGeneralArguments()
            {
                Interpolations = Formatter,
                InterpolationsList = new object[] { player, this.Pickup },
                Player = player,
                Schematic = OSchematic,
                Transform = this.transform,
                TargetCalculated = false,
            };
            if (denyType.HasFlag(IPActionType.SendMessage)) MessageModule.Execute(messages, denyArgs);
            if (denyType.HasFlag(IPActionType.PlayAudio)) AudioModule.Execute(audio, denyArgs);
            if (denyType.HasFlag(IPActionType.CallGroovieNoise)) CGNModule.Execute(groovie, denyArgs);
            if (denyType.HasFlag(IPActionType.CallFunction)) CFEModule.Execute(functions, denyArgs);
        }

        protected void ExecuteFIPDenyActions(Player player, IPActionType denyType,
            List<FMessageModule> messages, List<FAudioModule> audio,
            List<FCGNModule> groovie, List<FCFEModule> functions)
        {
            FunctionArgument denyArgs = new FunctionArgument(this, player);
            if (denyType.HasFlag(IPActionType.SendMessage)) FMessageModule.Execute(messages, denyArgs);
            if (denyType.HasFlag(IPActionType.PlayAudio)) FAudioModule.Execute(audio, denyArgs);
            if (denyType.HasFlag(IPActionType.CallGroovieNoise)) FCGNModule.Execute(groovie, denyArgs);
            if (denyType.HasFlag(IPActionType.CallFunction)) FCFEModule.Execute(functions, denyArgs);
        }

        public virtual void RunProcess(Player player, Pickup pickup, out bool remove)
        {
            bool r = false;
            remove = false;

            if (pickup != this.Pickup || !Active || player == null || pickup == null)
                return;

            if (!InteractableObject.HasKeycardAccess(player, Base.KeycardPermissions, Base.RequireAllPermissions))
            {
                Log.Debug("Player: " + player.Nickname + " denied IP (no keycard permission) on: " + gameObject.name);
                if (Base.DenyActionType != 0)
                    ExecuteIPDenyActions(player, Base.DenyActionType, Base.DenyMessageModules, Base.DenyAudioModules, Base.DenyGroovieNoiseToCall, Base.DenyFunctionToCall);
                return;
            }

            ModuleGeneralArguments args = new ModuleGeneralArguments()
            {
                Interpolations = Formatter,
                InterpolationsList = new object[] { player, pickup },
                Player = player,
                Schematic = OSchematic,
                Transform = this.transform,
                TargetCalculated = false,
            };

            var ipActionExecutors = new Dictionary<IPActionType, Action>
            {
                { IPActionType.Disappear, () => r = true },
                { IPActionType.Explode, () => ExplodeModule.Execute(Base.ExplodeModules, args) },
                { IPActionType.PlayAnimation, () => AnimationDTO.Execute(Base.AnimationModules, args) },
                { IPActionType.Warhead, () => AlphaWarhead(Base.warheadActionType) },
                { IPActionType.SendMessage, () => MessageModule.Execute(Base.MessageModules, args) },
                { IPActionType.DropItems, () => DropItem.Execute(Base.dropItems, args) },
                { IPActionType.SendCommand, () => Commanding.Execute(Base.commandings, args) },
                {
                    IPActionType.UpgradeItem, () =>
                    {
                        if (player.GameObject.TryGetComponent<Collider>(out Collider col))
                        {
                            var vs = Enumerable.Range(0, 5)
                                .Where(j => Base.Scp914Mode.HasFlag((Scp914Mode)j))
                                .ToList();

                            if (vs.Count > 0)
                            {
                                Scp914.Scp914Upgrader.Upgrade(
                                    new Collider[] { col },
                                    Scp914.Scp914Mode.Held,
                                    (Scp914.Scp914KnobSetting)vs.RandomItem()
                                );
                            }
                        }
                    }
                },
                { IPActionType.GiveEffect, () => EffectGivingModule.Execute(Base.effectGivingModules, args) },
                { IPActionType.PlayAudio, () => AudioModule.Execute(Base.AudioModules, args) },
                { IPActionType.CallGroovieNoise, () => CGNModule.Execute(Base.GroovieNoiseToCall, args) },
                { IPActionType.CallFunction, () => CFEModule.Execute(Base.FunctionToCall, args) },
                { IPActionType.ModifyPrimitive, () => PrimitiveModifyModule.Execute(Base.PrimitiveModifyModules, args) },
                { IPActionType.ControlSpeaker, () => LoopSpeakerControlModule.Execute(Base.LoopSpeakerModules, args) },
                { IPActionType.ControlItemSpawner, () => ItemSpawnerControlModule.Execute(Base.ItemSpawnerModules, args) },
            };

            foreach (IPActionType type in Enum.GetValues(typeof(IPActionType)))
            {
                if (Base.ActionType.HasFlag(type) && ipActionExecutors.TryGetValue(type, out var execute))
                {
                    Log.Debug("- IP: executing IPAction: " + type);
                    execute();
                }
            }

            remove = r;
        }
    }

    public class FInteractablePickup : InteractablePickup
    {
        public new FIPDTO Base { get; set; }

        protected override void Start()
        {
            this.Base = ((ZAMERTInteractable)this).Base as FIPDTO;

            if (gameObject.TryGetComponent<ItemPickupBase>(out var pickupBase))
            {
                Pickup = Pickup.Get(pickupBase);
            }

            if (Pickup != null)
            {
                if (!ZAMERTPlugin.Singleton.InteractablePickups.Contains(this))
                    ZAMERTPlugin.Singleton.InteractablePickups.Add(this);
            }
            else
            {
                Destroy(this);
            }
        }

        public override void RunProcess(Player player, Pickup pickup, out bool remove)
        {
            bool r = false;
            remove = false;

            if (pickup != this.Pickup || !Active || player == null || pickup == null)
                return;

            if (!InteractableObject.HasKeycardAccess(player, Base.KeycardPermissions, Base.RequireAllPermissions))
            {
                Log.Debug("Player: " + player.Nickname + " denied FIP (no keycard permission) on: " + gameObject.name);
                if (Base.DenyActionType != 0)
                    ExecuteFIPDenyActions(player, Base.DenyActionType, Base.DenyMessageModules, Base.DenyAudioModules, Base.DenyGroovieNoiseToCall, Base.DenyFunctionToCall);
                return;
            }

            FunctionArgument args = new FunctionArgument(this, player);

            var ipActionExecutors = new Dictionary<IPActionType, Action>
            {
                { IPActionType.Disappear, () => r = true },
                { IPActionType.Explode, () => FExplodeModule.Execute(Base.ExplodeModules, args) },
                { IPActionType.PlayAnimation, () => FAnimationDTO.Execute(Base.AnimationModules, args) },
                { IPActionType.Warhead, () => AlphaWarhead(Base.warheadActionType.GetValue<WarheadActionType>(args, 0)) },
                { IPActionType.SendMessage, () => FMessageModule.Execute(Base.MessageModules, args) },
                { IPActionType.DropItems, () => FDropItem.Execute(Base.dropItems, args) },
                { IPActionType.SendCommand, () => FCommanding.Execute(Base.commandings, args) },
                {
                    IPActionType.UpgradeItem, () =>
                    {
                        if (player.GameObject.TryGetComponent<Collider>(out Collider col))
                        {
                            Scp914Mode mode = Base.Scp914Mode.GetValue<Scp914Mode>(args, 0);
                            var vs = Enumerable.Range(0, 5)
                                .Where(j => mode.HasFlag((Scp914Mode)j))
                                .ToList();

                            if (vs.Count > 0)
                            {
                                Scp914.Scp914Upgrader.Upgrade(
                                    new Collider[] { col },
                                    Scp914.Scp914Mode.Held,
                                    (Scp914.Scp914KnobSetting)vs.RandomItem()
                                );
                            }
                        }
                    }
                },
                { IPActionType.GiveEffect, () => FEffectGivingModule.Execute(Base.effectGivingModules, args) },
                { IPActionType.PlayAudio, () => FAudioModule.Execute(Base.AudioModules, args) },
                { IPActionType.CallGroovieNoise, () => FCGNModule.Execute(Base.GroovieNoiseToCall, args) },
                { IPActionType.CallFunction, () => FCFEModule.Execute(Base.FunctionToCall, args) },
                { IPActionType.ModifyPrimitive, () => FPrimitiveModifyModule.Execute(Base.PrimitiveModifyModules, args) },
                { IPActionType.ControlSpeaker, () => FLoopSpeakerControlModule.Execute(Base.LoopSpeakerModules, args) },
                { IPActionType.ControlItemSpawner, () => FItemSpawnerControlModule.Execute(Base.ItemSpawnerModules, args) },
            };

            foreach (IPActionType type in Enum.GetValues(typeof(IPActionType)))
            {
                if (Base.ActionType.HasFlag(type) && ipActionExecutors.TryGetValue(type, out var execute))
                {
                    execute();
                }
            }

            remove = r;
        }
    }
}
