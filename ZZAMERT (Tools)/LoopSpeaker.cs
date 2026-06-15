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
    public SVector3 LocalPosition = new SVector3();
    [Header("Start playing automatically when the schematic spawns")]
    public bool AutoStart = true;
    [Header("Per-listener volume zones (shapes that mute / gate this speaker)")]
    public List<AudioZoneDef> AudioZones = new List<AudioZoneDef>();
}

// Keep these enum orders identical to the plugin side (serialized as integers).
public enum AudioZoneShape
{
    Sphere,
    Box,
}

public enum AudioZoneMode
{
    [Tooltip("Players inside the zone do NOT hear this speaker (e.g. a hum that stops in the central room).")]
    SilenceInside,
    [Tooltip("Only players inside the zone hear this speaker.")]
    OnlyAudibleInside,
}

[Serializable]
public class AudioZoneDef
{
    public AudioZoneShape Shape = AudioZoneShape.Sphere;
    public AudioZoneMode Mode = AudioZoneMode.SilenceInside;
    [Tooltip("If true the zone follows the speaker; if false it is frozen in world space at spawn.")]
    public bool Attached = true;
    [Tooltip("Zone center as a local offset from the speaker. Can point at a different room than the source.")]
    public SVector3 Center = new SVector3();
    [Tooltip("Sphere radius (Shape = Sphere).")]
    public float Radius = 5f;
    [Tooltip("Box half-extents (Shape = Box).")]
    public SVector3 HalfExtents = new SVector3 { x = 3f, y = 3f, z = 3f };
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
