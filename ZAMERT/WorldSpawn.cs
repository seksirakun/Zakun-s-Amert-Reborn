using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using LabApi.Features.Wrappers;
using ProjectMER.Features;
using ProjectMER.Features.Objects;
using ProjectMER.Features.Serializable;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using PMERDoorType = ProjectMER.Features.Enums.DoorType;

namespace ZAMERT
{
    internal static class WorldSpawn
    {
        private static readonly FieldInfo AttachedBlocks = typeof(SchematicObject).GetField("_attachedBlocks", BindingFlags.Instance | BindingFlags.NonPublic);

        public static bool TryFromAuthoredId(int id, out PMERDoorType type)
        {
            switch (id)
            {
                case 0:
                case 1:
                    type = PMERDoorType.Lcz;
                    return true;
                case 2:
                case 3:
                    type = PMERDoorType.Hcz;
                    return true;
                case 4:
                case 5:
                    type = PMERDoorType.Ez;
                    return true;
                case 6:
                case 7:
                    type = PMERDoorType.Bulkdoor;
                    return true;
                case 8:
                    type = PMERDoorType.Gate;
                    return true;
                default:
                    type = PMERDoorType.Ez;
                    return false;
            }
        }

        public static bool TryFromName(string name, out PMERDoorType type)
        {
            switch ((name ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "lcz":
                case "light":
                case "lightcontainment":
                case "lightcontainmentdoor":
                    type = PMERDoorType.Lcz;
                    return true;
                case "hcz":
                case "heavy":
                case "heavycontainment":
                case "heavycontainmentdoor":
                    type = PMERDoorType.Hcz;
                    return true;
                case "ez":
                case "entrance":
                case "entrancedoor":
                    type = PMERDoorType.Ez;
                    return true;
                case "bulk":
                case "bulkdoor":
                case "heavybulk":
                case "heavybulkdoor":
                    type = PMERDoorType.Bulkdoor;
                    return true;
                case "gate":
                case "prygate":
                    type = PMERDoorType.Gate;
                    return true;
                default:
                    type = PMERDoorType.Ez;
                    return false;
            }
        }

        public static GameObject SpawnDoor(PMERDoorType type, Vector3 position, Quaternion rotation, Vector3 scale, DoorPermissionFlags permissions, bool requireAll, bool open, bool locked)
        {
            if (PrefabFor(type) == null)
            {
                ZAMERTLogger.Warn("Door prefab for " + type + " is not ready. ProjectMER has to finish loading first.");
                return null;
            }

            if (scale.sqrMagnitude < 0.0001f)
                scale = Vector3.one;

            SerializableDoor data = new SerializableDoor
            {
                DoorType = type,
                Position = position,
                Rotation = rotation.eulerAngles,
                Scale = scale,
                IsOpen = open,
                IsLocked = locked,
                RequiredPermissions = permissions,
                RequireAll = requireAll,
            };

            return data.SpawnOrUpdateObject(null, null);
        }

        public static void Attach(SchematicObject schematic, GameObject spawned)
        {
            if (schematic == null || spawned == null || AttachedBlocks == null)
                return;

            List<GameObject> list = AttachedBlocks.GetValue(schematic) as List<GameObject>;
            if (list != null && !list.Contains(spawned))
                list.Add(spawned);
        }

        public static Door NearestDoor(Vector3 position, float maxDistance)
        {
            Door nearest = null;
            float best = maxDistance * maxDistance;
            foreach (Door door in Door.List)
            {
                if (door == null || door.GameObject == null)
                    continue;

                float sqr = (door.Position - position).sqrMagnitude;
                if (sqr > best)
                    continue;

                best = sqr;
                nearest = door;
            }

            return nearest;
        }

        private static DoorVariant PrefabFor(PMERDoorType type)
        {
            DoorVariant managed = null;
            switch (type)
            {
                case PMERDoorType.Lcz: managed = PrefabManager.DoorLcz; break;
                case PMERDoorType.Hcz: managed = PrefabManager.DoorHcz; break;
                case PMERDoorType.Ez: managed = PrefabManager.DoorEz; break;
                case PMERDoorType.Bulkdoor: managed = PrefabManager.DoorHeavyBulk; break;
                case PMERDoorType.Gate: managed = PrefabManager.DoorGate; break;
            }

            if (managed != null)
                return managed;

            string prefabName;
            switch (type)
            {
                case PMERDoorType.Lcz: prefabName = "LCZ BreakableDoor"; break;
                case PMERDoorType.Hcz: prefabName = "HCZ BreakableDoor"; break;
                case PMERDoorType.Ez: prefabName = "EZ BreakableDoor"; break;
                case PMERDoorType.Bulkdoor: prefabName = "HCZ BulkDoor"; break;
                case PMERDoorType.Gate: prefabName = "Spawnable Unsecured Pryable GateDoor"; break;
                default: return null;
            }

            GameObject prefab;
            if (!ZamertPrefabRegistry.TryGetPrefab(prefabName, out prefab) || prefab == null)
                return null;

            return prefab.GetComponent<DoorVariant>();
        }
    }
}
