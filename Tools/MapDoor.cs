namespace ZAMERT
{
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public enum MapDoorType
{
    Lcz = 0,
    LightContainmentDoor = 1,
    Hcz = 2,
    HeavyContainmentDoor = 3,
    Ez = 4,
    EntranceDoor = 5,
    Bulkdoor = 6,
    HeavyBulkDoor = 7,
    Gate = 8,
}

public class MapDoor : FakeMono
{
    public new MDDTO data = new MDDTO();
    public new MDDTO ScriptValueData = new MDDTO();
    public override DTO _data { get => data; }
    public override DTO _ScriptValueData { get => ScriptValueData; }
}

[Serializable]
public class MDDTO : DTO
{
    public override void OnValidate() { }

    [Tooltip("Spawnlanacak kapının türü.")]
    public MapDoorType DoorType = MapDoorType.EntranceDoor;

    [Tooltip("Kapıyı açabilecek keycard izinleri.")]
    public DoorPermissionFlags Permissions = DoorPermissionFlags.None;

    public bool IsOpen = false;
    public bool IsLocked = false;
    public bool RequireAllPermissions = false;
}

#if UNITY_EDITOR
public class MapDoorCompiler
{
    [MenuItem("SchematicManager/Compile Map Doors", priority = -11)]
    public static void OnCompile()
    {
        foreach (Schematic schematic in GameObject.FindObjectsOfType<Schematic>())
            Compile(schematic);
    }

    public static void Compile(Schematic schematic)
    {
        string dir = AmertCompile.GetSchematicDirectory(schematic);
        if (dir == null)
            return;

        MapDoor[] doors = AmertCompile.Collect<MapDoor>(schematic);
        foreach (MapDoor door in doors)
            AmertCompile.RefreshMeta(door);

        AmertCompile.WritePlain(dir, schematic, "MapDoors", doors.Select(x => x.data).ToList());
        Debug.Log("Compiled Map Doors (" + doors.Length + ") for " + schematic.gameObject.name);
    }
}
#endif
}
