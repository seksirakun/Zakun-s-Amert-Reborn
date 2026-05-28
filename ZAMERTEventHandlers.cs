using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.CustomHandlers;
using LabApi.Features.Wrappers;
using Mirror;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using PlayerRoles;
using ProjectMER.Events.Arguments;
using ProjectMER.Features;
using ProjectMER.Features.Serializable;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UserSettings.ServerSpecific;
using Log = ZAMERT.ZAMERTLogger;

namespace ZAMERT
{
    public class ZAMERTEventHandlers : CustomEventsHandler
    {
        private const float DefaultInteractionRange = 5f;
        private const float InteractionTraceDistance = 1000f;

        public Config Config => ZAMERTPlugin.Singleton.Config;

        public static List<NetworkIdentity> Identities { get; set; } = new List<NetworkIdentity>();

        public static DataSerializationBinder DSbinder { get; set; } = new DataSerializationBinder();

        public class DataSerializationBinder : ISerializationBinder
        {
            private Dictionary<string, Type> _types;
            public DefaultSerializationBinder DefaultBinder { get; } = new DefaultSerializationBinder();

            private Dictionary<string, Type> GetTypes()
            {
                if (_types == null)
                {
                    _types = Assembly.GetAssembly(typeof(ZAMERTPlugin))
                        .GetTypes()
                        .Where(t => t.IsClass && !t.IsAbstract
                            && (typeof(Value).IsAssignableFrom(t) || typeof(Function).IsAssignableFrom(t)))
                        .ToDictionary(x => x.Name);
                }
                return _types;
            }

            void ISerializationBinder.BindToName(Type serializedType, out string assemblyName, out string typeName)
            {
                DefaultBinder.BindToName(serializedType, out assemblyName, out typeName);
            }

            Type ISerializationBinder.BindToType(string assemblyName, string typeName)
            {
                string shortName = typeName.Contains(".") ? typeName.Substring(typeName.LastIndexOf('.') + 1) : typeName;
                var types = GetTypes();
                if (types.ContainsKey(shortName))
                    return types[shortName];
                return DefaultBinder.BindToType(assemblyName, typeName);
            }
        }

        public void OnSSInput(ReferenceHub sender, ServerSpecificSettingBase setting)
        {
            if (!(setting is SSKeybindSetting ssKeybind) || !ssKeybind.SyncIsPressed) return;

            SSKeybindSetting originalDefinition = setting.OriginalDefinition as SSKeybindSetting;
            if (originalDefinition == null) return;

            int key = (int)originalDefinition.SuggestedKey;
            if (!ZAMERTPlugin.Singleton.IOkeys.ContainsKey(key)) return;
            if (!originalDefinition.Label.StartsWith(ServerSettings.IOLabelPreamble)) return;

            Player player = Player.Get(sender);
            if (player == null) return;

            Ray ray = new Ray(sender.PlayerCameraReference.position, sender.PlayerCameraReference.forward);

            if (Config.InteractableObjectDetectionMode == InteractableDetectionMode.ProximityOnly)
            {
                TryProcessNearestCandidate(player, key, ray.origin);
                return;
            }

            if (TryProcessInteractableHits(player, key, Physics.RaycastAll(ray, InteractionTraceDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide)))
                return;

            if (Config.InteractableObjectDetectionMode == InteractableDetectionMode.RaycastOnly)
                return;

            float assistRadius = Mathf.Max(0f, Config.InteractableObjectAssistSphereRadius);
            if (assistRadius > 0f && TryProcessInteractableHits(player, key, Physics.SphereCastAll(ray, assistRadius, InteractionTraceDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide)))
                return;

            TryProcessBestAimCandidate(player, key, ray.origin, ray.direction);
        }

        private bool TryProcessInteractableHits(Player player, int key, RaycastHit[] hits)
        {
            if (hits == null || hits.Length == 0)
                return false;

            foreach (RaycastHit hit in hits.OrderBy(x => x.distance))
            {
                if (hit.collider == null)
                    continue;

                if (TryProcessInteractablesOnCollider(player, key, hit.collider, hit.distance))
                    return true;
            }

            return false;
        }

