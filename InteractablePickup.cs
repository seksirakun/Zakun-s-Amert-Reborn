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

        private ItemPickupBase _pickupBase;

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

            if (TryResolvePickup())
            {
                Log.Debug("Adding interactable pickup: " + gameObject.name + " (" + OSchematic.Name + ")");
                if (!ZAMERTPlugin.Singleton.InteractablePickups.Contains(this))
                    ZAMERTPlugin.Singleton.InteractablePickups.Add(this);
                LuaScriptService.ExecuteEvent(this, LuaEventType.Spawned.ToString().ToLowerInvariant());
            }
            else
            {
                Destroy(this);
            }
        }

        protected bool TryResolvePickup()
        {
            ItemPickupBase pickupBase = GetComponent<ItemPickupBase>();
            if (pickupBase == null)
            {
                List<ItemPickupBase> childPickups = GetComponentsInChildren<ItemPickupBase>(true).Where(x => x != null).Distinct().ToList();
                if (childPickups.Count == 1)
                    pickupBase = childPickups[0];
                else if (childPickups.Count > 1)
                {
                    Log.Warn("InteractablePickup '" + gameObject.name + "' has multiple child pickups. Put the component on the exact pickup object to prevent cross-actions.");
                    return false;
                }
            }
            if (pickupBase == null)
            {
                List<ItemPickupBase> parentPickups = GetComponentsInParent<ItemPickupBase>(true).Where(x => x != null).Distinct().ToList();
                if (parentPickups.Count == 1)
                    pickupBase = parentPickups[0];
                else if (parentPickups.Count > 1)
                {
                    Log.Warn("InteractablePickup '" + gameObject.name + "' has multiple parent pickups. Put the component on the exact pickup object to prevent cross-actions.");
                    return false;
                }
            }

            if (pickupBase == null)
                return false;

            _pickupBase = pickupBase;
            Pickup = Pickup.Get(pickupBase);
            return Pickup != null;
        }

        public bool MatchesPickup(Pickup pickup)
        {
            if (pickup == null)
                return false;

            if (Pickup == null && !TryResolvePickup())
                return false;

            if (ReferenceEquals(Pickup, pickup))
                return true;

            return _pickupBase != null && pickup.Base != null && _pickupBase == pickup.Base;
        }

        protected bool IsIgnoredRole(Player player)
        {
            if (this is FInteractablePickup fInteractable)
                return ZAMERTInteractable.IsRoleIgnored(player, fInteractable.Base?.IgnoredRoles);

            return ZAMERTInteractable.IsRoleIgnored(player, Base?.IgnoredRoles);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            ZAMERTPlugin.Singleton?.InteractablePickups?.Remove(this);
        }

        protected void ExecuteIPDenyActions(Player player, IPDTO Base)
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
            if (Base.DenyActionType.HasFlag(IPActionType.SendMessage)) MessageModule.Execute(Base.DenyMessageModules, denyArgs);
            if (Base.DenyActionType.HasFlag(IPActionType.PlayAudio)) AudioModule.Execute(Base.DenyAudioModules, denyArgs);
            if (Base.DenyActionType.HasFlag(IPActionType.CallGroovieNoise)) CGNModule.Execute(Base.DenyGroovieNoiseToCall, denyArgs);
            if (Base.DenyActionType.HasFlag(IPActionType.CallFunction)) CFEModule.Execute(Base.DenyFunctionToCall, denyArgs);
            if (Base.DenyActionType.HasFlag(IPActionType.PlayAnimation)) AnimationDTO.Execute(Base.DenyAnimationModules, denyArgs);
            if (Base.DenyActionType.HasFlag(IPActionType.Warhead)) AlphaWarhead(Base.DenyWarheadActionType);
            if (Base.DenyActionType.HasFlag(IPActionType.DropItems)) DropItem.Execute(Base.DenyDropItems, denyArgs);
            if (Base.DenyActionType.HasFlag(IPActionType.SendCommand)) Commanding.Execute(Base.DenyCommandings, denyArgs);
            if (Base.DenyActionType.HasFlag(IPActionType.Explode)) ExplodeModule.Execute(Base.DenyExplodeModules, denyArgs);
            if (Base.DenyActionType.HasFlag(IPActionType.GiveEffect)) EffectGivingModule.Execute(Base.DenyEffectGivingModules, denyArgs);
            if (Base.DenyActionType.HasFlag(IPActionType.ModifyPrimitive)) PrimitiveModifyModule.Execute(Base.DenyPrimitiveModifyModules, denyArgs);
            if (Base.DenyActionType.HasFlag(IPActionType.ControlSpeaker)) LoopSpeakerControlModule.Execute(Base.DenyLoopSpeakerModules, denyArgs);
            if (Base.DenyActionType.HasFlag(IPActionType.ControlItemSpawner)) ItemSpawnerControlModule.Execute(Base.DenyItemSpawnerModules, denyArgs);
            if (Base.DenyActionType.HasFlag(IPActionType.PlayerLink)) PlayerLinkModule.Execute(Base.DenyPlayerLinkModules, denyArgs);
        }

        protected void ExecuteFIPDenyActions(Player player, FIPDTO FBase)
        {
            FunctionArgument denyArgs = new FunctionArgument(this, player);
            if (FBase.DenyActionType.HasFlag(IPActionType.SendMessage)) FMessageModule.Execute(FBase.DenyMessageModules, denyArgs);
            if (FBase.DenyActionType.HasFlag(IPActionType.PlayAudio)) FAudioModule.Execute(FBase.DenyAudioModules, denyArgs);
            if (FBase.DenyActionType.HasFlag(IPActionType.CallGroovieNoise)) FCGNModule.Execute(FBase.DenyGroovieNoiseToCall, denyArgs);
            if (FBase.DenyActionType.HasFlag(IPActionType.CallFunction)) FCFEModule.Execute(FBase.DenyFunctionToCall, denyArgs);
            if (FBase.DenyActionType.HasFlag(IPActionType.PlayAnimation)) FAnimationDTO.Execute(FBase.DenyAnimationModules, denyArgs);
            if (FBase.DenyActionType.HasFlag(IPActionType.Warhead)) AlphaWarhead(FBase.DenyWarheadActionType.GetValue(denyArgs, WarheadActionType.Start));
            if (FBase.DenyActionType.HasFlag(IPActionType.DropItems)) FDropItem.Execute(FBase.DenyDropItems, denyArgs);
            if (FBase.DenyActionType.HasFlag(IPActionType.SendCommand)) FCommanding.Execute(FBase.DenyCommandings, denyArgs);
            if (FBase.DenyActionType.HasFlag(IPActionType.Explode)) FExplodeModule.Execute(FBase.DenyExplodeModules, denyArgs);
            if (FBase.DenyActionType.HasFlag(IPActionType.GiveEffect)) FEffectGivingModule.Execute(FBase.DenyEffectGivingModules, denyArgs);
            if (FBase.DenyActionType.HasFlag(IPActionType.ModifyPrimitive)) FPrimitiveModifyModule.Execute(FBase.DenyPrimitiveModifyModules, denyArgs);
            if (FBase.DenyActionType.HasFlag(IPActionType.ControlSpeaker)) FLoopSpeakerControlModule.Execute(FBase.DenyLoopSpeakerModules, denyArgs);
            if (FBase.DenyActionType.HasFlag(IPActionType.ControlItemSpawner)) FItemSpawnerControlModule.Execute(FBase.DenyItemSpawnerModules, denyArgs);
            if (FBase.DenyActionType.HasFlag(IPActionType.PlayerLink)) PlayerLinkModule.Execute(FBase.DenyPlayerLinkModules, new ModuleGeneralArguments { Player = player, Schematic = OSchematic, Transform = transform, TargetCalculated = false });
        }

        public virtual void RunProcess(Player player, Pickup pickup, out bool remove, string eventName = null)
        {
            bool r = false;
            remove = false;

            if (!MatchesPickup(pickup) || !Active || player == null || pickup == null || IsIgnoredRole(player))
                return;

            if (!InteractableObject.HasKeycardAccess(player, Base.KeycardPermissions, Base.RequireAllPermissions))
            {
                Log.Debug("Player: " + player.Nickname + " denied IP (no keycard permission) on: " + gameObject.name);
                if (Base.DenyActionType != 0)
                    ExecuteIPDenyActions(player, Base);

                LuaScriptService.ExecuteEvent(this, LuaEventType.Denied.ToString().ToLowerInvariant(), new LuaExecutionContext
                {
                    Player = player,
                    Pickup = pickup,
                    Detail = eventName ?? string.Empty,
                });
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
                { IPActionType.PlayerLink, () => PlayerLinkModule.Execute(Base.PlayerLinkModules, args) },
            };

            foreach (IPActionType type in Enum.GetValues(typeof(IPActionType)))
            {
                if (Base.ActionType.HasFlag(type) && ipActionExecutors.TryGetValue(type, out var execute))
                {
                    Log.Debug("- IP: executing IPAction: " + type);
                    execute();
                }
            }

            LuaExecutionContext luaContext = LuaScriptService.ExecuteEvent(this, (eventName ?? LuaEventType.Interacted.ToString()).ToLowerInvariant(), new LuaExecutionContext
            {
                Player = player,
                Pickup = pickup,
            });

            if (luaContext != null && luaContext.RemovePickup)
                r = true;

            remove = r;
        }
    }

    public class FInteractablePickup : InteractablePickup
    {
        public new FIPDTO Base { get; set; }

        protected override void Start()
        {
            this.Base = ((ZAMERTInteractable)this).Base as FIPDTO;

            if (TryResolvePickup())
            {
                if (!ZAMERTPlugin.Singleton.InteractablePickups.Contains(this))
                    ZAMERTPlugin.Singleton.InteractablePickups.Add(this);
                LuaScriptService.ExecuteEvent(this, LuaEventType.Spawned.ToString().ToLowerInvariant());
            }
            else
            {
                Destroy(this);
            }
        }

        public override void RunProcess(Player player, Pickup pickup, out bool remove, string eventName = null)
        {
            bool r = false;
            remove = false;

            if (!MatchesPickup(pickup) || !Active || player == null || pickup == null || IsIgnoredRole(player))
                return;

            if (!InteractableObject.HasKeycardAccess(player, Base.KeycardPermissions, Base.RequireAllPermissions))
            {
                Log.Debug("Player: " + player.Nickname + " denied FIP (no keycard permission) on: " + gameObject.name);
                if (Base.DenyActionType != 0)
                    ExecuteFIPDenyActions(player, Base);

                LuaScriptService.ExecuteEvent(this, LuaEventType.Denied.ToString().ToLowerInvariant(), new LuaExecutionContext
                {
                    Player = player,
                    Pickup = pickup,
                    Detail = eventName ?? string.Empty,
                });
                return;
            }

            FunctionArgument args = new FunctionArgument(this, player);
            ModuleGeneralArguments moduleArgs = new ModuleGeneralArguments { Player = player, Schematic = OSchematic, Transform = transform, TargetCalculated = false };

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
                { IPActionType.PlayerLink, () => PlayerLinkModule.Execute(Base.PlayerLinkModules, moduleArgs) },
            };

            foreach (IPActionType type in Enum.GetValues(typeof(IPActionType)))
            {
                if (Base.ActionType.HasFlag(type) && ipActionExecutors.TryGetValue(type, out var execute))
                {
                    execute();
                }
            }

            LuaExecutionContext luaContext = LuaScriptService.ExecuteEvent(this, (eventName ?? LuaEventType.Interacted.ToString()).ToLowerInvariant(), new LuaExecutionContext
            {
                Player = player,
                Pickup = pickup,
            });

            if (luaContext != null && luaContext.RemovePickup)
                r = true;

            remove = r;
        }
    }
}
