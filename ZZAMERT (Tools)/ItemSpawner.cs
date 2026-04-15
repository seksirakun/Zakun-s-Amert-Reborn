using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ItemSpawner : FakeMono
{
    public new ISDTO data = new ISDTO();
    public override DTO _data { get => data; }

    public override DTO _ScriptValueData { get => data; }
}

[Serializable]
public class ISDTO : DTO
{
    public override void OnValidate() { }

    [Header("Item to spawn (None = disabled)")]
    public ItemType ItemType;
    public int Count = 1;
    public Vector3 LocalPosition;
    [Header("Spawn automatically when the schematic spawns")]
    public bool AutoStart = true;
    [Tooltip("Delay (seconds) before the first auto-spawn. 0 uses a default 0.5s grace period.")]
    public float SpawnDelay;
    [Tooltip("Seconds to wait after all spawned items are picked up before respawning. 0 = no respawn.")]
    public float RespawnTime;
}

public class ItemSpawnerCompiler
{
    private static readonly Config Config = SchematicManager.Config;

    [MenuItem("SchematicManager/Compile Item Spawners", priority = -11)]
    public static void OnCompile()
    {
        foreach (Schematic schematic in GameObject.FindObjectsOfType<Schematic>())
        {
            Compile(schematic);
        }
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
            Debug.LogError("Could Not find root object's compiled directory!");
            return;
        }

        Directory.CreateDirectory(schematicDirectoryPath);

        File.WriteAllText(
            Path.Combine(schematicDirectoryPath, $"{schematic.gameObject.name}-ItemSpawners.json"),
            JsonConvert.SerializeObject(
                schematic.transform.GetComponentsInChildren<ItemSpawner>().Select(x => x.data),
                Formatting.Indented,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));

        Debug.Log("Successfully compiled Item Spawners.");
    }
}
