using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public enum MapCameraType
{
    Lcz = 0,
    Hcz = 1,
    Ez = 2,
    EzArm = 3,
    Sz = 4,
}

public class MapScp079Camera : FakeMono
{
    public new SCDTO data = new SCDTO();
    public new SCDTO ScriptValueData = new SCDTO();
    public override DTO _data { get => data; }
    public override DTO _ScriptValueData { get => ScriptValueData; }
}

[Serializable]
public class SCDTO : DTO
{
    public override void OnValidate() { }

    [Tooltip("Kameranın görsel/model türü (zone-themed).")]
    public MapCameraType CameraType = MapCameraType.Lcz;

    [Tooltip("SCP-079 ekranında bu kamera için gösterilecek isim.")]
    public string Label = "Custom Camera";
}

public class MapScp079CameraCompiler
{
    private static readonly Config Config = SchematicManager.Config;

    [MenuItem("SchematicManager/Compile Scp079 Cameras", priority = -11)]
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
            Path.Combine(schematicDirectoryPath, $"{schematic.gameObject.name}-Scp079Cameras.json"),
            JsonConvert.SerializeObject(
                schematic.transform.GetComponentsInChildren<MapScp079Camera>().Select(x => x.data),
                Formatting.Indented,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));

        Debug.Log("Compiled Scp079 Cameras for " + schematic.gameObject.name);
    }
}
