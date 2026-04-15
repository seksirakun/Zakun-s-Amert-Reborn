using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class PlayerCountTrigger : FakeMono
{
    public new PCTDTO data = new PCTDTO();
    public new FPCTDTO ScriptValueData = new FPCTDTO();
    public override DTO _data { get => data; }
    public override DTO _ScriptValueData { get => ScriptValueData; }
}

[Serializable]
public class PCTDTO : DTO
{
    public override void OnValidate()
    {
        AnimationModules.ForEach(x => x.AnimatorAdress = PublicFunctions.FindPath(x.Animator));
    }

    [Tooltip("Fire when this many players are inside the zone.")]
    public int TriggerThreshold = 1;
    public PlayerCountTriggerMode TriggerMode = PlayerCountTriggerMode.OnReachThreshold;
    public IPActionType ActionType;
    [Tooltip("Minimum seconds between consecutive triggers.")]
    public float Cooldown = 1f;
    public bool AutoStart = true;

    public List<AnimationDTO> AnimationModules;
    public WarheadActionType warheadActionType;
    public List<MessageModule> MessageModules;
    public List<DropItem> dropItems;
    public List<Commanding> commandings;
    public List<ExplodeModule> ExplodeModules;
    public List<EffectGivingModule> effectGivingModules;
    public List<AudioModule> AudioModules;
    public List<CGNModule> GroovieNoiseToCall;
    public List<CFEModule> FunctionToCall;
    public List<PrimitiveModifyModule> PrimitiveModifyModules;
    public List<LoopSpeakerControlModule> LoopSpeakerModules;
    public List<ItemSpawnerControlModule> ItemSpawnerModules;
}

[Serializable]
public class FPCTDTO : DTO
{
    public override void OnValidate()
    {
        Cooldown.OnValidate();
        warheadActionType.OnValidate();
        AnimationModules.ForEach(x => { x.parent = this; x.OnValidate(); x.AnimatorAdress = PublicFunctions.FindPath(x.Animator); });
        MessageModules.ForEach(x => x.OnValidate());
        dropItems.ForEach(x => x.OnValidate());
        commandings.ForEach(x => x.OnValidate());
        ExplodeModules.ForEach(x => x.OnValidate());
        effectGivingModules.ForEach(x => x.OnValidate());
        AudioModules.ForEach(x => x.OnValidate());
        GroovieNoiseToCall.ForEach(x => x.OnValidate());
        FunctionToCall.ForEach(x => x.OnValidate());
        PrimitiveModifyModules.ForEach(x => x.OnValidate());
        LoopSpeakerModules.ForEach(x => x.OnValidate());
        ItemSpawnerModules.ForEach(x => x.OnValidate());
    }

    public int TriggerThreshold = 1;
    public PlayerCountTriggerMode TriggerMode = PlayerCountTriggerMode.OnReachThreshold;
    public IPActionType ActionType;
    public ScriptValue Cooldown;
    public bool AutoStart = true;

    public List<FAnimationDTO> AnimationModules;
    public ScriptValue warheadActionType;
    public List<FMessageModule> MessageModules;
    public List<FDropItem> dropItems;
    public List<FCommanding> commandings;
    public List<FExplodeModule> ExplodeModules;
    public List<FEffectGivingModule> effectGivingModules;
    public List<FAudioModule> AudioModules;
    public List<FCGNModule> GroovieNoiseToCall;
    public List<FCFEModule> FunctionToCall;
    public List<FPrimitiveModifyModule> PrimitiveModifyModules;
    public List<FLoopSpeakerControlModule> LoopSpeakerModules;
    public List<FItemSpawnerControlModule> ItemSpawnerModules;
}

public class PlayerCountTriggerCompiler
{
    private static readonly Config Config = SchematicManager.Config;

    [MenuItem("SchematicManager/Compile Player Count Triggers", priority = -11)]
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

        File.WriteAllText(Path.Combine(schematicDirectoryPath, $"{schematic.gameObject.name}-PlayerCountTriggers.json"),
            JsonConvert.SerializeObject(
                schematic.transform.GetComponentsInChildren<PlayerCountTrigger>().Where(x => !x.UseScriptValue).Select(x => x.data),
                Formatting.Indented,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));

        File.WriteAllText(Path.Combine(schematicDirectoryPath, $"{schematic.gameObject.name}-FPlayerCountTriggers.json"),
            JsonConvert.SerializeObject(
                schematic.transform.GetComponentsInChildren<PlayerCountTrigger>().Where(x => x.UseScriptValue).Select(x => x.ScriptValueData),
                Formatting.Indented,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, TypeNameHandling = TypeNameHandling.Auto })
            .Replace("Assembly-CSharp", "AdvancedMERTools"));

        Debug.Log("Successfully compiled Player Count Triggers.");
    }
}
