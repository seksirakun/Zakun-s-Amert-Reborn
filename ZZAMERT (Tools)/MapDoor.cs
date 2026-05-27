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

public class MapDoorCompiler
{
    private static readonly Config Config = SchematicManager.Config;

    [MenuItem("SchematicManager/Compile Map Doors", priority = -11)]
    public static void OnCompile()
    {
        foreach (Schematic schematic in GameObject.FindObjectsOfType<Schematic>())
            Compile(schematic);
    }

    public static void Compile(Schematic schematic)
    {
        string parentDirectoryPath = Directory.Exists(Config.ExportPath)
            ? Config.ExportPath
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "MapEditorReborn_CompiledSchematics");
        string schematicDirectoryPath = Path.Combine(parentDirectoryPath, schematic.gameObject.name);

        if (!Directory.Exists(parentDirectoryPath))
        {
            Debug.LogError("Could not find compiled directory.");
            return;
        }
        Directory.CreateDirectory(schematicDirectoryPath);

        File.WriteAllText(
            Path.Combine(schematicDirectoryPath, $"{schematic.gameObject.name}-MapDoors.json"),
            JsonConvert.SerializeObject(
                schematic.transform.GetComponentsInChildren<MapDoor>().Select(x => x.data),
                Formatting.Indented,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));

        Debug.Log("Compiled Map Doors for " + schematic.gameObject.name);
    }
}
