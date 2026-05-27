using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public enum ClutterPrefabType
{
    SimpleBoxes = 0,
    PipesShort = 1,
    BoxesLadder = 2,
    TankSupportedShelf = 3,
    AngledFences = 4,
    HugeOrangePipes = 5,
    PipesLongOpen = 6,
}

public class Clutter : FakeMono
{
    public new CLDTO data = new CLDTO();
    public new CLDTO ScriptValueData = new CLDTO();
    public override DTO _data { get => data; }
    public override DTO _ScriptValueData { get => ScriptValueData; }
}

[Serializable]
public class CLDTO : DTO
{
    public override void OnValidate()
    {
        if (SpawnChance < 0f) SpawnChance = 0f;
        if (SpawnChance > 100f) SpawnChance = 100f;
    }

    [Tooltip("Spawnlanacak built-in SCP:SL clutter prefab türü.")]
    public ClutterPrefabType ClutterType = ClutterPrefabType.SimpleBoxes;

    [Tooltip("0-100 arası spawn şansı.")]
    [Range(0f, 100f)]
    public float SpawnChance = 100f;
}

public class ClutterCompiler
{
    private static readonly Config Config = SchematicManager.Config;

    [MenuItem("SchematicManager/Compile Clutter", priority = -11)]
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
            Path.Combine(schematicDirectoryPath, $"{schematic.gameObject.name}-Clutter.json"),
            JsonConvert.SerializeObject(
                schematic.transform.GetComponentsInChildren<Clutter>().Select(x => x.data),
                Formatting.Indented,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));

        Debug.Log("Compiled Clutter for " + schematic.gameObject.name);
    }
}
