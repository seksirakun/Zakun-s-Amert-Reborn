using System.Collections.Generic;
using System.Linq;
using ProjectMER.Features.Objects;
using UnityEngine;

namespace ZAMERT
{
    public static class RegistryMaintenance
    {
        public static void Prune()
        {
            ZAMERTPlugin plugin = ZAMERTPlugin.Singleton;
            if (plugin == null)
                return;

            PruneList(plugin.HealthObjects);
            PruneList(plugin.InteractablePickups);
            PruneList(plugin.CustomInteractableToys);
            PruneList(plugin.InteractableTeleporters);
            PruneList(plugin.CustomColliders);
            PruneList(plugin.DummyDoors);
            PruneList(plugin.DummyGates);
            PruneList(plugin.GroovyNoises);
            PruneList(plugin.CustomDoors);
            PruneList(plugin.InteractableObjects);
            PruneList(plugin.LoopSpeakers);
            PruneList(plugin.ItemSpawners);
            PruneList(plugin.DamageTriggers);
            PruneList(plugin.PlayerCountTriggers);
            PruneList(plugin.PrefabAnchors);

            PruneSchematicMap(plugin.CodeClassPair, true);
            PruneSchematicMap(plugin.ZAMERTGroup, false);
            PruneSchematicMap(plugin.FunctionExecutors, false);
            PruneSchematicMap(plugin.SchematicVariables, false);

            if (plugin.IOkeys == null)
                return;

            List<int> emptyKeys = new List<int>();
            foreach (KeyValuePair<int, List<InteractableObject>> pair in plugin.IOkeys)
            {
                if (pair.Value == null)
                {
                    emptyKeys.Add(pair.Key);
                    continue;
                }

                pair.Value.RemoveAll(item => item == null);
                if (pair.Value.Count == 0)
                    emptyKeys.Add(pair.Key);
            }

            foreach (int key in emptyKeys)
                plugin.IOkeys.Remove(key);
        }

        public static void ReregisterKeybinds()
        {
            ZAMERTPlugin plugin = ZAMERTPlugin.Singleton;
            if (plugin == null || plugin.Config == null || plugin.Config.DisableSSS || plugin.IOkeys == null)
                return;

            foreach (int key in plugin.IOkeys.Keys.ToList())
            {
                List<InteractableObject> list;
                if (plugin.IOkeys.TryGetValue(key, out list) && list != null && list.Count > 0)
                    ServerSettings.RegisterKeybind((KeyCode)key);
            }
        }

        public static IEnumerable<ZAMERTInteractable> All()
        {
            ZAMERTPlugin plugin = ZAMERTPlugin.Singleton;
            if (plugin == null)
                return Enumerable.Empty<ZAMERTInteractable>();

            HashSet<int> seen = new HashSet<int>();
            List<ZAMERTInteractable> result = new List<ZAMERTInteractable>();

            if (plugin.CodeClassPair != null)
            {
                foreach (Dictionary<int, ZAMERTInteractable> dict in plugin.CodeClassPair.Values)
                {
                    if (dict == null)
                        continue;
                    foreach (ZAMERTInteractable item in dict.Values)
                        Add(result, seen, item);
                }
            }

            AddRange(result, seen, plugin.HealthObjects);
            AddRange(result, seen, plugin.InteractableObjects);
            AddRange(result, seen, plugin.InteractablePickups);
            AddRange(result, seen, plugin.CustomInteractableToys);
            AddRange(result, seen, plugin.CustomColliders);
            AddRange(result, seen, plugin.LoopSpeakers);
            AddRange(result, seen, plugin.ItemSpawners);
            AddRange(result, seen, plugin.DamageTriggers);
            AddRange(result, seen, plugin.PlayerCountTriggers);
            AddRange(result, seen, plugin.CustomDoors);
            AddRange(result, seen, plugin.GroovyNoises);
            AddRange(result, seen, plugin.PrefabAnchors);
            AddRange(result, seen, plugin.InteractableTeleporters);
            return result;
        }

        private static void AddRange<T>(List<ZAMERTInteractable> result, HashSet<int> seen, List<T> source)
            where T : ZAMERTInteractable
        {
            if (source == null)
                return;
            for (int i = 0; i < source.Count; i++)
                Add(result, seen, source[i]);
        }

        private static void Add(List<ZAMERTInteractable> result, HashSet<int> seen, ZAMERTInteractable item)
        {
            if (item == null)
                return;
            if (seen.Add(item.GetInstanceID()))
                result.Add(item);
        }

        private static void PruneList<T>(List<T> list) where T : Object
        {
            if (list != null)
                list.RemoveAll(item => item == null);
        }

        private static void PruneSchematicMap<T>(Dictionary<SchematicObject, T> map, bool pruneCodes)
        {
            if (map == null)
                return;

            List<SchematicObject> dead = map.Keys.Where(key => key == null).ToList();
            foreach (SchematicObject key in dead)
                map.Remove(key);

            if (!pruneCodes)
                return;

            Dictionary<SchematicObject, Dictionary<int, ZAMERTInteractable>> codes = map as Dictionary<SchematicObject, Dictionary<int, ZAMERTInteractable>>;
            if (codes == null)
                return;

            foreach (Dictionary<int, ZAMERTInteractable> dict in codes.Values)
            {
                if (dict == null)
                    continue;
                List<int> deadCodes = dict.Where(pair => pair.Value == null).Select(pair => pair.Key).ToList();
                foreach (int code in deadCodes)
                    dict.Remove(code);
            }
        }
    }
}
