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

            RaycastHit hit;
            if (!Physics.Raycast(sender.PlayerCameraReference.position, sender.PlayerCameraReference.forward, out hit, 1000f, 1))
                return;

            Player player = Player.Get(sender);
            if (player == null) return;

            foreach (InteractableObject interactable in hit.collider.GetComponentsInParent<InteractableObject>())
            {
                if (interactable is FInteractableObject) continue;
                if (interactable.Base.InputKeyCode == key && hit.distance <= interactable.Base.InteractionMaxRange)
                    interactable.RunProcess(player);
            }
            foreach (FInteractableObject interactable in hit.collider.GetComponentsInParent<FInteractableObject>())
            {
                if (interactable.Base.InputKeyCode == key
                    && hit.distance <= interactable.Base.InteractionMaxRange.GetValue(new FunctionArgument(interactable), 0f))
                    interactable.RunProcess(player);
            }
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
            DataLoad<IODTO, InteractableObject>("Objects", ev);
            DataLoad<LSDTO, LoopSpeaker>("LoopSpeakers", ev);
            DataLoad<ISDTO, ItemSpawner>("ItemSpawners", ev);
            DataLoad<DTTDTO, DamageTrigger>("DamageTriggers", ev);
            DataLoad<PCTDTO, PlayerCountTrigger>("PlayerCountTriggers", ev);
            DataLoad<FPCTDTO, FPlayerCountTrigger>("FPlayerCountTriggers", ev);

            DataLoad<FGNDTO, FGroovyNoise>("FGroovyNoises", ev);
            DataLoad<FHODTO, FHealthObject>("FHealthObjects", ev);
            DataLoad<FIPDTO, FInteractablePickup>("FPickups", ev);
            DataLoad<FCCDTO, FCustomCollider>("FColliders", ev);
            DataLoad<FIODTO, FInteractableObject>("FObjects", ev);

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
            ServerSettings.RegisterSettings();

            foreach (Player p in Player.List)
            {
                if (p?.ReferenceHub == null)
                    continue;

                ServerSpecificSettingsSync.SendToPlayer(p.ReferenceHub);
            }
        }

        public override void OnServerMapGenerated(MapGeneratedEventArgs ev)
        {
            ZAMERTPlugin.Singleton.HealthObjects.Clear();
            ZAMERTPlugin.Singleton.InteractablePickups.Clear();
            ZAMERTPlugin.Singleton.DummyDoors.Clear();
            ZAMERTPlugin.Singleton.DummyGates.Clear();
            ZAMERTPlugin.Singleton.CustomColliders.Clear();
            ZAMERTPlugin.Singleton.GroovyNoises.Clear();
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
            List<InteractablePickup> list = ZAMERTPlugin.Singleton.InteractablePickups.FindAll(x => x.Pickup == ev.Pickup);
            List<Pickup> removeList = new List<Pickup>();

            foreach (InteractablePickup interactable in list)
            {
                if (interactable is FInteractablePickup) continue;
                if (!interactable.Base.InvokeType.HasFlag(InvokeType.Searching)) continue;
                Log.Debug("Player " + ev.Player.Nickname + " searching InteractablePickup " + interactable.OSchematic.Name);
                interactable.RunProcess(ev.Player, ev.Pickup, out bool remove);
                if (interactable.Base.CancelActionWhenActive) ev.IsAllowed = false;
                if (remove && !removeList.Contains(interactable.Pickup)) removeList.Add(interactable.Pickup);
            }
            foreach (FInteractablePickup interactable in list.FindAll(x => x is FInteractablePickup).ConvertAll(x => (FInteractablePickup)x))
            {
                if (!interactable.Base.InvokeType.HasFlag(InvokeType.Searching)) continue;
                interactable.RunProcess(ev.Player, ev.Pickup, out bool remove);
                if (interactable.Base.CancelActionWhenActive.GetValue(new FunctionArgument(interactable, ev.Player), true))
                    ev.IsAllowed = false;
                if (remove && !removeList.Contains(interactable.Pickup)) removeList.Add(interactable.Pickup);
            }
            removeList.ForEach(x => x.Destroy());
            ZAMERTPlugin.Singleton.DummyGates.ForEach(x => x.OnSearchingPickup(ev));
        }

        public override void OnPlayerPickingUpItem(PlayerPickingUpItemEventArgs ev)
        {
            List<InteractablePickup> list = ZAMERTPlugin.Singleton.InteractablePickups.FindAll(x => x.Pickup == ev.Pickup);
            List<Pickup> removeList = new List<Pickup>();

            foreach (InteractablePickup interactable in list)
            {
                if (interactable is FInteractablePickup) continue;
                if (!interactable.Base.InvokeType.HasFlag(InvokeType.Picked)) continue;
                Log.Debug("Player " + ev.Player.Nickname + " picking up InteractablePickup " + interactable.OSchematic.Name);
                interactable.RunProcess(ev.Player, ev.Pickup, out bool remove);
                if (remove && !removeList.Contains(interactable.Pickup)) removeList.Add(interactable.Pickup);
            }
            foreach (FInteractablePickup interactable in list.FindAll(x => x is FInteractablePickup).ConvertAll(x => (FInteractablePickup)x))
            {
                if (!interactable.Base.InvokeType.HasFlag(InvokeType.Picked)) continue;
                interactable.RunProcess(ev.Player, ev.Pickup, out bool remove);
                if (remove && !removeList.Contains(interactable.Pickup)) removeList.Add(interactable.Pickup);
            }
            removeList.ForEach(x => x.Destroy());
        }

        public override void OnPlayerSpawned(PlayerSpawnedEventArgs ev)
        {
            if (ev.Player?.ReferenceHub != null)
                ServerSpecificSettingsSync.SendToPlayer(ev.Player.ReferenceHub);

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
                        Transform target = FindObjectWithPath(ev.Schematic.transform, dto.ObjectId);
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
    }
}
