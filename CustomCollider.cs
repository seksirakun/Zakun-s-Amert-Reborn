using InventorySystem.Items.Pickups;
using InventorySystem.Items.ThrowableProjectiles;
using LabApi.Features.Extensions;
using LabApi.Features.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Log = ZAMERT.ZAMERTLogger;

namespace ZAMERT
{

public class CustomCollider : ZAMERTInteractable
{
    public new CCDTO Base { get; set; }

    public MeshCollider MeshCollider { get; private set; }

    public Transform OriginalTransform { get; private set; }

    protected Collider ZoneCollider;
    protected readonly HashSet<ReferenceHub> _playersInside = new HashSet<ReferenceHub>();
    private float _pollTimer;
    private const float PollInterval = 0.2f;

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
                if (!(vs[1] is GameObject go)) return "0 0 0";
                Vector3 pos = go.transform.position;
                return pos.x + " " + pos.y + " " + pos.z;
            }
        },
        { "{o_room}", vs => vs[1] is GameObject go && Room.GetRoomAtPosition(go.transform.position) != null ? Room.GetRoomAtPosition(go.transform.position).Name.ToString() : "None" },
        { "{o_zone}", vs => vs[1] is GameObject go && Room.GetRoomAtPosition(go.transform.position) != null ? Room.GetRoomAtPosition(go.transform.position).Zone.ToString() : "None" },
    };

    protected virtual void Start()
    {
        this.Base = base.Base as CCDTO;
        Register();
        LuaScriptService.ExecuteEvent(this, LuaEventType.Spawned.ToString().ToLowerInvariant());
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        ZAMERTPlugin.Singleton?.CustomColliders?.Remove(this);
    }

    protected void Register()
    {
        Log.Debug("Registering CustomCollider: " + gameObject.name + " (" + OSchematic.Name + ")");
        if (!ZAMERTPlugin.Singleton.CustomColliders.Contains(this))
            ZAMERTPlugin.Singleton.CustomColliders.Add(this);

        MeshCollider existingMeshCollider = gameObject.GetComponent<MeshCollider>();
        if (existingMeshCollider != null)
        {
            MeshCollider = existingMeshCollider;
            MeshCollider.convex = true;
            MeshCollider.isTrigger = true;
            ZoneCollider = MeshCollider;
            EnsureTriggerRigidbody();
            HidePrimitiveToys();
            return;
        }

        try
        {
            MeshFilter[] meshFilters = transform.GetComponentsInChildren<MeshFilter>()
                .Where(x => x != null && x.sharedMesh != null)
                .ToArray();
            if (meshFilters.Length == 0)
                throw new InvalidOperationException("No MeshFilters found under collider object.");

            MeshCollider = gameObject.AddComponent<MeshCollider>();
            CombineInstance[] combineInstances = new CombineInstance[meshFilters.Length];

            for (int i = 0; i < meshFilters.Length; i++)
            {
                combineInstances[i].mesh = meshFilters[i].sharedMesh;
                combineInstances[i].transform = transform.worldToLocalMatrix * meshFilters[i].transform.localToWorldMatrix;
            }

            Mesh mesh = new Mesh();
            mesh.CombineMeshes(combineInstances);
            MeshCollider.sharedMesh = mesh;
            MeshCollider.convex = true;
            MeshCollider.isTrigger = true;
            ZoneCollider = MeshCollider;
            EnsureTriggerRigidbody();
            HidePrimitiveToys();
        }
        catch (Exception ex)
        {
            Log.Error("CustomCollider mesh combine failed on " + gameObject.name + ": " + ex.Message);

            SphereCollider sc = gameObject.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 1f;
            ZoneCollider = sc;
            EnsureTriggerRigidbody();
        }
    }

    protected virtual void Update()
    {
        if (!Active || ZoneCollider == null) return;
        if (!DetectsPlayers()) return;

        _pollTimer += UnityEngine.Time.deltaTime;
        if (_pollTimer < PollInterval) return;
        _pollTimer = 0f;

        PollPlayers();
    }

    protected virtual bool DetectsPlayers()
    {
        return Base != null && Base.DetectType.HasFlag(DetectType.Player);
    }

    protected void PollPlayers()
    {
        CollisionType ct = GetCollisionType();

        foreach (Player player in Player.List)
        {
            if (player == null || !player.IsAlive || player.ReferenceHub == null)
                continue;

            ReferenceHub hub = player.ReferenceHub;
            bool inside = ZoneUtils.IsInside(ZoneCollider, player.Position);
            bool was = _playersInside.Contains(hub);

            if (inside && !was)
            {
                _playersInside.Add(hub);
                if (ct.HasFlag(CollisionType.OnEnter))
                    ExecuteColliderActions(player, "player", LuaEventType.Entered.ToString().ToLowerInvariant());
            }
            else if (inside)
            {
                if (ct.HasFlag(CollisionType.OnStay))
                    ExecuteColliderActions(player, "player", LuaEventType.Stayed.ToString().ToLowerInvariant());
            }
            else if (was)
            {
                _playersInside.Remove(hub);
                if (ct.HasFlag(CollisionType.OnExit))
                    ExecuteColliderActions(player, "player", LuaEventType.Exited.ToString().ToLowerInvariant());
            }
        }

        _playersInside.RemoveWhere(h => h == null || Player.Get(h) == null || !Player.Get(h).IsAlive);
    }

    private void EnsureTriggerRigidbody()
    {
        Rigidbody rigidbody = gameObject.GetComponent<Rigidbody>();
        if (rigidbody == null)
            rigidbody = gameObject.AddComponent<Rigidbody>();

        rigidbody.isKinematic = true;
        rigidbody.useGravity = false;
    }

    private void HidePrimitiveToys()
    {
        transform.GetComponentsInChildren<AdminToys.PrimitiveObjectToy>().ForEach(x =>
        {
            PrimitiveObjectToy.Get(x).Flags = AdminToys.PrimitiveFlags.None;
        });
    }

    protected void OnTriggerEnter(Collider collider)
    {
        if (collider.GetComponentInParent<ReferenceHub>() != null) return; // players handled by polling
        if (GetCollisionType().HasFlag(CollisionType.OnEnter))
        {
            RunProcess(collider, LuaEventType.Entered.ToString().ToLowerInvariant());
        }
    }

    protected void OnTriggerExit(Collider collider)
    {
        if (collider.GetComponentInParent<ReferenceHub>() != null) return;
        if (GetCollisionType().HasFlag(CollisionType.OnExit))
        {
            RunProcess(collider, LuaEventType.Exited.ToString().ToLowerInvariant());
        }
    }

    protected void OnTriggerStay(Collider collider)
    {
        if (collider.GetComponentInParent<ReferenceHub>() != null) return;
        if (GetCollisionType().HasFlag(CollisionType.OnStay))
        {
            RunProcess(collider, LuaEventType.Stayed.ToString().ToLowerInvariant());
        }
    }

    protected virtual CollisionType GetCollisionType()
    {
        return Base != null ? Base.CollisionType : 0;
    }

    public virtual void RunProcess(Collider collider, string eventName = null)
    {
        if (!Active)
        {
            return;
        }

        Pickup pickup = null;
        ItemPickupBase ipb = collider.GetComponentInParent<ItemPickupBase>();
        if (ipb != null)
        {
            pickup = Pickup.Get(ipb);
            if (pickup is null)
            {
                Log.Debug("CC: Pickup was not found in collider object's ItemPickupBase: " + ipb.name);
            }
        }

        bool flag = false;
        Player target = null;
        string detail = "player";
        if (Base.DetectType.HasFlag(DetectType.Pickup) && pickup != null)
        {
            flag = true;
            target = pickup.LastOwner;
            detail = "pickup";
        }
        ThrownProjectile projectile = collider.GetComponentInParent<ThrownProjectile>();
        if (Base.DetectType.HasFlag(DetectType.Projectile) && projectile != null)
        {
            flag = true;
            target = Player.Get(projectile.PreviousOwner.Hub);
            detail = "projectile";
        }
        if (!flag)
        {
            return;
        }

        ExecuteColliderActions(target, detail, eventName);
    }

    public virtual void ExecuteColliderActions(Player target, string detail, string eventName)
    {
        if (!Active)
            return;

        if (target != null && ZAMERTInteractable.IsRoleIgnored(target, Base.IgnoredRoles))
            return;

        LuaExecutionContext luaContext = LuaScriptService.ExecuteEvent(this, (eventName ?? LuaEventType.Stayed.ToString()).ToLowerInvariant(), new LuaExecutionContext
        {
            Player = target,
            Detail = detail,
        });

        if (luaContext != null && luaContext.Cancelled)
            return;

        ModuleGeneralArguments args = new ModuleGeneralArguments()
        {
            Player = target,
            TargetCalculated = false,
            Transform = this.transform,
            Schematic = OSchematic,
            Interpolations = Formatter,
            InterpolationsList = new object[]
            {
                target,
                gameObject,
            },
        };
        var colliderActionExecutors = new Dictionary<ColliderActionType, Action>
        {
            {
                ColliderActionType.ModifyHealth, () =>
                {
                    if (target != null)
                    {
                        if (Base.ModifyHealthAmount > 0)
                        {
                            target.Heal(Base.ModifyHealthAmount);
                        }
                        else
                        {
                            target.Damage(-1f * Base.ModifyHealthAmount, "ZAMERT CustomCollider");
                        }
                    }
                }
            },
            { ColliderActionType.Explode, () => ExplodeModule.Execute(Base.ExplodeModules, args) },
            { ColliderActionType.PlayAnimation, () => AnimationDTO.Execute(Base.AnimationModules, args) },
            { ColliderActionType.Warhead, () => AlphaWarhead(Base.warheadActionType) },
            { ColliderActionType.SendMessage, () => MessageModule.Execute(Base.MessageModules, args) },
            { ColliderActionType.SendCommand, () => Commanding.Execute(Base.commandings, args) },
            { ColliderActionType.GiveEffect, () => EffectGivingModule.Execute(Base.effectGivingModules, args) },
            { ColliderActionType.PlayAudio, () => AudioModule.Execute(Base.AudioModules, args) },
            { ColliderActionType.CallGroovieNoise, () => CGNModule.Execute(Base.GroovieNoiseToCall, args) },
            { ColliderActionType.CallFunction, () => CFEModule.Execute(Base.FunctionToCall, args) },
            { ColliderActionType.DropItems, () => DropItem.Execute(Base.dropItems, args) },
            { ColliderActionType.ModifyPrimitive, () => PrimitiveModifyModule.Execute(Base.PrimitiveModifyModules, args) },
            { ColliderActionType.ControlSpeaker, () => LoopSpeakerControlModule.Execute(Base.LoopSpeakerModules, args) },
            { ColliderActionType.ControlItemSpawner, () => ItemSpawnerControlModule.Execute(Base.ItemSpawnerModules, args) },
            { ColliderActionType.PlayerLink, () => PlayerLinkModule.Execute(Base.PlayerLinkModules, args) },
        };
        foreach (ColliderActionType type in Enum.GetValues(typeof(ColliderActionType)))
        {
            if (Base.ColliderActionType.HasFlag(type) && colliderActionExecutors.TryGetValue(type, out var execute))
            {
                Log.Debug("- CC: detected one of [" + Base.DetectType.GetActiveFlagNames() + "] - executing ColliderAction: " + type);
                execute();
            }
        }
    }
}

