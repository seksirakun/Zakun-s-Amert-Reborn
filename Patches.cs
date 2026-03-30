using HarmonyLib;
using ProjectMER.Features;
using ProjectMER.Features.Objects;
using ProjectMER.Features.Serializable;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Utf8Json;
using Log = ZAMERT.ZAMERTLogger;

namespace ZAMERT
{
    [HarmonyPatch(typeof(MapUtils), nameof(MapUtils.LoadMap), typeof(string))]
    public class MapLoadingPatcher
    {
        public static void Postfix(string mapName)
        {
            if (string.IsNullOrEmpty(mapName)) return;
            if (!MapUtils.LoadedMaps.ContainsKey(mapName))
            {
                Log.Error("MapLoadingPatcher: Map '" + mapName + "' not in LoadedMaps");
                return;
            }

            string path = Path.Combine(ProjectMER.ProjectMER.MapsDir, mapName + "-ITeleporters.json");
            if (!File.Exists(path)) return;

            try
            {
                List<ITDTO> dtos = JsonSerializer.Deserialize<List<ITDTO>>(File.ReadAllText(path));
                TeleportObject[] teleports = MapUtils.LoadedMaps[mapName].SpawnedObjects
                    .Where(x => x.Base is SerializableTeleport)
                    .Cast<TeleportObject>()
                    .ToArray();

                foreach (ITDTO dto in dtos)
                {
                    int n;
                    if (!int.TryParse(dto.ObjectId, out n)) continue;
                    if (n > 0 && n <= teleports.Length)
                    {
                        InteractableTeleporter it = teleports[n - 1].gameObject.AddComponent<InteractableTeleporter>();
                        it.Base = dto;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("MapLoadingPatcher error for '" + mapName + "': " + ex.Message);
            }
        }
    }
}
