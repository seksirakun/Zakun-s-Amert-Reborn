namespace ZAMERT
{
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
    [Tooltip("Choose whether damage happens on touch, while staying inside, on exit, or a mix of them.")]
    public DamageTriggerMode TriggerMode = DamageTriggerMode.OnStay;
    [Tooltip("If enabled, the trigger kills valid targets instantly instead of applying DamageAmount.")]
    public bool KillInstant = false;
    public bool AffectHumans = true;
    public bool AffectScps = true;
}

#if UNITY_EDITOR
public class DamageTriggerCompiler
{
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
        string dir = AmertCompile.GetSchematicDirectory(schematic);
        if (dir == null)
            return;

        DamageTrigger[] triggers = AmertCompile.Collect<DamageTrigger>(schematic);
        foreach (DamageTrigger trigger in triggers)
            AmertCompile.RefreshMeta(trigger);

        AmertCompile.WritePlain(dir, schematic, "DamageTriggers", triggers.Select(x => x.data).ToList());
        Debug.Log("Compiled Damage Triggers (" + triggers.Length + ") for " + schematic.gameObject.name);
    }
}
#endif
}
