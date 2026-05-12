using HarmonyLib;
using LabApi.Events.Arguments.Scp096Events;
using LabApi.Events.Arguments.Scp173Events;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using Mirror;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp049;
using PlayerRoles.PlayableScps.Scp049.Zombies;
using PlayerRoles.PlayableScps.Scp096;
using PlayerRoles.PlayableScps.Scp173;
using PlayerRoles.PlayableScps.Scp939;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ZAMERT
{
    internal static class ScpHealthObjectCombat
    {
        private static readonly CachedLayerMask HitMask = new CachedLayerMask("Default", "Door", "Glass");

        private static readonly Dictionary<int, bool> Scp096EnrageState = new Dictionary<int, bool>();
        private static readonly Dictionary<int, HashSet<int>> Scp173Observers = new Dictionary<int, HashSet<int>>();

        private const float MeleeRange = 5f;
        private const float Scp173TeleportDamageRadius = 5f;
        private const float Scp173TeleportDamage = 100f;

        private static readonly PropertyInfo CanTriggerAbilityProperty = AccessTools.Property(typeof(Scp939ClawAbility), "CanTriggerAbility");
        private static readonly FieldInfo Scp939FocusAbilityField = AccessTools.Field(typeof(Scp939ClawAbility), "_focusAbility");
        private static readonly PropertyInfo Scp096AttackPossibleProperty = AccessTools.Property(typeof(Scp096AttackAbility), "AttackPossible");
        private static readonly FieldInfo Scp096HumanDamageField = AccessTools.Field(typeof(Scp096AttackAbility), "HumanDamage");
        private static readonly PropertyInfo Scp049CanTriggerAbilityProperty = AccessTools.Property(typeof(Scp049AttackAbility), "CanTriggerAbility");
        private static readonly PropertyInfo Scp049DamageAmountProperty = AccessTools.Property(typeof(Scp049AttackAbility), "DamageAmount");
        private static readonly PropertyInfo ZombieCanTriggerAbilityProperty = AccessTools.Property(typeof(ZombieAttackAbility), "CanTriggerAbility");
        private static readonly PropertyInfo ZombieDamageAmountProperty = AccessTools.Property(typeof(ZombieAttackAbility), "DamageAmount");

        private sealed class PatchMethodCandidate
        {
            public PatchMethodCandidate(string methodName, params Type[] argumentTypes)
            {
                MethodName = methodName;
                ArgumentTypes = argumentTypes ?? Type.EmptyTypes;
            }

            public string MethodName { get; }
            public Type[] ArgumentTypes { get; }
        }

        internal static void Register()
        {
            Scp096Events.ChangedState += OnScp096ChangedState;
            Scp173Events.AddedObserver += OnScp173AddedObserver;
            Scp173Events.RemovedObserver += OnScp173RemovedObserver;
            Scp173Events.Teleported += OnScp173Teleported;
        }

        internal static void ApplyHarmonyPatches(Harmony harmony)
        {
            if (harmony == null)
                return;

            PatchOptionalPrefix(harmony, typeof(Scp939ClawAbility), typeof(Scp939ClawAbilityPatch), "Prefix",
                new PatchMethodCandidate("ServerProcessCmd", typeof(NetworkReader)));
            PatchOptionalPrefix(harmony, typeof(Scp096AttackAbility), typeof(Scp096AttackAbilityPatch), "Prefix",
                new PatchMethodCandidate("ServerProcessCmd", typeof(NetworkReader)));
            PatchOptionalPrefix(harmony, typeof(Scp049AttackAbility), typeof(Scp049AttackAbilityPatch), "Prefix",
                new PatchMethodCandidate("ServerProcessCmd", typeof(NetworkReader)));
            PatchOptionalPrefix(harmony, typeof(ZombieAttackAbility), typeof(ZombieAttackAbilityPatch), "Prefix",
                new PatchMethodCandidate("ServerProcessCmd", typeof(NetworkReader)));
        }

        internal static void Unregister()
        {
            Scp096Events.ChangedState -= OnScp096ChangedState;
            Scp173Events.AddedObserver -= OnScp173AddedObserver;
            Scp173Events.RemovedObserver -= OnScp173RemovedObserver;
            Scp173Events.Teleported -= OnScp173Teleported;

            Scp096EnrageState.Clear();
            Scp173Observers.Clear();
        }

        internal static IReadOnlyCollection<HealthObject> HealthObjects =>
            ZAMERTPlugin.Singleton?.HealthObjects ?? new List<HealthObject>();

        internal static HealthObject FindNearestHealthObject(Vector3 position, float maxDistance)
        {
            HealthObject nearest = null;
            float nearestSqrDistance = maxDistance * maxDistance;

            foreach (HealthObject healthObject in HealthObjects)
            {
                if (healthObject == null || !healthObject.Active || !healthObject.IsAlive)
                    continue;

                float sqrDistance = (healthObject.transform.position - position).sqrMagnitude;
                if (sqrDistance > nearestSqrDistance)
                    continue;

                nearest = healthObject;
                nearestSqrDistance = sqrDistance;
            }

            return nearest;
        }

        internal static bool TryDamageHealthObject(HealthObject healthObject, Player attacker, float damage)
        {
            if (healthObject == null || attacker == null || !healthObject.Active || !healthObject.IsAlive || damage <= 0f)
                return false;

            try
            {
                healthObject.CheckDead(attacker, damage);
                return true;
            }
            catch (Exception ex)
            {
                ZAMERTLogger.Error("SCP HealthObject damage failed for " + attacker.Nickname + " -> " + healthObject.gameObject.name + ": " + ex);
                return false;
            }
        }

        internal static bool TryDamageLookedAtHealthObject(Player attacker, Vector3 rayOrigin, Vector3 rayDirection, float maxDistance, float damage)
        {
            if (attacker == null || damage <= 0f)
                return false;

            if (!Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hitInfo, maxDistance, HitMask))
                return false;

            HealthObject healthObject = ResolveHealthObject(hitInfo.collider);
            return TryDamageHealthObject(healthObject, attacker, damage);
        }

        private static HealthObject ResolveHealthObject(Collider collider)
        {
            if (collider == null)
                return null;

            Healther healther = collider.GetComponentInParent<Healther>();
            if (healther != null)
            {
                return healther.Parents.FirstOrDefault(x => x != null && x.Active && x.IsAlive);
            }

            return collider.GetComponentInParent<HealthObject>();
        }

        private static void OnScp096ChangedState(Scp096ChangedStateEventArgs ev)
        {
            if (ev.Player == null)
                return;

            Scp096EnrageState[ev.Player.PlayerId] = ev.State == Scp096RageState.Enraged;
        }

        private static void OnScp173AddedObserver(Scp173AddedObserverEventArgs ev)
        {
            if (ev.Player == null || ev.Target == null)
                return;

            AddObserverLink(ev.Player.PlayerId, ev.Target.PlayerId);
            AddObserverLink(ev.Target.PlayerId, ev.Player.PlayerId);
        }

        private static void AddObserverLink(int sourceId, int targetId)
        {
            if (!Scp173Observers.TryGetValue(sourceId, out HashSet<int> observers))
            {
                observers = new HashSet<int>();
                Scp173Observers[sourceId] = observers;
            }

            observers.Add(targetId);
        }

        private static void OnScp173RemovedObserver(Scp173RemovedObserverEventArgs ev)
        {
            if (ev.Player == null || ev.Target == null)
                return;

            RemoveObserverLink(ev.Player.PlayerId, ev.Target.PlayerId);
            RemoveObserverLink(ev.Target.PlayerId, ev.Player.PlayerId);
        }

        private static void RemoveObserverLink(int sourceId, int targetId)
        {
            if (!Scp173Observers.TryGetValue(sourceId, out HashSet<int> observers))
                return;

            observers.Remove(targetId);
            if (observers.Count == 0)
                Scp173Observers.Remove(sourceId);
        }

        private static void OnScp173Teleported(Scp173TeleportedEventArgs ev)
        {
            if (ev.Player == null)
                return;

            Vector3 position = ev.Player.Position;
            HealthObject nearest = FindNearestHealthObject(position, Scp173TeleportDamageRadius);
            if (nearest == null)
                return;

            TryDamageHealthObject(nearest, ev.Player, Scp173TeleportDamage);
        }

        private static bool GetBool(PropertyInfo property, object instance, bool fallback = false)
        {
            if (property == null || instance == null)
                return fallback;

            object value = property.GetValue(instance, null);
            return value is bool flag ? flag : fallback;
        }

        private static float GetFloat(PropertyInfo property, object instance, float fallback = 0f)
        {
            if (property == null || instance == null)
                return fallback;

            object value = property.GetValue(instance, null);
            return value is float amount ? amount : fallback;
        }

        private static float GetStaticFloat(FieldInfo field, float fallback = 0f)
        {
            if (field == null)
                return fallback;

            object value = field.GetValue(null);
            return value is float amount ? amount : fallback;
        }

        private static bool IsScp939ClawReady(Scp939ClawAbility ability)
        {
            if (ability == null || !GetBool(CanTriggerAbilityProperty, ability))
                return false;

            if (Scp939FocusAbilityField == null)
                return true;

            object focusAbility = Scp939FocusAbilityField.GetValue(ability);
            if (focusAbility == null)
                return true;

            PropertyInfo stateProperty = AccessTools.Property(focusAbility.GetType(), "State");
            if (stateProperty == null)
                return true;

            object state = stateProperty.GetValue(focusAbility, null);
            return !(state is float value) || Mathf.Approximately(value, 0f);
        }

        private static void PatchOptionalPrefix(Harmony harmony, Type targetType, Type patchType, string patchMethodName, params PatchMethodCandidate[] candidates)
        {
            MethodInfo reflectedTargetMethod = null;
            PatchMethodCandidate resolvedCandidate = null;

            foreach (PatchMethodCandidate candidate in candidates ?? Array.Empty<PatchMethodCandidate>())
            {
                reflectedTargetMethod = AccessTools.Method(targetType, candidate.MethodName, candidate.ArgumentTypes);
                if (reflectedTargetMethod != null)
                {
                    resolvedCandidate = candidate;
                    break;
                }
            }

            if (reflectedTargetMethod == null)
            {
                ZAMERTLogger.Warn("Skipping SCP combat patch for " + targetType.FullName + " because no supported target method was found.");
                return;
            }

            MethodInfo targetMethod = ResolveImplementedMethod(reflectedTargetMethod);
            if (targetMethod == null)
            {
                ZAMERTLogger.Warn("Skipping SCP combat patch for " + targetType.FullName + "." + resolvedCandidate.MethodName + " because the implemented target method could not be resolved.");
                return;
            }

            MethodInfo prefixMethod = AccessTools.Method(patchType, patchMethodName);
            if (prefixMethod == null)
            {
                ZAMERTLogger.Error("Failed to apply SCP combat patch for " + targetType.FullName + "." + resolvedCandidate.MethodName + " because prefix method " + patchType.FullName + "." + patchMethodName + " was not found.");
                return;
            }

            try
            {
                harmony.Patch(targetMethod, prefix: new HarmonyMethod(prefixMethod));
            }
            catch (Exception ex)
            {
                ZAMERTLogger.Error("Failed to apply SCP combat patch for " + targetType.FullName + "." + resolvedCandidate.MethodName + ": " + ex);
            }
        }

        private static MethodInfo ResolveImplementedMethod(MethodInfo reflectedMethod)
        {
            if (reflectedMethod == null)
                return null;

            System.Type declaringType = reflectedMethod.DeclaringType;
            if (declaringType == null)
                return reflectedMethod;

            MethodInfo declaredMethod = declaringType.GetMethod(
                reflectedMethod.Name,
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                reflectedMethod.GetParameters().Select(x => x.ParameterType).ToArray(),
                null);

            return declaredMethod ?? reflectedMethod;
        }

        private static bool TryGetAttackContext(Player attacker, Transform cameraReference, float damage, out Vector3 rayOrigin, out Vector3 rayDirection)
        {
            rayOrigin = Vector3.zero;
            rayDirection = Vector3.forward;

            if (attacker == null || cameraReference == null || damage <= 0f)
                return false;

            rayDirection = cameraReference.forward;
            rayOrigin = cameraReference.position + rayDirection;
            return true;
        }

        private static void runwithoutcrashes(string context, Action action)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                ZAMERTLogger.Error("SCP combat hook failed at " + context + ": " + ex);
            }
        }

        private static class Scp939ClawAbilityPatch
        {
            private static void Prefix(object __instance, NetworkReader reader)
            {
                runwithoutcrashes("Scp939ClawAbility", () =>
                {
                    Scp939ClawAbility ability = __instance as Scp939ClawAbility;
                    if (!IsScp939ClawReady(ability))
                        return;

                    Player attacker = Player.Get(ability.Owner);
                    if (!TryGetAttackContext(attacker, ability.Owner?.PlayerCameraReference, ability.DamageAmount, out Vector3 rayOrigin, out Vector3 rayDirection))
                        return;

                    TryDamageLookedAtHealthObject(attacker, rayOrigin, rayDirection, MeleeRange, ability.DamageAmount);
                });
            }
        }

        private static class Scp096AttackAbilityPatch
        {
            private static void Prefix(object __instance)
            {
                runwithoutcrashes("Scp096AttackAbility", () =>
                {
                    Scp096AttackAbility ability = __instance as Scp096AttackAbility;
                    if (ability == null || !GetBool(Scp096AttackPossibleProperty, ability))
                        return;

                    Player attacker = Player.Get(ability.Owner);
                    if (attacker == null)
                        return;

                    if (!Scp096EnrageState.TryGetValue(attacker.PlayerId, out bool enraged) || !enraged)
                        return;

                    float damage = GetStaticFloat(Scp096HumanDamageField, 40f);
                    if (!TryGetAttackContext(attacker, ability.Owner?.PlayerCameraReference, damage, out Vector3 rayOrigin, out Vector3 rayDirection))
                        return;

                    TryDamageLookedAtHealthObject(attacker, rayOrigin, rayDirection, MeleeRange, damage);
                });
            }
        }

        private static class Scp049AttackAbilityPatch
        {
            private static void Prefix(object __instance)
            {
                runwithoutcrashes("Scp049AttackAbility", () =>
                {
                    Scp049AttackAbility ability = __instance as Scp049AttackAbility;
                    if (ability == null || !GetBool(Scp049CanTriggerAbilityProperty, ability, true))
                        return;

                    Player attacker = Player.Get(ability.Owner);
                    float damage = GetFloat(Scp049DamageAmountProperty, ability, 40f);
                    if (!TryGetAttackContext(attacker, ability.Owner?.PlayerCameraReference, damage, out Vector3 rayOrigin, out Vector3 rayDirection))
                        return;

                    TryDamageLookedAtHealthObject(attacker, rayOrigin, rayDirection, MeleeRange, damage);
                });
            }
        }

        private static class ZombieAttackAbilityPatch
        {
            private static void Prefix(object __instance)
            {
                runwithoutcrashes("ZombieAttackAbility", () =>
                {
                    ZombieAttackAbility ability = __instance as ZombieAttackAbility;
                    if (ability == null || !GetBool(ZombieCanTriggerAbilityProperty, ability))
                        return;

                    Player attacker = Player.Get(ability.Owner);
                    if (attacker == null || attacker.Role != RoleTypeId.Scp0492)
                        return;

                    float damage = GetFloat(ZombieDamageAmountProperty, ability, 40f);
                    if (!TryGetAttackContext(attacker, ability.Owner?.PlayerCameraReference, damage, out Vector3 rayOrigin, out Vector3 rayDirection))
                        return;

                    TryDamageLookedAtHealthObject(attacker, rayOrigin, rayDirection, MeleeRange, damage);
                });
            }
        }
    }
}
