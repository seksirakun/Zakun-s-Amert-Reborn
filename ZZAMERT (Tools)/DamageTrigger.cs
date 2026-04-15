using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class DamageTrigger : FakeMono
{
    public new DTTDTO data = new DTTDTO();
    public override DTO _data { get => data; }
    public override DTO _ScriptValueData { get => data; }
}

[Serializable]
public class DTTDTO : DTO
{
    public override void OnValidate() { }

    [Tooltip("Damage dealt to each player per tick.")]
    public float DamageAmount = 10f;
    [Tooltip("Seconds between damage ticks.")]
    public float DamageInterval = 1f;
    [Tooltip("Minimum player HP floor. Damage will not reduce HP below this value. Set 0 to allow kills.")]
    public float MinimumHealth = 1f;
    [Tooltip("Start the damage zone automatically when the schematic spawns.")]
    public bool AutoStart = true;
}

public class DamageTriggerCompiler
{
    private static readonly Config Config = SchematicManager.Config;

    [MenuItem("SchematicManager/Compile Damage Triggers", priority = -11)]
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
            Path.Combine(schematicDirectoryPath, $"{schematic.gameObject.name}-DamageTriggers.json"),
            JsonConvert.SerializeObject(
                schematic.transform.GetComponentsInChildren<DamageTrigger>().Select(x => x.data),
                Formatting.Indented,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));

        Debug.Log("Successfully compiled Damage Triggers.");
    }
}
