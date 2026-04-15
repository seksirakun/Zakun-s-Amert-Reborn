using AdminToys;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items;
using LabApi.Events;
using LabApi.Events.Arguments.Interfaces;
using LabApi.Features.Wrappers;
using MEC;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UserSettings.ServerSpecific;
using ZAMERT.Events.Arguments;
using ZAMERT.Events.Handlers;
using Log = ZAMERT.ZAMERTLogger;

namespace ZAMERT
{

    public class InteractableObject : ZAMERTInteractable
    {
        public new IODTO Base { get; set; }

        public Config Configs => ZAMERTPlugin.Singleton.Config;

        protected float _lastRunTime = -999f;

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
        };

        protected void SpawnInteractableToy(AdminToys.PrimitiveObjectToy primitiveObjectToy)
        {
            if (primitiveObjectToy == null)
                return;

            InteractableToy interactableToy = InteractableToy.Create(primitiveObjectToy.transform, false);

            switch (primitiveObjectToy.PrimitiveType)
            {
                case PrimitiveType.Plane:
                    interactableToy.Shape = InvisibleInteractableToy.ColliderShape.Box;
                    break;

                case PrimitiveType.Quad:
                    interactableToy.Shape = InvisibleInteractableToy.ColliderShape.Box;
                    break;

                case PrimitiveType.Cube:
                    interactableToy.Shape = InvisibleInteractableToy.ColliderShape.Box;
                    break;

                case PrimitiveType.Sphere:
                    interactableToy.Shape = InvisibleInteractableToy.ColliderShape.Sphere;
                    break;

                case PrimitiveType.Capsule:
                    interactableToy.Shape = InvisibleInteractableToy.ColliderShape.Capsule;
                    break;

                default:
                    interactableToy.Destroy();
                    return;
            }

            interactableToy.Transform.localScale = Vector3.one * 1.3f;
            interactableToy.OnInteracted += p => RunProcess(p, toyId: primitiveObjectToy.name);
            interactableToy.Spawn();
            Log.Debug("-- spawned IoToy for PrimitiveObjectToy: " + primitiveObjectToy.name);

            if (ZAMERTPlugin.Singleton.Config.IoToysDebug)
            {
                LabApi.Features.Wrappers.PrimitiveObjectToy indicator = LabApi.Features.Wrappers.PrimitiveObjectToy.Create(primitiveObjectToy.transform, false);
                indicator.Flags = PrimitiveFlags.Visible;
                indicator.Type = primitiveObjectToy.PrimitiveType;
                indicator.Transform.localScale = Vector3.one * 1.25f;
                indicator.Color = new Color(1f, 1f, 1f, 0.2f);
                indicator.Spawn();
            }
        }

        protected virtual void Start()
        {
            Base = base.Base as IODTO;
            Log.Debug("Adding InteractableObject: " + gameObject.name + " (" + OSchematic.Name + ")");
            Register();
        }

