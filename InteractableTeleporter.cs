using LabApi.Features.Wrappers;
using ProjectMER.Features.Objects;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZAMERT
{

public class InteractableTeleporter : ZAMERTInteractable
{
    public new ITDTO Base { get; set; }

    public TeleportObject Teleport { get; set; }

    public static readonly Dictionary<string, Func<object[], string>> Formatter = new Dictionary<string, Func<object[], string>>()
    {
        { "{p_i}", vs => vs[0] is Player p ? p.PlayerId.ToString() : "null" },
        { "{p_name}", vs => vs[0] is Player p ? p.Nickname : "null" },
        {
            "{p_pos}", vs =>
            {
                if (!(vs[0] is Player p)) return "0 0 0";
                Vector3 pos = p.Position;
                return string.Format("{0} {1} {2}", pos.x, pos.y, pos.z);
            }
        },
        { "{p_room}", vs => vs[0] is Player p && p.Room != null ? p.Room.Name.ToString() : "None" },
        { "{p_zone}", vs => vs[0] is Player p ? p.Zone.ToString() : "None" },
        { "{p_role}", vs => vs[0] is Player p ? p.Role.ToString() : "None" },
        { "{p_item}", vs => vs[0] is Player p && p.CurrentItem != null ? p.CurrentItem.Type.ToString() : "None" },
        {
            "{o_pos}", vs =>
            {
                if (!(vs[1] is TeleportObject to)) return "0 0 0";
                Vector3 pos = to.transform.position;
                return string.Format("{0} {1} {2}", pos.x, pos.y, pos.z);
            }
        },
        { "{o_room}", vs => vs[1] is TeleportObject to && Room.GetRoomAtPosition(to.transform.position) != null ? Room.GetRoomAtPosition(to.transform.position).Name.ToString() : "None" },
        { "{o_zone}", vs => vs[1] is TeleportObject to && Room.GetRoomAtPosition(to.transform.position) != null ? Room.GetRoomAtPosition(to.transform.position).Zone.ToString() : "None" },
    };

    protected virtual void Start()
    {
        Base = Base ?? base.Base as ITDTO;
        if (Base == null)
        {
            Destroy(this);
            return;
        }
        Active = Base.Active;

        TeleportObject tpObject = GetComponent<TeleportObject>() ?? GetComponentInParent<TeleportObject>();
        if (tpObject != null)
        {
            Teleport = tpObject;
            if (!ZAMERTPlugin.Singleton.InteractableTeleporters.Contains(this))
                ZAMERTPlugin.Singleton.InteractableTeleporters.Add(this);
            LuaScriptService.ExecuteEvent(this, LuaEventType.Spawned.ToString().ToLowerInvariant());
        }
        else
        {
            Destroy(this);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        ZAMERTPlugin.Singleton?.InteractableTeleporters?.Remove(this);
    }

    public void OnTriggerEnter(Collider collider)
    {
        ReferenceHub hub = collider.GetComponentInParent<ReferenceHub>();
        Player player = hub != null ? Player.Get(hub) : null;
        if (player != null && Base.InvokeType.HasFlag(TeleportInvokeType.Collide))
        {
            RunProcess(player);
        }
    }

    public void RunProcess(Player player)
    {
        if (!Active || player == null)
        {
            return;
        }

        ModuleGeneralArguments args = new ModuleGeneralArguments()
        {
            Interpolations = Formatter,
            InterpolationsList = new object[] { player },
            Player = player,
            Schematic = OSchematic,
            Transform = transform,
            TargetCalculated = false,
        };
        var actionExecutors = new Dictionary<IPActionType, Action>
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
            { IPActionType.PlayerLink, () => PlayerLinkModule.Execute(Base.PlayerLinkModules, args) },
            {
                IPActionType.UpgradeItem, () =>
                {
                    if (!player.GameObject.TryGetComponent<Collider>(out Collider col))
                        return;

                    List<int> modes = new List<int>();
                    for (int i = 0; i < 5; i++)
                    {
                        if (Base.Scp914Mode.HasFlag((Scp914Mode)i))
                            modes.Add(i);
                    }
                    if (modes.Count > 0)
                        Scp914.Scp914Upgrader.Upgrade(new[] { col }, Scp914.Scp914Mode.Held, (Scp914.Scp914KnobSetting)modes.RandomItem());
                }
            },
        };
        foreach (IPActionType type in Enum.GetValues(typeof(IPActionType)))
        {
            if (Base.ActionType.HasFlag(type) && actionExecutors.TryGetValue(type, out var execute))
            {
                execute();
            }
        }

        LuaScriptService.ExecuteEvent(this, LuaEventType.Interacted.ToString().ToLowerInvariant(), new LuaExecutionContext
        {
            Player = player,
            Detail = "teleporter",
        });
    }
}
}
