using LabApi.Events;
using LabApi.Features.Wrappers;
using InventorySystem.Items.Pickups;
using ProjectMER.Features.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ZAMERT.Events.Arguments;

namespace ZAMERT
{
    public static class ZAMERTApi
    {
        public static event LabEventHandler<HealthObjectTakingDamageEventArgs> HealthObjectTakingDamage;

        public static event LabEventHandler<HealthObjectDiedEventArgs> HealthObjectDied;

        public static event LabEventHandler<InteractableObjectInteractedEventArgs> InteractableObjectInteracted;

        internal static void OnHealthObjectTakingDamage(HealthObjectTakingDamageEventArgs ev)
            => HealthObjectTakingDamage.InvokeEvent(ev);

        internal static void OnHealthObjectDied(HealthObjectDiedEventArgs ev)
            => HealthObjectDied.InvokeEvent(ev);

        internal static void OnInteractableObjectInteracted(InteractableObjectInteractedEventArgs ev)
            => InteractableObjectInteracted.InvokeEvent(ev);

        public static IReadOnlyCollection<HealthObject> GetHealthObjects()
            => ScpHealthObjectCombat.HealthObjects;

        public static IReadOnlyCollection<InteractableObject> GetInteractableObjects()
            => ZAMERTPlugin.Singleton?.InteractableObjects?.AsReadOnly() ?? new List<InteractableObject>().AsReadOnly();

        public static IReadOnlyCollection<InteractablePickup> GetInteractablePickups()
            => ZAMERTPlugin.Singleton?.InteractablePickups?.AsReadOnly() ?? new List<InteractablePickup>().AsReadOnly();

        public static IReadOnlyCollection<CustomInteractableToy> GetCustomInteractableToys()
            => ZAMERTPlugin.Singleton?.CustomInteractableToys?.AsReadOnly() ?? new List<CustomInteractableToy>().AsReadOnly();

        public static IReadOnlyCollection<CustomCollider> GetCustomColliders()
            => ZAMERTPlugin.Singleton?.CustomColliders?.AsReadOnly() ?? new List<CustomCollider>().AsReadOnly();

        public static IReadOnlyCollection<LoopSpeaker> GetLoopSpeakers()
            => ZAMERTPlugin.Singleton?.LoopSpeakers?.AsReadOnly() ?? new List<LoopSpeaker>().AsReadOnly();

        public static IReadOnlyCollection<ItemSpawner> GetItemSpawners()
            => ZAMERTPlugin.Singleton?.ItemSpawners?.AsReadOnly() ?? new List<ItemSpawner>().AsReadOnly();

        public static IReadOnlyCollection<CustomDoor> GetCustomDoors()
            => ZAMERTPlugin.Singleton?.CustomDoors?.AsReadOnly() ?? new List<CustomDoor>().AsReadOnly();

        public static IReadOnlyCollection<DamageTrigger> GetDamageTriggers()
            => ZAMERTPlugin.Singleton?.DamageTriggers?.AsReadOnly() ?? new List<DamageTrigger>().AsReadOnly();

        public static IReadOnlyCollection<PlayerCountTrigger> GetPlayerCountTriggers()
            => ZAMERTPlugin.Singleton?.PlayerCountTriggers?.AsReadOnly() ?? new List<PlayerCountTrigger>().AsReadOnly();

        public static IReadOnlyCollection<PrefabAnchor> GetPrefabAnchors()
            => ZAMERTPlugin.Singleton?.PrefabAnchors?.AsReadOnly() ?? new List<PrefabAnchor>().AsReadOnly();

        public static HealthObject FindNearestHealthObject(Vector3 position, float maxDistance)
            => ScpHealthObjectCombat.FindNearestHealthObject(position, maxDistance);

        public static bool TryDamageHealthObject(HealthObject healthObject, Player attacker, float damage)
            => ScpHealthObjectCombat.TryDamageHealthObject(healthObject, attacker, damage);

        public static bool TryRunInteractableObject(InteractableObject interactable, Player player, string toyId = "API")
        {
            if (interactable == null || player == null || !interactable.Active)
                return false;

            try
            {
                interactable.RunProcess(player, toyId);
                return true;
            }
            catch (Exception ex)
            {
                ZAMERTLogger.Warn("ZAMERTApi.TryRunInteractableObject failed: " + ex.Message);
                return false;
            }
        }

        public static bool TryRunInteractablePickup(InteractablePickup interactable, Player player, Pickup pickup, out bool remove, string eventName = "api")
        {
            remove = false;
            if (interactable == null || player == null || pickup == null || !interactable.Active)
                return false;

            try
            {
                interactable.RunProcess(player, pickup, out remove, eventName);
                return true;
            }
            catch (Exception ex)
            {
                ZAMERTLogger.Warn("ZAMERTApi.TryRunInteractablePickup failed: " + ex.Message);
                return false;
            }
        }

        public static bool TryRunCustomCollider(CustomCollider collider, Collider unityCollider, string eventName = "api")
        {
            if (collider == null || unityCollider == null || !collider.Active)
                return false;

            try
            {
                collider.RunProcess(unityCollider, eventName);
                return true;
            }
            catch (Exception ex)
            {
                ZAMERTLogger.Warn("ZAMERTApi.TryRunCustomCollider failed: " + ex.Message);
                return false;
            }
        }

        public static bool TryGetInteractableByCode<T>(SchematicObject schematic, int code, out T interactable)
            where T : ZAMERTInteractable
        {
            interactable = null;
            if (schematic == null || ZAMERTPlugin.Singleton == null)
                return false;

            if (!ZAMERTPlugin.Singleton.CodeClassPair.TryGetValue(schematic, out Dictionary<int, ZAMERTInteractable> codeDict))
                return false;

            if (!codeDict.TryGetValue(code, out ZAMERTInteractable value))
                return false;

            interactable = value as T;
            return interactable != null;
        }

        public static IReadOnlyCollection<T> GetInteractablesByGroup<T>(SchematicObject schematic, string group)
            where T : ZAMERTInteractable
        {
            if (schematic == null || string.IsNullOrWhiteSpace(group) || ZAMERTPlugin.Singleton == null)
                return new List<T>().AsReadOnly();

            if (!ZAMERTPlugin.Singleton.ZAMERTGroup.TryGetValue(schematic, out Dictionary<string, List<ZAMERTInteractable>> groupDict))
                return new List<T>().AsReadOnly();

            if (!groupDict.TryGetValue(group, out List<ZAMERTInteractable> values))
                return new List<T>().AsReadOnly();

            return values.OfType<T>().ToList().AsReadOnly();
        }

        public static bool SetInteractableActive(ZAMERTInteractable interactable, bool active)
        {
            if (interactable == null)
                return false;

            interactable.Active = active;
            if (interactable.Base != null)
                interactable.Base.Active = active;
            return true;
        }

        public static bool IsRoleIgnored(Player player, IEnumerable<PlayerRoles.RoleTypeId> ignoredRoles)
            => ZAMERTInteractable.IsRoleIgnored(player, ignoredRoles);

        public static LuaExecutionContext ExecuteLuaEvent(ZAMERTInteractable interactable, string eventName, LuaExecutionContext context = null)
            => LuaScriptService.ExecuteEvent(interactable, eventName, context);

        public static IReadOnlyCollection<string> GetRegisteredPrefabNames()
            => ZamertPrefabRegistry.PrefabNames;

        public static GameObject SpawnRegisteredPrefab(string prefabName, Vector3 position, Quaternion rotation, Vector3 scale)
            => ZamertPrefabRegistry.SpawnPrefab(prefabName, position, rotation, scale);
    }
}
