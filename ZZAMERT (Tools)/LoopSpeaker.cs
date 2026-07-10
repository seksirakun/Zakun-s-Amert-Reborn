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

public enum AudioZoneShape
{
    Sphere,
    Box,
}

public enum AudioZoneMode
{
    SilenceInside,
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

#if UNITY_EDITOR
public class LoopSpeakerCompiler
{
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
        string dir = AmertCompile.GetSchematicDirectory(schematic);
        if (dir == null)
            return;

        LoopSpeaker[] speakers = AmertCompile.Collect<LoopSpeaker>(schematic);
        foreach (LoopSpeaker speaker in speakers)
            AmertCompile.RefreshMeta(speaker);

        AmertCompile.WritePlain(dir, schematic, "LoopSpeakers", speakers.Select(x => x.data).ToList());
        Debug.Log("Compiled Loop Speakers (" + speakers.Length + ") for " + schematic.gameObject.name);
    }
}
#endif
