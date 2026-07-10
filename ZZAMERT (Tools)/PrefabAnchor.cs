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

#if UNITY_EDITOR
public class PrefabAnchorCompiler
{
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
        string dir = AmertCompile.GetSchematicDirectory(schematic);
        if (dir == null)
            return;

        PrefabAnchor[] anchors = AmertCompile.Collect<PrefabAnchor>(schematic);
        foreach (PrefabAnchor anchor in anchors)
            AmertCompile.RefreshMeta(anchor);

        AmertCompile.WritePlain(dir, schematic, "PrefabAnchors", anchors.Select(x => x.data).ToList());
        Debug.Log("Compiled Prefab Anchors (" + anchors.Length + ") for " + schematic.gameObject.name);
    }
}
#endif
