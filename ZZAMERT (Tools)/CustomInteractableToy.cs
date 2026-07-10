using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class CustomInteractableToy : FakeMono
{
    public new CITDTO data = new CITDTO();
    public new FCITDTO ScriptValueData = new FCITDTO();
    public override DTO _data { get => data; }
    public override DTO _ScriptValueData { get => ScriptValueData; }
}

[Serializable]
public class CITDTO : DTO
{
    public override void OnValidate()
    {
        AnimationModules.ForEach(x => { x.OnValidate(); x.AnimatorAdress = PublicFunctions.FindPath(x.Animator); });
        DenyAnimationModules.ForEach(x => { x.OnValidate(); x.AnimatorAdress = PublicFunctions.FindPath(x.Animator); });
    }

    public IPActionType ActionType;
    [Tooltip("Seconds between interactions, shared by all ZAMERT components on this object. 0 = default 0.2s.")]
    public float InteractionCooldown;
    public List<AnimationDTO> AnimationModules;
    public WarheadActionType warheadActionType;
    public List<MessageModule> MessageModules;
    public List<DropItem> dropItems;
    public List<Commanding> commandings;
    public Scp914Mode Scp914Mode;
    public List<ExplodeModule> ExplodeModules;
    public List<EffectGivingModule> effectGivingModules;
    public List<AudioModule> AudioModules;
    public List<CGNModule> GroovieNoiseToCall;
    public List<CFEModule> FunctionToCall;
    public List<PrimitiveModifyModule> PrimitiveModifyModules;
    public List<LoopSpeakerControlModule> LoopSpeakerModules;
    public List<ItemSpawnerControlModule> ItemSpawnerModules;
    [Header("Keycard Permission (None = no restriction)")]
    public DoorPermissionFlags KeycardPermissions;
    public bool RequireAllPermissions;
    public bool IncludeRootPrimitive = true;
    public bool IncludeChildPrimitives = true;
    public float ToyScaleMultiplier = 1.3f;
    [Header("Deny Action (when keycard denied)")]
    public IPActionType DenyActionType;
    public List<MessageModule> DenyMessageModules;
    public List<AudioModule> DenyAudioModules;
    public List<CGNModule> DenyGroovieNoiseToCall;
    public List<CFEModule> DenyFunctionToCall;
    public List<AnimationDTO> DenyAnimationModules;
    public WarheadActionType DenyWarheadActionType;
    public List<DropItem> DenyDropItems;
    public List<Commanding> DenyCommandings;
    public List<ExplodeModule> DenyExplodeModules;
    public List<EffectGivingModule> DenyEffectGivingModules;
    public List<PrimitiveModifyModule> DenyPrimitiveModifyModules;
    public List<LoopSpeakerControlModule> DenyLoopSpeakerModules;
    public List<ItemSpawnerControlModule> DenyItemSpawnerModules;
}

[Serializable]
public class FCITDTO : DTO
{
    public override void OnValidate()
    {
        Scp914Mode.OnValidate();
        InteractionCooldown.OnValidate();
        ToyScaleMultiplier.OnValidate();
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
        DenyMessageModules.ForEach(x => x.OnValidate());
        DenyAudioModules.ForEach(x => x.OnValidate());
        DenyGroovieNoiseToCall.ForEach(x => x.OnValidate());
        DenyFunctionToCall.ForEach(x => x.OnValidate());
        DenyWarheadActionType.OnValidate();
        DenyAnimationModules.ForEach(x => { x.parent = this; x.OnValidate(); x.AnimatorAdress = PublicFunctions.FindPath(x.Animator); });
        DenyDropItems.ForEach(x => x.OnValidate());
        DenyCommandings.ForEach(x => x.OnValidate());
        DenyExplodeModules.ForEach(x => x.OnValidate());
        DenyEffectGivingModules.ForEach(x => x.OnValidate());
        DenyPrimitiveModifyModules.ForEach(x => x.OnValidate());
        DenyLoopSpeakerModules.ForEach(x => x.OnValidate());
        DenyItemSpawnerModules.ForEach(x => x.OnValidate());
        PrimitiveModifyModules.ForEach(x => x.OnValidate());
        LoopSpeakerModules.ForEach(x => x.OnValidate());
        ItemSpawnerModules.ForEach(x => x.OnValidate());
    }

    public IPActionType ActionType;
    public ScriptValue Scp914Mode;
    public ScriptValue InteractionCooldown;
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
    [Header("Keycard Permission (None = no restriction)")]
    public DoorPermissionFlags KeycardPermissions;
    public bool RequireAllPermissions;
    public bool IncludeRootPrimitive = true;
    public bool IncludeChildPrimitives = true;
    public ScriptValue ToyScaleMultiplier;
    [Header("Deny Action (when keycard denied)")]
    public IPActionType DenyActionType;
    public List<FMessageModule> DenyMessageModules;
    public List<FAudioModule> DenyAudioModules;
    public List<FCGNModule> DenyGroovieNoiseToCall;
    public List<FCFEModule> DenyFunctionToCall;
    public List<FAnimationDTO> DenyAnimationModules;
    public ScriptValue DenyWarheadActionType;
    public List<FDropItem> DenyDropItems;
    public List<FCommanding> DenyCommandings;
    public List<FExplodeModule> DenyExplodeModules;
    public List<FEffectGivingModule> DenyEffectGivingModules;
    public List<FPrimitiveModifyModule> DenyPrimitiveModifyModules;
    public List<FLoopSpeakerControlModule> DenyLoopSpeakerModules;
    public List<FItemSpawnerControlModule> DenyItemSpawnerModules;
}

#if UNITY_EDITOR
public class CustomInteractableToyCompiler
{
    [MenuItem("SchematicManager/Compile Custom Interactable Toys", priority = -11)]
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

        CustomInteractableToy[] toys = AmertCompile.Collect<CustomInteractableToy>(schematic);
        foreach (CustomInteractableToy toy in toys)
            AmertCompile.RefreshMeta(toy);

        AmertCompile.WritePlain(dir, schematic, "ToyInteractables", toys.Where(x => !x.UseScriptValue).Select(x => x.data).ToList());
        AmertCompile.WriteTyped(dir, schematic, "FToyInteractables", toys.Where(x => x.UseScriptValue).Select(x => x.ScriptValueData).ToList());
        Debug.Log("Compiled Custom Interactable Toys (" + toys.Length + ") for " + schematic.gameObject.name);
    }
}
#endif