        private bool TryProcessInteractablesOnCollider(Player player, int key, Collider collider, float hitDistance)
        {
            bool triggered = false;

            foreach (InteractableObject interactable in collider.GetComponentsInParent<InteractableObject>())
            {
                if (interactable is FInteractableObject)
                    continue;

                if (interactable.Base.InputKeyCode != key || hitDistance > GetInteractionRange(interactable) || !CanInteractableAcceptPlayer(interactable, player))
                    continue;

                interactable.RunProcess(player);
                triggered = true;
            }

            foreach (FInteractableObject interactable in collider.GetComponentsInParent<FInteractableObject>())
            {
                if (interactable.Base.InputKeyCode != key || hitDistance > GetInteractionRange(interactable) || !CanInteractableAcceptPlayer(interactable, player))
                    continue;

                interactable.RunProcess(player);
                triggered = true;
            }

            return triggered;
        }

        private bool TryProcessBestAimCandidate(Player player, int key, Vector3 origin, Vector3 direction)
        {
            if (!ZAMERTPlugin.Singleton.IOkeys.TryGetValue(key, out List<InteractableObject> candidates) || candidates == null || candidates.Count == 0)
                return false;

            direction = direction.normalized;

            InteractableObject bestCandidate = null;
            float bestScore = float.MaxValue;
            float bestDistance = 0f;

            foreach (InteractableObject interactable in candidates)
            {
                if (interactable == null || !interactable.Active)
                    continue;

                if (!CanInteractableAcceptPlayer(interactable, player))
                    continue;

                float range = GetInteractionRange(interactable);
                Vector3 detectionPoint = GetDetectionPoint(interactable);
                Vector3 toTarget = detectionPoint - origin;
                float distance = toTarget.magnitude;

                if (distance <= 0.01f || distance > range)
                    continue;

                float angle = Vector3.Angle(direction, toTarget);
                if (angle > Mathf.Max(0f, Config.InteractableObjectAssistAngleDegrees))
                    continue;

                if (!HasLineOfSight(interactable, origin, direction, distance))
                    continue;

                float score = angle * 8f + distance;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestCandidate = interactable;
                    bestDistance = distance;
                }
            }

            if (bestCandidate == null)
                return false;

            bestCandidate.RunProcess(player);
            Log.Debug("IO aim-assist selected interactable: " + bestCandidate.gameObject.name + " at distance " + bestDistance);
            return true;
        }

        private bool TryProcessNearestCandidate(Player player, int key, Vector3 origin)
        {
            if (!ZAMERTPlugin.Singleton.IOkeys.TryGetValue(key, out List<InteractableObject> candidates) || candidates == null || candidates.Count == 0)
                return false;

            InteractableObject bestCandidate = null;
            float bestDistance = float.MaxValue;

            foreach (InteractableObject interactable in candidates)
            {
                if (!CanInteractableAcceptPlayer(interactable, player))
                    continue;

                float range = GetInteractionRange(interactable);
                float distance = Vector3.Distance(origin, GetDetectionPoint(interactable));
                if (distance > range || distance >= bestDistance)
                    continue;

                bestCandidate = interactable;
                bestDistance = distance;
            }

            if (bestCandidate == null)
                return false;

            bestCandidate.RunProcess(player);
            Log.Debug("IO proximity selected interactable: " + bestCandidate.gameObject.name + " at distance " + bestDistance);
            return true;
        }

        private static bool CanInteractableAcceptPlayer(InteractableObject interactable, Player player)
        {
            if (interactable == null || !interactable.Active || player == null)
                return false;

            if (interactable is FInteractableObject fInteractable)
                return !ZAMERTInteractable.IsRoleIgnored(player, fInteractable.Base?.IgnoredRoles);

            return !ZAMERTInteractable.IsRoleIgnored(player, interactable.Base?.IgnoredRoles);
        }

        private static float GetInteractionRange(InteractableObject interactable)
        {
            if (interactable == null)
                return DefaultInteractionRange;

            if (interactable is FInteractableObject fInteractable)
            {
                float scriptedRange = fInteractable.Base.InteractionMaxRange.GetValue(new FunctionArgument(fInteractable), DefaultInteractionRange);
                return scriptedRange > 0f ? scriptedRange : DefaultInteractionRange;
            }

            return interactable.Base.InteractionMaxRange > 0f
                ? interactable.Base.InteractionMaxRange
                : DefaultInteractionRange;
        }

