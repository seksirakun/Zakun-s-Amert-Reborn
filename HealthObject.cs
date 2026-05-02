using AdminToys;
using InventorySystem.Items.Armor;
using InventorySystem.Items.ThrowableProjectiles;
using LabApi.Features.Wrappers;
using LabApi.Events.Arguments.ServerEvents;
using Mirror;
using PlayerStatsSystem;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ZAMERT.Events.Arguments;
using ZAMERT.Events.Handlers;
using Log = ZAMERT.ZAMERTLogger;
using InventorySystem.Items.Pickups;

namespace ZAMERT
{

    public class Healther : NetworkBehaviour, IDestructible
    {
        public List<HealthObject> Parents { get; set; } = new List<HealthObject>();

        public uint NetworkId => base.netId;

        public Vector3 CenterOfMass => transform.position;

        public bool Damage(float damage, DamageHandlerBase handler, Vector3 exactHitPos)
        {
            bool hit = false;
            Parents.ForEach(x => hit |= x.Damage(damage, handler, exactHitPos));
            return hit;
        }
    }

    public class HealthObject : ZAMERTInteractable, IDestructible
    {
        public new HODTO Base { get; set; }

        public bool AnimationEnded { get; set; } = false;
        public float Health { get; set; }
        public bool IsAlive { get; set; } = true;

        protected bool _startShrinking = false;

        protected static Dictionary<ExplosionType, ItemType> ExplosionDic { get; } = new Dictionary<ExplosionType, ItemType>
        {
            { ExplosionType.Grenade, ItemType.GrenadeHE },
            { ExplosionType.Disruptor, ItemType.ParticleDisruptor },
            { ExplosionType.Jailbird, ItemType.Jailbird },
            { ExplosionType.Cola, ItemType.SCP207 },
            { ExplosionType.PinkCandy, ItemType.SCP330 },
            { ExplosionType.SCP018, ItemType.SCP018 },
        };

        protected static Dictionary<string, Func<object[], string>> Formatter { get; } = new Dictionary<string, Func<object[], string>>
        {
            { "{p_i}", vs => vs[0] is Player p ? p.PlayerId.ToString() : "null" },
            { "{p_name}", vs => vs[0] is Player p ? p.Nickname : "null" },
            {
                "{p_pos}", vs =>
                {
                    if (!(vs[0] is Player p))
                        return "0 0 0";

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
                    if (!(vs[1] is Transform t))
                        return "0 0 0";

                    Vector3 pos = t.position;
                    return pos.x + " " + pos.y + " " + pos.z;
                }
            },
            { "{o_room}", vs => vs[1] is Transform t && Room.GetRoomAtPosition(t.position) != null ? Room.GetRoomAtPosition(t.position).Name.ToString() : "None" },
            { "{o_zone}", vs => vs[1] is Transform t && Room.GetRoomAtPosition(t.position) != null ? Room.GetRoomAtPosition(t.position).Zone.ToString() : "None" },
            { "{damage}", vs => vs.Length > 2 ? vs[2].ToString() : "0" },
        };

        public uint NetworkId => base.netId;
        public Vector3 CenterOfMass => transform.position;

        protected virtual void Start()
        {
            this.Base = base.Base as HODTO;
            Health = Base.Health;
            Register();
        }

        protected void Register()
        {
            Log.Debug("Registering HealthObject: " + gameObject.name + " (" + OSchematic.Name + ")");
            this.transform.GetComponentsInChildren<AdminToys.PrimitiveObjectToy>().ForEach(x =>
            {
                Healther healther = x.gameObject.GetComponent<Healther>();
                if (healther == null)
                    healther = x.gameObject.AddComponent<Healther>();

                if (!healther.Parents.Contains(this))
                    healther.Parents.Add(this);
            });

            if (!ZAMERTPlugin.Singleton.HealthObjects.Contains(this))
                ZAMERTPlugin.Singleton.HealthObjects.Add(this);
        }