public class FCustomCollider : CustomCollider
{
    public new FCCDTO Base { get; set; }

    protected override void Start()
    {
        Base = ((ZAMERTInteractable)this).Base as FCCDTO;
        Register();
        LuaScriptService.ExecuteEvent(this, LuaEventType.Spawned.ToString().ToLowerInvariant());
    }

    protected override CollisionType GetCollisionType()
    {
        return Base != null ? Base.CollisionType.GetValue<CollisionType>(new FunctionArgument(this), 0) : 0;
    }

    protected override bool DetectsPlayers()
    {
        return Base != null && Base.DetectType.GetValue<DetectType>(new FunctionArgument(this), 0).HasFlag(DetectType.Player);
    }

    public override void RunProcess(Collider collider, string eventName = null)
    {
        if (!Active)
        {
            return;
        }

        Pickup pickup = null;
        ItemPickupBase ipb = collider.GetComponentInParent<ItemPickupBase>();
        if (ipb != null)
        {
            pickup = Pickup.Get(ipb);
        }

        bool flag = false;
        Player target = null;
        string detail = "player";
        DetectType collision = Base.DetectType.GetValue<DetectType>(new FunctionArgument(this), 0);
        if (collision.HasFlag(DetectType.Pickup) && pickup != null)
        {
            flag = true;
            target = pickup.LastOwner;
            detail = "pickup";
        }
        ThrownProjectile projectile = collider.GetComponentInParent<ThrownProjectile>();
        if (collision.HasFlag(DetectType.Projectile) && projectile != null)
        {
            flag = true;
            target = Player.Get(projectile.PreviousOwner.Hub);
            detail = "projectile";
        }
        if (!flag)
        {
            return;
        }

        ExecuteColliderActions(target, detail, eventName);
    }

