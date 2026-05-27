using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class InteractableTeleporter : FakeMono
{
    [Tooltip("Hedef PMER haritasının adı (örn. 'MyMap'). Compile sonrası bu isimle JSON dosyası üretilir.")]
    public string TargetMapName = "";

    [Tooltip("Haritadaki SerializableTeleport'un 1-tabanlı index'i (YAML'deki sırasıyla).")]
    public int TeleporterIndex = 1;

    public new ITDTO data = new ITDTO();
    public new ITDTO ScriptValueData = new ITDTO();
    public override DTO _data { get => data; }
    public override DTO _ScriptValueData { get => ScriptValueData; }
}

[Serializable]
public class ITDTO : DTO
{
    public override void OnValidate()
    {
        AnimationModules.ForEach(x => { x.OnValidate(); x.AnimatorAdress = PublicFunctions.FindPath(x.Animator); });
    }

    public TeleportInvokeType InvokeType = TeleportInvokeType.Collide;
    public IPActionType ActionType;
    public Scp914Mode Scp914Mode;
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

public class InteractableTeleporterCompiler
{
    private static readonly Config Config = SchematicManager.Config;

    [MenuItem("SchematicManager/Compile Interactable Teleporters", priority = -11)]
    public static void OnCompile()
    {
        string parentDirectoryPath = Directory.Exists(Config.ExportPath)
            ? Config.ExportPath
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "MapEditorReborn_CompiledSchematics");

        if (!Directory.Exists(parentDirectoryPath))
        {
            Debug.LogError("Could not find compiled directory.");
            return;
        }

        var groups = GameObject.FindObjectsOfType<InteractableTeleporter>()
            .Where(x => !string.IsNullOrWhiteSpace(x.TargetMapName))
            .GroupBy(x => x.TargetMapName);

        int fileCount = 0;
        foreach (var group in groups)
        {
            string mapName = group.Key.Trim();
            List<ITDTO> entries = group
                .OrderBy(x => x.TeleporterIndex)
                .Select(x =>
                {
                    x.data.ObjectId = x.TeleporterIndex.ToString();
                    return x.data;
                })
                .ToList();

            File.WriteAllText(
                Path.Combine(parentDirectoryPath, $"{mapName}-ITeleporters.json"),
                JsonConvert.SerializeObject(entries, Formatting.Indented,
                    new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            fileCount++;
        }

        Debug.Log("Compiled Interactable Teleporters: " + fileCount + " file(s). Copy them to your PMER Maps folder.");
    }
}