        protected void Update()
        {
            if (_startShrinking && !AnimationEnded)
            {
                this.transform.localScale = Vector3.Lerp(this.transform.localScale, Vector3.zero, Time.deltaTime * 5f);
                if (this.transform.localScale.magnitude <= 0.1f)
                {
                    Destroy();
                }
            }
        }

        protected void SmartDestroy(float delay = 0f)
        {
            ZAMERTInteractable[] others = GetComponents<ZAMERTInteractable>();
            bool sharing = others.Length > 1;
            if (sharing)
            {
                if (delay > 0f)
                    MEC.Timing.CallDelayed(delay, () => { if (this != null) UnityEngine.Object.Destroy(this); });
                else
                    UnityEngine.Object.Destroy(this);
            }
            else
            {
                if (delay > 0f)
                    UnityEngine.Object.Destroy(this.gameObject, delay);
                else
                    UnityEngine.Object.Destroy(this.gameObject);
            }
        }

        protected virtual void Destroy()
        {
            AnimationEnded = true;
            if (Base.DoNotDestroyAfterDeath)
                return;

            SmartDestroy();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            ZAMERTPlugin.Singleton?.HealthObjects?.Remove(this);
        }

        public virtual bool Damage(float damage, DamageHandlerBase handler, Vector3 pos)
        {
            if (!IsAlive || !Active)
                return false;

            Player attacker = null;
            AttackerDamageHandler damageHandler = handler as AttackerDamageHandler;
            if (damageHandler != null)
            {
                try
                {
                    attacker = Player.Get(damageHandler.Attacker.PlayerId);
                }
                catch
                {
                    attacker = null;
                }

                FirearmDamageHandler firearm = handler as FirearmDamageHandler;
                ExplosionDamageHandler explosion = handler as ExplosionDamageHandler;

                if (firearm != null)
                {
                    bool allowed = Base.whitelistWeapons.Count == 0;

                    if (!allowed && attacker != null && attacker.CurrentItem != null)
                    {
                        allowed = Base.whitelistWeapons.Any(x =>
                        {
                            if (Item.TryGet(attacker.CurrentItem.Base, out Item item))
                                return item.Base.ItemId.SerialNumber == x.CustomItemId;

                            return attacker.CurrentItem.Type == x.ItemType;
                        });
                    }

                    if (!allowed)
                        return false;

                    FieldInfo info = typeof(FirearmDamageHandler).GetField("_penetration", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (info != null)
                    {
                        damage = BodyArmorUtils.ProcessDamage(
                            Base.ArmorEfficient,
                            damage,
                            Mathf.RoundToInt((float)info.GetValue(firearm) * 100f)
                        );
                    }
                }

                if (explosion != null)
                {
                    if (Base.whitelistWeapons.Count != 0 && !Base.whitelistWeapons.Any(x =>
                    {
                        ItemType item;
                        if (ExplosionDic.TryGetValue(explosion.ExplosionType, out item))
                            return item == x.ItemType;

                        return false;
                    }))
                    {
                        return false;
                    }
                }
            }

            CheckDead(attacker, damage);
            return true;
        }

        public virtual void OnProjectileExploded(ProjectileExplodedEventArgs ev)
        {
            Log.Debug($"[HO] Projectile exploded. Type={ev.TimedGrenade?.Type}, Pos={ev.Position}");

            if (!IsAlive || !Active)
                return;

            if (Base.whitelistWeapons.Count != 0 &&
                Base.whitelistWeapons.Find(x => x.CustomItemId == 0 && x.ItemType == ItemType.GrenadeHE) == null)
            {
                Log.Debug("[HO] Blocked by whitelist.");
                return;
            }

            if (ev.TimedGrenade.Type != ItemType.GrenadeHE)
            {
                Log.Debug("[HO] Not HE grenade.");
                return;
            }

            float distance = Vector3.Distance(this.transform.position, ev.Position);
            Log.Debug($"[HO] Distance to explosion = {distance}");

            float damage = GetGrenadeDamageAtDistance(distance);
            Log.Debug($"[HO] Calculated grenade damage = {damage}");

            if (damage <= 0f)
                return;

            damage = BodyArmorUtils.ProcessDamage(Base.ArmorEfficient, damage, 50);
            Log.Debug($"[HO] Damage after armor = {damage}");

            CheckDead(ev.Player, damage);
        }

        internal static float GetGrenadeDamageAtDistance(float distance)
        {
            const float maxDamage = 150f;
            const float radius = 6f;

            if (distance <= 0f)
                return maxDamage;

            if (distance >= radius)
                return 0f;

            float t = distance / radius;
            return maxDamage * (1f - (t * t));
        }

        public virtual void CheckDead(Player player, float damage)
        {
            HODTO clone = new HODTO()
            {
                Health = this.Base.Health,
                ArmorEfficient = this.Base.ArmorEfficient,
                DeadType = this.Base.DeadType,
                ObjectId = this.Base.ObjectId,
            };

            HealthObjectTakingDamageEventArgs damagingEventArgs = new HealthObjectTakingDamageEventArgs(clone, player);
            HealthObjectEventHandlers.OnHealthObjectTakingDamage(damagingEventArgs);

            if (!damagingEventArgs.IsAllowed)
                return;

            Health -= damage;

            if (player != null && player.ReferenceHub != null)
            {
                Hitmarker.SendHitmarkerDirectly(player.ReferenceHub, damage / 10f);
            }

            if (Health <= 0)
            {
                IsAlive = false;

                ModuleGeneralArguments args = new ModuleGeneralArguments()
                {
                    Interpolations = Formatter,
                    InterpolationsList = new object[] { player, transform, damage },
                    Player = player,
                    Schematic = OSchematic,
                    Transform = transform,
                    TargetCalculated = false,
                };

                HealthObjectEventHandlers.OnHealthObjectDied(new HealthObjectDiedEventArgs(clone, player));

                MEC.Timing.CallDelayed(Base.DeadActionDelay, () =>
                {
                    var deadTypeExecutors = new Dictionary<DeadType, Action>
                    {
                        { DeadType.Disappear, () => SmartDestroy(0.1f) },
                        {
                            DeadType.GetRigidbody, () =>
                            {
                                MakeNonStatic(gameObject);
                                if (this.gameObject.GetComponent<Rigidbody>() == null)
                                    this.gameObject.AddComponent<Rigidbody>();
                            }
                        },
                        {
                            DeadType.DynamicDisappearing, () =>
                            {
                                MakeNonStatic(gameObject);
                                _startShrinking = true;
                            }
                        },
                        { DeadType.Explode, () => ExplodeModule.Execute(Base.ExplodeModules, args) },
                        {
                            DeadType.ResetHP, () =>
                            {
                                Health = Base.ResetHPTo == 0 ? Base.Health : Base.ResetHPTo;
                                IsAlive = true;
                            }
                        },
                        { DeadType.PlayAnimation, () => AnimationDTO.Execute(Base.AnimationModules, args) },
                        { DeadType.Warhead, () => AlphaWarhead(Base.warheadActionType) },
                        { DeadType.SendMessage, () => MessageModule.Execute(Base.MessageModules, args) },
                        { DeadType.DropItems, () => DropItem.Execute(Base.dropItems, args) },
                        { DeadType.SendCommand, () => Commanding.Execute(Base.commandings, args) },
                        { DeadType.GiveEffect, () => EffectGivingModule.Execute(Base.effectGivingModules, args) },
                        { DeadType.PlayAudio, () => AudioModule.Execute(Base.AudioModules, args) },
                        { DeadType.CallGroovieNoise, () => CGNModule.Execute(Base.GroovieNoiseToCall, args) },
                        { DeadType.CallFunction, () => CFEModule.Execute(Base.FunctionToCall, args) },
                        { DeadType.ModifyPrimitive, () => PrimitiveModifyModule.Execute(Base.PrimitiveModifyModules, args) },
                        { DeadType.ControlSpeaker, () => LoopSpeakerControlModule.Execute(Base.LoopSpeakerModules, args) },
                        { DeadType.ControlItemSpawner, () => ItemSpawnerControlModule.Execute(Base.ItemSpawnerModules, args) },
                    };

                    foreach (DeadType type in Enum.GetValues(typeof(DeadType)))
                    {
                        if (Base.DeadType.HasFlag(type) && deadTypeExecutors.TryGetValue(type, out var execute))
                        {
                            Log.Debug("- HO: executing DeadAction: " + type);
                            execute();
                        }
                    }
                });
            }
        }

        public void MakeNonStatic(GameObject game)
        {
            foreach (AdminToyBase adminToyBase in game.transform.GetComponentsInChildren<AdminToyBase>())
            {
                adminToyBase.enabled = true;
            }
        }
    }

