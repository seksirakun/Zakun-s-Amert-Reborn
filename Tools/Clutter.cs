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

#if UNITY_EDITOR
public class ClutterCompiler
{
    [MenuItem("SchematicManager/Compile Clutter", priority = -11)]
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

        Clutter[] clutters = AmertCompile.Collect<Clutter>(schematic);
        foreach (Clutter clutter in clutters)
            AmertCompile.RefreshMeta(clutter);

        AmertCompile.WritePlain(dir, schematic, "Clutter", clutters.Select(x => x.data).ToList());
        Debug.Log("Compiled Clutter (" + clutters.Length + ") for " + schematic.gameObject.name);
    }
}
#endif
