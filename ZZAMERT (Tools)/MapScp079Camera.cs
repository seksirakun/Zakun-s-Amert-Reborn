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

#if UNITY_EDITOR
public class MapScp079CameraCompiler
{
    [MenuItem("SchematicManager/Compile Scp079 Cameras", priority = -11)]
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

        MapScp079Camera[] cameras = AmertCompile.Collect<MapScp079Camera>(schematic);
        foreach (MapScp079Camera camera in cameras)
            AmertCompile.RefreshMeta(camera);

        AmertCompile.WritePlain(dir, schematic, "Scp079Cameras", cameras.Select(x => x.data).ToList());
        Debug.Log("Compiled Scp079 Cameras (" + cameras.Length + ") for " + schematic.gameObject.name);
    }
}
#endif
