using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class PrefabAnchor : FakeMono
{
    public new PFADTO data = new PFADTO();
    public override DTO _data { get => data; }
    public override DTO _ScriptValueData { get => data; }
}

[Serializable]
public class PFADTO : DTO
{
    public override void OnValidate() { }

    [Tooltip("Exact network prefab name to spawn at runtime.")]
    public string PrefabName;
    public bool SpawnOnStart = true;
    public bool SpawnAsChild = false;
    public bool MatchScale = true;
    public bool DisableAnchorRenderers = false;
    public bool DestroyAnchorAfterSpawn = false;
}

public class PrefabAnchorCompiler
{
    private static readonly Config Config = SchematicManager.Config;

    [MenuItem("SchematicManager/Compile Prefab Anchors", priority = -11)]
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

        File.WriteAllText(Path.Combine(schematicDirectoryPath, $"{schematic.gameObject.name}-PrefabAnchors.json"), JsonConvert.SerializeObject(schematic.transform.GetComponentsInChildren<PrefabAnchor>().Select(x => x.data), Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
        Debug.Log("Successfully compiled Prefab Anchors.");
    }
}
