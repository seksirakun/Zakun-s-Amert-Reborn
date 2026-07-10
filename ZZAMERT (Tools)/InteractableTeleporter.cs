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
    public List<PlayerLinkModule> PlayerLinkModules;
}

#if UNITY_EDITOR
public class InteractableTeleporterCompiler
{
    [MenuItem("SchematicManager/Compile Interactable Teleporters", priority = -11)]
    public static void OnCompile()
    {
        string parentDirectoryPath = AmertCompile.GetExportRootDirectory();
        if (parentDirectoryPath == null)
            return;

        var groups = GameObject.FindObjectsOfType<InteractableTeleporter>(true)
            .Where(x => !string.IsNullOrWhiteSpace(x.TargetMapName))
            .GroupBy(x => x.TargetMapName.Trim());

        int fileCount = 0;
        foreach (var group in groups)
        {
            string mapName = group.Key;
            List<ITDTO> entries = group
                .OrderBy(x => x.TeleporterIndex)
                .Select(x =>
                {
                    try { x.data.OnValidate(); }
                    catch (Exception ex) { Debug.LogWarning("AMERT: teleporter validation failed on '" + x.gameObject.name + "': " + ex.Message); }
                    x.data.ObjectId = x.TeleporterIndex.ToString();
                    return x.data;
                })
                .ToList();

            File.WriteAllText(
                Path.Combine(parentDirectoryPath, mapName + "-ITeleporters.json"),
                AmertJson.SerializePlain(entries));
            fileCount++;
        }

        Debug.Log("Compiled Interactable Teleporters: " + fileCount + " file(s). Copy them to your PMER Maps folder.");
    }
}
#endif
