namespace ZAMERT
{
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ItemSpawner : FakeMono
{
    public new ISDTO data = new ISDTO();
    public override DTO _data { get => data; }

    public override DTO _ScriptValueData { get => data; }
}

[Serializable]
public class ISDTO : DTO
{
    public override void OnValidate() { }

    [Header("Item to spawn (None = disabled)")]
    public ItemType ItemType;
    public int Count = 1;
    public SVector3 LocalPosition = new SVector3();
    [Header("Spawn automatically when the schematic spawns")]
    public bool AutoStart = true;
    [Tooltip("Delay (seconds) before the first auto-spawn. 0 uses a default 0.5s grace period.")]
    public float SpawnDelay;
    [Tooltip("Seconds to wait after all spawned items are picked up before respawning. 0 = no respawn.")]
    public float RespawnTime;
}

#if UNITY_EDITOR
public class ItemSpawnerCompiler
{
    [MenuItem("SchematicManager/Compile Item Spawners", priority = -11)]
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

        ItemSpawner[] spawners = AmertCompile.Collect<ItemSpawner>(schematic);
        foreach (ItemSpawner spawner in spawners)
            AmertCompile.RefreshMeta(spawner);

        AmertCompile.WritePlain(dir, schematic, "ItemSpawners", spawners.Select(x => x.data).ToList());
        Debug.Log("Compiled Item Spawners (" + spawners.Length + ") for " + schematic.gameObject.name);
    }
}
#endif
}