        private static Vector3 GetDetectionPoint(InteractableObject interactable)
        {
            Collider[] colliders = interactable.GetComponentsInChildren<Collider>();
            if (colliders.Length > 0)
            {
                Bounds bounds = colliders[0].bounds;
                for (int i = 1; i < colliders.Length; i++)
                    bounds.Encapsulate(colliders[i].bounds);
                return bounds.center;
            }

            Renderer[] renderers = interactable.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);
                return bounds.center;
            }

            return interactable.transform.position;
        }

        private static bool HasLineOfSight(InteractableObject interactable, Vector3 origin, Vector3 direction, float maxDistance)
        {
            RaycastHit[] hits = Physics.RaycastAll(origin, direction, maxDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            if (hits == null || hits.Length == 0)
                return false;

            foreach (RaycastHit hit in hits.OrderBy(x => x.distance))
            {
                if (hit.collider == null)
                    continue;

                if (hit.collider.GetComponentInParent<InteractableObject>() == interactable)
                    return true;
            }

            return false;
        }

        public void OnSchematicSpawned(SchematicSpawnedEventArgs ev)
        {
            ZAMERTPlugin.Singleton.SchematicVariables[ev.Schematic] = new Dictionary<string, object>();
            ZAMERTPlugin.Singleton.ZAMERTGroup[ev.Schematic] = new Dictionary<string, List<ZAMERTInteractable>>();
            ZAMERTPlugin.Singleton.CodeClassPair[ev.Schematic] = new Dictionary<int, ZAMERTInteractable>();

            if (ev.Name.Equals("Gate", StringComparison.InvariantCultureIgnoreCase))
                ev.Schematic.gameObject.AddComponent<DummyGate>();

            DataLoad<GNDTO, GroovyNoise>("GroovyNoises", ev);
            DataLoad<HODTO, HealthObject>("HealthObjects", ev);
            DataLoad<IPDTO, InteractablePickup>("Pickups", ev);
            DataLoad<CCDTO, CustomCollider>("Colliders", ev);
            DataLoad<CDDTO, CustomDoor>("Doors", ev);
            DataLoad<IODTO, InteractableObject>("Objects", ev);
            DataLoad<CITDTO, CustomInteractableToy>("ToyInteractables", ev);
            DataLoad<LSDTO, LoopSpeaker>("LoopSpeakers", ev);
            DataLoad<ISDTO, ItemSpawner>("ItemSpawners", ev);
            DataLoad<DTTDTO, DamageTrigger>("DamageTriggers", ev);
            DataLoad<PCTDTO, PlayerCountTrigger>("PlayerCountTriggers", ev);
            DataLoad<PFADTO, PrefabAnchor>("PrefabAnchors", ev);
            DataLoad<CTDTO, CustomTexture>("CustomTextures", ev);
            DataLoad<MDDTO, MapDoor>("MapDoors", ev);
            DataLoad<CLDTO, Clutter>("Clutter", ev);
            DataLoad<SCDTO, MapScp079Camera>("Scp079Cameras", ev);
            DataLoad<FPCTDTO, FPlayerCountTrigger>("FPlayerCountTriggers", ev);

            DataLoad<FGNDTO, FGroovyNoise>("FGroovyNoises", ev);
            DataLoad<FHODTO, FHealthObject>("FHealthObjects", ev);
            DataLoad<FIPDTO, FInteractablePickup>("FPickups", ev);
            DataLoad<FCCDTO, FCustomCollider>("FColliders", ev);
            DataLoad<FIODTO, FInteractableObject>("FObjects", ev);
            DataLoad<FCITDTO, FCustomInteractableToy>("FToyInteractables", ev);

            ZAMERTPlugin.Singleton.FunctionExecutors[ev.Schematic] = new Dictionary<string, FunctionExecutor>();
            string path = Path.Combine(ev.Schematic.DirectoryPath, ev.Schematic.Name + "-Functions.json");
            if (File.Exists(path))
            {
                try
                {
                    var settings = new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        TypeNameHandling = TypeNameHandling.Auto,
                        SerializationBinder = DSbinder,
                    };
                    List<FEDTO> ts = JsonConvert.DeserializeObject<List<FEDTO>>(File.ReadAllText(path), settings);
                    if (ts != null)
                    {
                        foreach (FEDTO dto in ts)
                        {
                            if (ZAMERTPlugin.Singleton.FunctionExecutors[ev.Schematic].ContainsKey(dto.FunctionName))
                            {
                                Log.Warn("Duplicate function name: " + dto.FunctionName + " in schematic " + ev.Schematic.Name);
                                continue;
                            }
                            FunctionExecutor tclass = ev.Schematic.gameObject.AddComponent<FunctionExecutor>();
                            tclass.OSchematic = ev.Schematic;
                            tclass.Data = dto;
                            dto.OSchematic = ev.Schematic;
                            ZAMERTPlugin.Singleton.FunctionExecutors[ev.Schematic][dto.FunctionName] = tclass;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Error loading Functions for schematic " + ev.Schematic.Name + ": " + ex.Message);
                }
            }
        }

        public override void OnServerRoundStarted()
        {
        }

        public override void OnServerMapGenerated(MapGeneratedEventArgs ev)
        {
            ServerSettings.CleanupSettings();

            ZAMERTPlugin.Singleton.HealthObjects.Clear();
            ZAMERTPlugin.Singleton.InteractablePickups.Clear();
            ZAMERTPlugin.Singleton.CustomInteractableToys.Clear();
            ZAMERTPlugin.Singleton.InteractableTeleporters.Clear();
            ZAMERTPlugin.Singleton.DummyDoors.Clear();
            ZAMERTPlugin.Singleton.DummyGates.Clear();
            ZAMERTPlugin.Singleton.CustomColliders.Clear();
            ZAMERTPlugin.Singleton.GroovyNoises.Clear();
            ZAMERTPlugin.Singleton.CustomDoors.Clear();
            ZAMERTPlugin.Singleton.CodeClassPair.Clear();
            ZAMERTPlugin.Singleton.InteractableObjects.Clear();
            ZAMERTPlugin.Singleton.FunctionExecutors.Clear();
            ZAMERTPlugin.Singleton.SchematicVariables.Clear();
            ZAMERTPlugin.Singleton.RoundVariable.Clear();
            ZAMERTPlugin.Singleton.IOkeys.Clear();
            ZAMERTPlugin.Singleton.LoopSpeakers.Clear();
            ZAMERTPlugin.Singleton.ItemSpawners.Clear();
            ZAMERTPlugin.Singleton.DamageTriggers.Clear();
            ZAMERTPlugin.Singleton.PlayerCountTriggers.Clear();
            ZAMERTPlugin.Singleton.PrefabAnchors.Clear();
            ZamertPrefabRegistry.Refresh();
        }

        public override void OnServerProjectileExploded(ProjectileExplodedEventArgs ev)
        {
            var list = ZAMERTPlugin.Singleton.HealthObjects;
            for (int i = 0; i < list.Count; i++)
            {
                try { list[i].OnProjectileExploded(ev); }
                catch (Exception ex) { Log.Debug("HealthObject.OnProjectileExploded error: " + ex.Message); }
            }
        }

        public override void OnPlayerSearchingPickup(PlayerSearchingPickupEventArgs ev)
        {
            List<InteractablePickup> list = ZAMERTPlugin.Singleton.InteractablePickups.FindAll(x => x != null && x.MatchesPickup(ev.Pickup));
            List<Pickup> removeList = new List<Pickup>();

            foreach (InteractablePickup interactable in list)
            {
                if (interactable is FInteractablePickup) continue;
                if (!interactable.Base.InvokeType.HasFlag(InvokeType.Searching)) continue;
                Log.Debug("Player " + ev.Player.Nickname + " searching InteractablePickup " + interactable.OSchematic.Name);
                interactable.RunProcess(ev.Player, ev.Pickup, out bool remove, LuaEventType.Searching.ToString().ToLowerInvariant());
                if (interactable.Base.CancelActionWhenActive) ev.IsAllowed = false;
                if (remove && !removeList.Contains(interactable.Pickup)) removeList.Add(interactable.Pickup);
            }
            foreach (FInteractablePickup interactable in list.FindAll(x => x is FInteractablePickup).ConvertAll(x => (FInteractablePickup)x))
            {
                if (!interactable.Base.InvokeType.HasFlag(InvokeType.Searching)) continue;
                interactable.RunProcess(ev.Player, ev.Pickup, out bool remove, LuaEventType.Searching.ToString().ToLowerInvariant());
                if (interactable.Base.CancelActionWhenActive.GetValue(new FunctionArgument(interactable, ev.Player), true))
                    ev.IsAllowed = false;
                if (remove && !removeList.Contains(interactable.Pickup)) removeList.Add(interactable.Pickup);
            }
            removeList.ForEach(x => x.Destroy());
            ZAMERTPlugin.Singleton.DummyGates.ForEach(x => x.OnSearchingPickup(ev));
        }

        public override void OnPlayerPickingUpItem(PlayerPickingUpItemEventArgs ev)
        {
            List<InteractablePickup> list = ZAMERTPlugin.Singleton.InteractablePickups.FindAll(x => x != null && x.MatchesPickup(ev.Pickup));
            List<Pickup> removeList = new List<Pickup>();

            foreach (InteractablePickup interactable in list)
            {
                if (interactable is FInteractablePickup) continue;
                if (!interactable.Base.InvokeType.HasFlag(InvokeType.Picked)) continue;
                Log.Debug("Player " + ev.Player.Nickname + " picking up InteractablePickup " + interactable.OSchematic.Name);
                interactable.RunProcess(ev.Player, ev.Pickup, out bool remove, LuaEventType.Picked.ToString().ToLowerInvariant());
                if (remove && !removeList.Contains(interactable.Pickup)) removeList.Add(interactable.Pickup);
            }
            foreach (FInteractablePickup interactable in list.FindAll(x => x is FInteractablePickup).ConvertAll(x => (FInteractablePickup)x))
            {
                if (!interactable.Base.InvokeType.HasFlag(InvokeType.Picked)) continue;
                interactable.RunProcess(ev.Player, ev.Pickup, out bool remove, LuaEventType.Picked.ToString().ToLowerInvariant());
                if (remove && !removeList.Contains(interactable.Pickup)) removeList.Add(interactable.Pickup);
            }
            removeList.ForEach(x => x.Destroy());
        }

        public override void OnPlayerSpawned(PlayerSpawnedEventArgs ev)
        {

            if (!Config.CustomSpawnPointEnable) return;
            if (MapUtils.LoadedMaps.IsEmpty()) return;

            RoleTypeId playerRoleId = ev.Player.Role;
            List<SerializablePlayerSpawnpoint> list = new List<SerializablePlayerSpawnpoint>();
            foreach (var item in MapUtils.LoadedMaps)
            {
                List<SerializablePlayerSpawnpoint> roleSpawns = item.Value.PlayerSpawnpoints.Values
                    .Where(sp => sp.Roles.Contains(playerRoleId)).ToList();
                list.AddRange(roleSpawns);
            }
            if (list.Count > 0)
            {
                SerializablePlayerSpawnpoint serializable = list.RandomItem();
                ev.Player.Position = serializable.Position + Vector3.up;
            }
        }

        public void DataLoad<Tdto, Tclass>(string name, SchematicSpawnedEventArgs ev)
            where Tdto : ZAMERTDTO
            where Tclass : ZAMERTInteractable, new()
        {
            string path = Path.Combine(ev.Schematic.DirectoryPath, ev.Schematic.Name + "-" + name + ".json");
            if (!File.Exists(path)) return;

            try
            {
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto,
                    SerializationBinder = DSbinder
                };
                List<Tdto> ts = JsonConvert.DeserializeObject<List<Tdto>>(File.ReadAllText(path), settings);
                if (ts == null) return;

                foreach (Tdto dto in ts)
                {
                    try
                    {
                        Transform target = ResolveDtoTarget(ev.Schematic.transform, dto, name, ev.Schematic.Name);
                        if (target == null)
                            continue;

                        Tclass tclass = target.gameObject.AddComponent<Tclass>();
                        tclass.Base = dto;
                        tclass.Active = dto.Active;
                        tclass.OSchematic = ev.Schematic;

                        if (ZAMERTPlugin.Singleton.CodeClassPair[ev.Schematic].ContainsKey(dto.Code))
                        {
                            Log.Warn("Duplicate code " + dto.Code + " in schematic " + ev.Schematic.Name + " for " + name);
                        }
                        else
                        {
                            ZAMERTPlugin.Singleton.CodeClassPair[ev.Schematic][dto.Code] = tclass;
                        }

                        if (!string.IsNullOrEmpty(dto.ScriptGroup))
                        {
                            if (!ZAMERTPlugin.Singleton.ZAMERTGroup[ev.Schematic].ContainsKey(dto.ScriptGroup))
                                ZAMERTPlugin.Singleton.ZAMERTGroup[ev.Schematic][dto.ScriptGroup] = new List<ZAMERTInteractable>();
                            ZAMERTPlugin.Singleton.ZAMERTGroup[ev.Schematic][dto.ScriptGroup].Add(tclass);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error loading DTO in " + name + " for schematic " + ev.Schematic.Name + ": " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error deserializing " + name + " for schematic " + ev.Schematic.Name + ": " + ex.Message);
            }
        }

        public static Transform FindObjectWithPath(Transform target, string pathO)
        {
            if (string.IsNullOrWhiteSpace(pathO)) return target;
            string[] path = pathO.Trim().Split(' ');
            for (int i = path.Length - 1; i >= 0; i--)
            {
                int childIndex;
                if (!int.TryParse(path[i], out childIndex))
                {
                    Log.Warn("Invalid child index '" + path[i] + "' in path: " + pathO);
                    break;
                }
                if (target.childCount == 0 || target.childCount <= childIndex)
                {
                    Log.Warn("Child index " + childIndex + " out of range (childCount=" + target.childCount + ") in path: " + pathO);
                    break;
                }
                target = target.GetChild(childIndex);
            }
            return target;
        }

        private static Transform ResolveDtoTarget(Transform root, ZAMERTDTO dto, string dataName, string schematicName)
        {
            if (root == null || dto == null)
                return null;

            Transform target;
            if (TryFindObjectWithNamePath(root, dto.ObjectNamePath, out target))
                return target;

            if (TryFindObjectWithPath(root, dto.ObjectId, out target))
                return target;

            target = FindObjectWithPath(root, dto.ObjectId);
            if (target != null && target != root)
            {
                Log.Warn("DTO in " + dataName + " for schematic " + schematicName + " used legacy partial ObjectId fallback: " + (dto.ObjectId ?? "null"));
                return target;
            }

            if (string.IsNullOrWhiteSpace(dto.ObjectId) && string.IsNullOrWhiteSpace(dto.ObjectNamePath))
            {
                Log.Warn("DTO in " + dataName + " for schematic " + schematicName + " has no ObjectId/ObjectNamePath, using schematic root fallback.");
                return root;
            }

            Log.Warn("Skipping DTO in " + dataName + " for schematic " + schematicName + " because target could not be resolved. ObjectId=" + (dto.ObjectId ?? "null") + ", ObjectNamePath=" + (dto.ObjectNamePath ?? "null"));
            return null;
        }

        public static bool TryFindObjectWithPath(Transform root, string pathO, out Transform target)
        {
            target = root;
            if (root == null || string.IsNullOrWhiteSpace(pathO))
                return false;

            string[] path = pathO.Trim().Split(' ');
            for (int i = path.Length - 1; i >= 0; i--)
            {
                int childIndex;
                if (!int.TryParse(path[i], out childIndex))
                {
                    target = null;
                    return false;
                }

                if (target == null || childIndex < 0 || childIndex >= target.childCount)
                {
                    target = null;
                    return false;
                }

                target = target.GetChild(childIndex);
            }

            return target != null;
        }

        public static bool TryFindObjectWithNamePath(Transform root, string namePath, out Transform target)
        {
            target = root;
            if (root == null || string.IsNullOrWhiteSpace(namePath))
                return false;

            string[] segments = namePath.Split('/');
            for (int i = 0; i < segments.Length; i++)
            {
                string segment = segments[i].Replace("\\/", "/");
                if (string.IsNullOrEmpty(segment))
                    return false;

                Transform next = null;
                for (int c = 0; c < target.childCount; c++)
                {
                    Transform child = target.GetChild(c);
                    if (string.Equals(child.name, segment, StringComparison.Ordinal))
                    {
                        next = child;
                        break;
                    }
                }

                if (next == null)
                {
                    target = null;
                    return false;
                }

                target = next;
            }

            return target != null;
        }
    }
}
