using InventorySystem.Items.Pickups;
using InventorySystem.Items.ThrowableProjectiles;
using LabApi.Features.Extensions;
using LabApi.Features.Wrappers;
using System;
using System.Collections.Generic;
using UnityEngine;
using Log = ZAMERT.ZAMERTLogger;

namespace ZAMERT
{

public class CustomCollider : ZAMERTInteractable
{
    public new CCDTO Base { get; set; }

    public MeshCollider MeshCollider { get; private set; }

    public Transform OriginalTransform { get; private set; }

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
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        ZAMERTPlugin.Singleton?.CustomColliders?.Remove(this);
    }

    protected void Register()
    {
        Log.Debug("Registering CustomCollider: " + gameObject.name + " (" + OSchematic.Name + ")");
        ZAMERTPlugin.Singleton.CustomColliders.Add(this);
        CustomCollider[] customColliders = gameObject.GetComponents<CustomCollider>();
        if (customColliders.Length > 1 && customColliders[0] != this)
        {
            MEC.Timing.CallDelayed(0.1f, () =>
            {
                MeshCollider = customColliders[0].MeshCollider;
            });
            return;
        }
        Vector3[] vs = new Vector3[] { transform.position, transform.eulerAngles };
        transform.position = Vector3.zero;
        transform.eulerAngles = Vector3.zero;

        try
        {
            MeshFilter[] meshFilters = transform.GetComponentsInChildren<MeshFilter>();
            MeshCollider = gameObject.AddComponent<MeshCollider>();
            CombineInstance[] combineInstances = new CombineInstance[meshFilters.Length];

            for (int i = 0; i < meshFilters.Length; i++)
            {
                combineInstances[i].mesh = meshFilters[i].sharedMesh;
                combineInstances[i].transform = meshFilters[i].transform.localToWorldMatrix;
            }

            Mesh mesh = new Mesh();
            mesh.CombineMeshes(combineInstances);
            MeshCollider.sharedMesh = mesh;
            MeshCollider.convex = true;
            MeshCollider.isTrigger = true;
            Rigidbody rigidbody = gameObject.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;

            transform.GetComponentsInChildren<AdminToys.PrimitiveObjectToy>().ForEach(x =>
            {
                PrimitiveObjectToy.Get(x).Flags = AdminToys.PrimitiveFlags.None;
            });
        }
        catch (Exception ex)
        {
            Log.Error("CustomCollider mesh combine failed on " + gameObject.name + ": " + ex.Message);

            SphereCollider sc = gameObject.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 1f;
        }

        transform.position = vs[0];
        transform.eulerAngles = vs[1];
    }

    protected void OnTriggerEnter(Collider collider)
    {
        if (Base.CollisionType.HasFlag(CollisionType.OnEnter))
        {
            RunProcess(collider);
        }
    }

    protected void OnTriggerExit(Collider collider)
    {
        if (Base.CollisionType.HasFlag(CollisionType.OnExit))
        {
            RunProcess(collider);
        }
    }

    protected void OnTriggerStay(Collider collider)
    {
        if (Base.CollisionType.HasFlag(CollisionType.OnStay))
        {
            RunProcess(collider);
        }
    }

    public virtual void RunProcess(Collider collider)
    {
        if (!Active)
        {
            return;
        }

        Pickup pickup = null;
        if (collider.gameObject.TryGetComponent(out ItemPickupBase ipb))
        {
            pickup = Pickup.Get(ipb);
            if (pickup is null)
            {
                Log.Debug("CC: Pickup was not found in collider object's ItemPickupBase: " + ipb.name);
            }
        }

        bool flag = false;
        Player target = null;
        if (Base.DetectType.HasFlag(DetectType.Pickup) && pickup != null)
        {
            flag = true;
            target = pickup.LastOwner;
        }
        if (Base.DetectType.HasFlag(DetectType.Player) && Player.TryGet(collider.gameObject, out target))
        {
            flag = target.Role.GetRoleBase().ActiveTime > 0.25f;
        }
        ThrownProjectile projectile = collider.GetComponentInParent<ThrownProjectile>();
        if (Base.DetectType.HasFlag(DetectType.Projectile) && projectile != null)
        {
            flag = true;
            target = Player.Get(projectile.PreviousOwner.Hub);
        }
        if (!flag)
        {
            return;
        }

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
    }

    public override void RunProcess(Collider collider)
    {
        if (!Active)
        {
            return;
        }

        Pickup pickup = null;
        if (collider.gameObject.TryGetComponent(out ItemPickupBase ipb))
        {
            pickup = Pickup.Get(ipb);
        }

        bool flag = false;
        Player target = null;
        CollisionType collision = Base.DetectType.GetValue<CollisionType>(new FunctionArgument(this), 0);
        if (collision.HasFlag(DetectType.Pickup) && pickup != null)
        {
            flag = true;
            target = pickup.LastOwner;
        }
        if (collision.HasFlag(DetectType.Player) && Player.TryGet(collider.gameObject, out target))
        {
            flag = target.Role.GetRoleBase().ActiveTime > 0.25f;
        }
        ThrownProjectile projectile = collider.GetComponentInParent<ThrownProjectile>();
        if (collision.HasFlag(DetectType.Projectile) && projectile != null)
        {
            flag = true;
            target = Player.Get(projectile.PreviousOwner.Hub);
        }
        if (!flag)
        {
            return;
        }

        FunctionArgument args = new FunctionArgument(this, target);
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