    public override void ExecuteColliderActions(Player target, string detail, string eventName)
    {
        if (!Active)
            return;

        if (target != null && ZAMERTInteractable.IsRoleIgnored(target, Base.IgnoredRoles))
            return;

        LuaExecutionContext luaContext = LuaScriptService.ExecuteEvent(this, (eventName ?? LuaEventType.Stayed.ToString()).ToLowerInvariant(), new LuaExecutionContext
        {
            Player = target,
            Detail = detail,
        });

        if (luaContext != null && luaContext.Cancelled)
            return;

        FunctionArgument args = new FunctionArgument(this, target);
        ModuleGeneralArguments moduleArgs = new ModuleGeneralArguments { Player = target, Schematic = OSchematic, Transform = transform, TargetCalculated = false };
        var colliderActionExecutors = new Dictionary<ColliderActionType, Action>
        {
            {
                ColliderActionType.ModifyHealth, () =>
                {
                    if (target != null)
                    {
                        float amount = Base.ModifyHealthAmount.GetValue(args, 0f);
                        if (amount > 0)
                        {
                            target.Heal(amount);
                        }
                        else
                        {
                            target.Damage(-amount, "ZAMERT FCustomCollider");
                        }
                    }
                }
            },
            { ColliderActionType.Explode, () => FExplodeModule.Execute(Base.ExplodeModules, args) },
            { ColliderActionType.PlayAnimation, () => FAnimationDTO.Execute(Base.AnimationModules, args) },
            { ColliderActionType.Warhead, () => AlphaWarhead(Base.warheadActionType.GetValue<WarheadActionType>(args, 0)) },
            { ColliderActionType.SendMessage, () => FMessageModule.Execute(Base.MessageModules, args) },
            { ColliderActionType.SendCommand, () => FCommanding.Execute(Base.commandings, args) },
            { ColliderActionType.GiveEffect, () => FEffectGivingModule.Execute(Base.effectGivingModules, args) },
            { ColliderActionType.PlayAudio, () => FAudioModule.Execute(Base.AudioModules, args) },
            { ColliderActionType.CallGroovieNoise, () => FCGNModule.Execute(Base.GroovieNoiseToCall, args) },
            { ColliderActionType.CallFunction, () => FCFEModule.Execute(Base.FunctionToCall, args) },
            { ColliderActionType.DropItems, () => FDropItem.Execute(Base.dropItems, args) },
            { ColliderActionType.ModifyPrimitive, () => FPrimitiveModifyModule.Execute(Base.PrimitiveModifyModules, args) },
            { ColliderActionType.ControlSpeaker, () => FLoopSpeakerControlModule.Execute(Base.LoopSpeakerModules, args) },
            { ColliderActionType.ControlItemSpawner, () => FItemSpawnerControlModule.Execute(Base.ItemSpawnerModules, args) },
            { ColliderActionType.PlayerLink, () => PlayerLinkModule.Execute(Base.PlayerLinkModules, moduleArgs) },
        };
        foreach (ColliderActionType type in Enum.GetValues(typeof(ColliderActionType)))
        {
            if (Base.ColliderActionType.HasFlag(type) && colliderActionExecutors.TryGetValue(type, out var execute))
            {
                execute();
            }
        }
    }
}
}
