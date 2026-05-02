using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class LoopSpeaker : FakeMono
{
    public new LSDTO data = new LSDTO();
    public override DTO _data { get => data; }

    public override DTO _ScriptValueData { get => data; }
}

[Serializable]
public class LSDTO : DTO
{
    public override void OnValidate() { }

    [Header("Audio file name (from ZAMERT audio folder)")]
    public string AudioName;
    public float Volume = 1f;
    public bool IsSpatial = true;
    public float MaxDistance = 30f;
    public float MinDistance = 5f;
    public SVector3 LocalPosition;
    [Header("Start playing automatically when the schematic spawns")]
    public bool AutoStart = true;
}

public class LoopSpeakerCompiler
{
    private static readonly Config Config = SchematicManager.Config;

    [MenuItem("SchematicManager/Compile Loop Speakers", priority = -11)]
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
            Path.Combine(schematicDirectoryPath, $"{schematic.gameObject.name}-LoopSpeakers.json"),
            JsonConvert.SerializeObject(
                schematic.transform.GetComponentsInChildren<LoopSpeaker>().Select(x => x.data),
                Formatting.Indented,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));

        Debug.Log("Successfully compiled Loop Speakers.");
    }
}