        protected virtual void Register()
        {
            if (!ZAMERTPlugin.Singleton.InteractableObjects.Contains(this))
                ZAMERTPlugin.Singleton.InteractableObjects.Add(this);

            if (Configs.EnableIoToys && Configs.IoToysKeycodes.Contains(Base.InputKeyCode))
            {
                AdminToys.PrimitiveObjectToy rootToy = GetComponent<AdminToys.PrimitiveObjectToy>();

                if (rootToy != null && !Configs.IoToysNoRoot)
                    SpawnInteractableToy(rootToy);

                foreach (AdminToys.PrimitiveObjectToy primitiveObjectToy in GetComponentsInChildren<AdminToys.PrimitiveObjectToy>())
                {
                    if (Configs.IoToysNoRoot)
                    {
                        if (rootToy != null && (primitiveObjectToy.name == rootToy.name || primitiveObjectToy.name.Contains("Clone")))
                        {
                            Log.Debug("-- skipping duplicate/clone toy: " + primitiveObjectToy.name);
                            continue;
                        }
                    }

                    AdminToys.PrimitiveObjectToy cachedToy = primitiveObjectToy;
                    Timing.CallDelayed(1f, () => SpawnInteractableToy(cachedToy));
                }

                if (!Configs.IoToysBothModes)
                    return;

                Log.Debug("-- IoToysBothModes: also registering SSKeybind for " + gameObject.name);
            }

            if (ZAMERTPlugin.Singleton.IOkeys.ContainsKey(Base.InputKeyCode))
            {
                ZAMERTPlugin.Singleton.IOkeys[Base.InputKeyCode].Add(this);
            }
            else
            {
                ZAMERTPlugin.Singleton.IOkeys.Add(Base.InputKeyCode, new List<InteractableObject> { this });
                KeyCode ioKeyCode = (KeyCode)Base.InputKeyCode;

                Log.Debug("-- adding new IO SSKeybind setting for schematic with key: " + ioKeyCode);
                string expectedLabel = ServerSettings.IOLabelPreamble + " - " + ioKeyCode;
                bool alreadyRegistered = ServerSpecificSettingsSync.DefinedSettings != null &&
                    ServerSpecificSettingsSync.DefinedSettings.Any(x => x is SSKeybindSetting && x.Label == expectedLabel);
                if (!alreadyRegistered)
                {
                    SSKeybindSetting newSetting = ServerSettings.CreateIOSettingForKeycode(ioKeyCode);
                    ServerSpecificSettingsSync.DefinedSettings = ServerSpecificSettingsSync.DefinedSettings.Append(newSetting).ToArray();
                    ServerSpecificSettingsSync.SendToAll();
                }
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            ZAMERTPlugin.Singleton?.InteractableObjects?.Remove(this);
        }

        protected void ExecuteDenyActions(Player player, IPActionType denyType,
            List<MessageModule> messages, List<AudioModule> audio,
            List<CGNModule> groovie, List<CFEModule> functions)
        {
            ModuleGeneralArguments denyArgs = new ModuleGeneralArguments()
            {
                Interpolations = Formatter,
                InterpolationsList = new object[] { player },
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

        protected void ExecuteFDenyActions(Player player, IPActionType denyType,
            List<FMessageModule> messages, List<FAudioModule> audio,
            List<FCGNModule> groovie, List<FCFEModule> functions)
        {
            FunctionArgument denyArgs = new FunctionArgument(this, player);
            if (denyType.HasFlag(IPActionType.SendMessage)) FMessageModule.Execute(messages, denyArgs);
            if (denyType.HasFlag(IPActionType.PlayAudio)) FAudioModule.Execute(audio, denyArgs);
            if (denyType.HasFlag(IPActionType.CallGroovieNoise)) FCGNModule.Execute(groovie, denyArgs);
            if (denyType.HasFlag(IPActionType.CallFunction)) FCFEModule.Execute(functions, denyArgs);
        }

        internal static bool HasKeycardAccess(Player player, DoorPermissionFlags required, bool requireAll)
        {
            try
            {
                if (required == DoorPermissionFlags.None) return true;
                if (player == null) return false;
                if (player.IsBypassEnabled) return true;
                if (player.IsSCP) return required.HasFlag(DoorPermissionFlags.ScpOverride);
                if (player.CurrentItem == null) return false;
                if (!(player.CurrentItem is KeycardItem keycard)) return false;
                DoorPermissionFlags held = keycard.Base.GetPermissions(null);
                return requireAll
                    ? (held & required) == required
                    : (held & required) > DoorPermissionFlags.None;
            }
            catch (Exception ex)
            {
                Log.Warn("HasKeycardAccess exception: " + ex.Message);
                return true;
            }
        }

        public virtual void RunProcess(Player player, string toyId = "Unknown")
        {
            if (!Active || player == null)
                return;

            float now = Time.time;
            if (now - _lastRunTime < 0.2f)
            {
                Log.Debug("IO dedup: skipping duplicate trigger on " + gameObject.name);
                return;
            }
            _lastRunTime = now;

            if (!HasKeycardAccess(player, Base.KeycardPermissions, Base.RequireAllPermissions))
            {
                Log.Debug("Player: " + player.Nickname + " denied IO (no keycard permission) on: " + gameObject.name);
                if (Base.DenyActionType != 0)
                    ExecuteDenyActions(player, Base.DenyActionType, Base.DenyMessageModules, Base.DenyAudioModules, Base.DenyGroovieNoiseToCall, Base.DenyFunctionToCall);
                return;
            }

            Log.Debug("Player: " + player.Nickname + " interacted with InteractableObject: " + gameObject.name + " (" + OSchematic.Name + ")" + (Configs.EnableIoToys ? " -- toy id: " + toyId : ""));

            ModuleGeneralArguments args = new ModuleGeneralArguments()
            {
                Interpolations = Formatter,
                InterpolationsList = new object[] { player },
                Player = player,
                Schematic = OSchematic,
                Transform = this.transform,
                TargetCalculated = false,
            };

            var actionExecutors = new Dictionary<IPActionType, Action>
            {
                { IPActionType.Disappear, () => Destroy(this.gameObject, 0.1f) },
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
                            List<int> vs = new List<int>();
                            for (int j = 0; j < 5; j++)
                            {
                                if (Base.Scp914Mode.HasFlag((Scp914Mode)j))
                                    vs.Add(j);
                            }

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
                if (Base.ActionType.HasFlag(type) && actionExecutors.TryGetValue(type, out var execute))
                {
                    Log.Debug("- IO: executing IOAction: " + type);
                    execute();
                }
            }

            IODTO clone = new IODTO
            {
                ObjectId = Base.ObjectId,
                InputKeyCode = Base.InputKeyCode,
            };
            InteractableObjectEventHandlers.OnPlayerIOInteracted(new InteractableObjectInteractedEventArgs(player, clone, gameObject.name));
        }
    }

    public class FInteractableObject : InteractableObject
    {
        public new FIODTO Base { get; set; }

        protected override void Start()
        {
            Base = ((ZAMERTInteractable)this).Base as FIODTO;
            Log.Debug("Adding FInteractableObject: " + gameObject.name + " (" + OSchematic.Name + ")");
            Register();
        }

        public override void RunProcess(Player player, string toyId = "Unknown")
        {
            if (!Active || player == null)
                return;

            float now = Time.time;
            if (now - _lastRunTime < 0.2f)
            {
                Log.Debug("FIO dedup: skipping duplicate trigger on " + gameObject.name);
                return;
            }
            _lastRunTime = now;

            if (!HasKeycardAccess(player, Base.KeycardPermissions, Base.RequireAllPermissions))
            {
                Log.Debug("Player: " + player.Nickname + " denied FIO (no keycard permission) on: " + gameObject.name);
                if (Base.DenyActionType != 0)
                    ExecuteFDenyActions(player, Base.DenyActionType, Base.DenyMessageModules, Base.DenyAudioModules, Base.DenyGroovieNoiseToCall, Base.DenyFunctionToCall);
                return;
            }

            Log.Debug("Player: " + player.Nickname + " interacted with FInteractableObject: " + gameObject.name + " (" + OSchematic.Name + ") -- toy id: " + toyId);

            FunctionArgument args = new FunctionArgument(this, player);
            var actionExecutors = new Dictionary<IPActionType, Action>
            {
                { IPActionType.Disappear, () => Destroy(this.gameObject, 0.1f) },
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
                            List<int> vs = new List<int>();
                            Scp914Mode mode = Base.Scp914Mode.GetValue<Scp914Mode>(args, 0);
                            for (int j = 0; j < 5; j++)
                            {
                                if (mode.HasFlag((Scp914Mode)j))
                                    vs.Add(j);
                            }

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
                if (Base.ActionType.HasFlag(type) && actionExecutors.TryGetValue(type, out var execute))
                {
                    execute();
                }
            }
        }
    }
}
