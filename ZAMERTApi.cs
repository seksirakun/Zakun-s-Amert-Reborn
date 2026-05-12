using LabApi.Events;
using LabApi.Features.Wrappers;
using System.Collections.Generic;
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

        public static IReadOnlyCollection<PlayerCountTrigger> GetPlayerCountTriggers()
            => ZAMERTPlugin.Singleton?.PlayerCountTriggers?.AsReadOnly() ?? new List<PlayerCountTrigger>().AsReadOnly();

        public static IReadOnlyCollection<PrefabAnchor> GetPrefabAnchors()
            => ZAMERTPlugin.Singleton?.PrefabAnchors?.AsReadOnly() ?? new List<PrefabAnchor>().AsReadOnly();

        public static HealthObject FindNearestHealthObject(Vector3 position, float maxDistance)
            => ScpHealthObjectCombat.FindNearestHealthObject(position, maxDistance);

        public static bool TryDamageHealthObject(HealthObject healthObject, Player attacker, float damage)
            => ScpHealthObjectCombat.TryDamageHealthObject(healthObject, attacker, damage);

        public static LuaExecutionContext ExecuteLuaEvent(ZAMERTInteractable interactable, string eventName, LuaExecutionContext context = null)
            => LuaScriptService.ExecuteEvent(interactable, eventName, context);

        public static IReadOnlyCollection<string> GetRegisteredPrefabNames()
            => ZamertPrefabRegistry.PrefabNames;

        public static GameObject SpawnRegisteredPrefab(string prefabName, Vector3 position, Quaternion rotation, Vector3 scale)
            => ZamertPrefabRegistry.SpawnPrefab(prefabName, position, rotation, scale);
    }
}