    public class FHealthObject : HealthObject
    {
        public new FHODTO Base { get; set; }

        protected override void Start()
        {
            this.Base = ((ZAMERTInteractable)this).Base as FHODTO;
            Health = Base.Health.GetValue(new FunctionArgument(this), 100f);
            Register();
        }

        protected override void Destroy()
        {
            AnimationEnded = true;
            if (Base.DoNotDestroyAfterDeath.GetValue(new FunctionArgument(this), false))
                return;

            SmartDestroy();
        }

        public override bool Damage(float damage, DamageHandlerBase handler, Vector3 pos)
        {
            if (!IsAlive || !Active)
                return false;

            Player attacker = null;
            AttackerDamageHandler damageHandler = handler as AttackerDamageHandler;
            if (damageHandler != null)
            {
                try
                {
                    attacker = Player.Get(damageHandler.Attacker.PlayerId);
                }
                catch
                {
                    attacker = null;
                }

                FunctionArgument args = new FunctionArgument(this, attacker);
                FirearmDamageHandler firearm = handler as FirearmDamageHandler;
                ExplosionDamageHandler explosion = handler as ExplosionDamageHandler;

                if (firearm != null)
                {
                    bool allowed = Base.whitelistWeapons.Count == 0;

                    if (!allowed && attacker != null && attacker.CurrentItem != null)
                    {
                        allowed = Base.whitelistWeapons.Any(x =>
                        {
                            if (Item.TryGet(attacker.CurrentItem.Base, out Item item))
                                return item.Base.ItemId.SerialNumber == x.CustomItemId.GetValue(args, 0);

                            return attacker.CurrentItem.Type == x.ItemType.GetValue(args, ItemType.None);
                        });
                    }

                    if (!allowed)
                        return false;

                    FieldInfo info = typeof(FirearmDamageHandler).GetField("_penetration", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (info != null)
                    {
                        damage = BodyArmorUtils.ProcessDamage(
                            Base.ArmorEfficient.GetValue(args, 0),
                            damage,
                            Mathf.RoundToInt((float)info.GetValue(firearm) * 100f)
                        );
                    }
                }

                if (explosion != null)
                {
                    if (Base.whitelistWeapons.Count != 0 && !Base.whitelistWeapons.Any(x =>
                    {
                        ItemType item;
                        if (ExplosionDic.TryGetValue(explosion.ExplosionType, out item))
                            return item == x.ItemType.GetValue(args, ItemType.None);

                        return false;
                    }))
                    {
                        return false;
                    }
                }
            }

            CheckDead(attacker, damage);
            return true;
        }

        public override void OnProjectileExploded(ProjectileExplodedEventArgs ev)
        {
            if (!IsAlive || !Active)
                return;

            FunctionArgument args = new FunctionArgument(this, ev.Player);
            if (Base.whitelistWeapons.Count != 0 &&
                Base.whitelistWeapons.Find(x => x.CustomItemId.GetValue(args, 0) == 0 && x.ItemType.GetValue(args, ItemType.None) == ItemType.GrenadeHE) == null)
            {
                return;
            }

            if (ev.TimedGrenade.Type != ItemType.GrenadeHE)
                return;

            float distance = Vector3.Distance(this.transform.position, ev.Position);
            float damage = GetGrenadeDamageAtDistance(distance);
            if (damage <= 0f)
                return;

            damage = BodyArmorUtils.ProcessDamage(Base.ArmorEfficient.GetValue(args, 0), damage, 50);
            CheckDead(ev.Player, damage);
        }

        public override void CheckDead(Player player, float damage)
        {
            Health -= damage;

            if (player != null && player.ReferenceHub != null)
            {
                Hitmarker.SendHitmarkerDirectly(player.ReferenceHub, damage / 10f);
            }

            if (Health <= 0)
            {
                IsAlive = false;
                FunctionArgument args = new FunctionArgument(this, player);

                MEC.Timing.CallDelayed(Base.DeadActionDelay.GetValue(args, 0f), () =>
                {
                    var deadTypeExecutors = new Dictionary<DeadType, Action>
                    {
                        { DeadType.Disappear, () => SmartDestroy(0.1f) },
                        {
                            DeadType.GetRigidbody, () =>
                            {
                                MakeNonStatic(gameObject);
                                if (this.gameObject.GetComponent<Rigidbody>() == null)
                                    this.gameObject.AddComponent<Rigidbody>();
                            }
                        },
                        {
                            DeadType.DynamicDisappearing, () =>
                            {
                                MakeNonStatic(gameObject);
                                _startShrinking = true;
                            }
                        },
                        { DeadType.Explode, () => FExplodeModule.Execute(Base.ExplodeModules, args) },
                        {
                            DeadType.ResetHP, () =>
                            {
                                float rHealth = Base.ResetHPTo.GetValue(args, 0f);
                                Health = rHealth == 0 ? Base.Health.GetValue(args, 100f) : rHealth;
                                IsAlive = true;
                            }
                        },
                        { DeadType.PlayAnimation, () => FAnimationDTO.Execute(Base.AnimationModules, args) },
                        { DeadType.Warhead, () => AlphaWarhead(Base.warheadActionType.GetValue<WarheadActionType>(args, 0)) },
                        { DeadType.SendMessage, () => FMessageModule.Execute(Base.MessageModules, args) },
                        { DeadType.DropItems, () => FDropItem.Execute(Base.dropItems, args) },
                        { DeadType.SendCommand, () => FCommanding.Execute(Base.commandings, args) },
                        { DeadType.GiveEffect, () => FEffectGivingModule.Execute(Base.effectGivingModules, args) },
                        { DeadType.PlayAudio, () => FAudioModule.Execute(Base.AudioModules, args) },
                        { DeadType.CallGroovieNoise, () => FCGNModule.Execute(Base.GroovieNoiseToCall, args) },
                        { DeadType.CallFunction, () => FCFEModule.Execute(Base.FunctionToCall, args) },
                        { DeadType.ModifyPrimitive, () => FPrimitiveModifyModule.Execute(Base.PrimitiveModifyModules, args) },
                        { DeadType.ControlSpeaker, () => FLoopSpeakerControlModule.Execute(Base.LoopSpeakerModules, args) },
                        { DeadType.ControlItemSpawner, () => FItemSpawnerControlModule.Execute(Base.ItemSpawnerModules, args) },
                    };

                    foreach (DeadType type in Enum.GetValues(typeof(DeadType)))
                    {
                        if (Base.DeadType.HasFlag(type) && deadTypeExecutors.TryGetValue(type, out var execute))
                        {
                            execute();
                        }
                    }
                });
            }
        }
    }
}
