using System.Collections;
using System.ComponentModel;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using UnityEditor;
using Newtonsoft.Json;
using System.Linq;

public class InteractablePickup : FakeMono
{
    public new IPDTO data = new IPDTO();
    public new FIPDTO ScriptValueData = new FIPDTO();
    public override DTO _data { get => data; }
    public override DTO _ScriptValueData { get => ScriptValueData; }
}

[Serializable]
public class IPDTO : DTO
{
    public override void OnValidate()
    {
        AnimationModules.ForEach(x => { x.OnValidate(); x.AnimatorAdress = PublicFunctions.FindPath(x.Animator); });
        DenyAnimationModules.ForEach(x => { x.OnValidate(); x.AnimatorAdress = PublicFunctions.FindPath(x.Animator); });
    }

    public InvokeType InvokeType;
    public IPActionType ActionType;
    public bool CancelActionWhenActive;
    public List<AnimationDTO> AnimationModules;
    public WarheadActionType warheadActionType;
    public List<MessageModule> MessageModules;
    public List<DropItem> dropItems;
    public List<Commanding> commandings;
    public Scp914Mode Scp914Mode;
    [Header("Players with these roles cannot trigger this pickup.")]
    public List<RoleTypeId> IgnoredRoles = new List<RoleTypeId>();
    public List<ExplodeModule> ExplodeModules;
    public List<EffectGivingModule> effectGivingModules;
    public List<AudioModule> AudioModules;
    public List<CGNModule> GroovieNoiseToCall;
    public List<CFEModule> FunctionToCall;
    public List<PrimitiveModifyModule> PrimitiveModifyModules;
    public List<LoopSpeakerControlModule> LoopSpeakerModules;
    public List<ItemSpawnerControlModule> ItemSpawnerModules;
    public List<PlayerLinkModule> PlayerLinkModules;
    [Header("Keycard Permission (None = no restriction)")]
    public DoorPermissionFlags KeycardPermissions;
    public bool RequireAllPermissions;
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
    public List<PlayerLinkModule> DenyPlayerLinkModules;
}

[Serializable]
public class FIPDTO : DTO
{
    public override void OnValidate()
    {
        CancelActionWhenActive.OnValidate();
        Scp914Mode.OnValidate();
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

    public InvokeType InvokeType;
    public IPActionType ActionType;
    public ScriptValue CancelActionWhenActive;
    public ScriptValue Scp914Mode;
    [Header("Players with these roles cannot trigger this pickup.")]
    public List<RoleTypeId> IgnoredRoles = new List<RoleTypeId>();
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
    public List<PlayerLinkModule> PlayerLinkModules;
    [Header("Keycard Permission (None = no restriction)")]
    public DoorPermissionFlags KeycardPermissions;
    public bool RequireAllPermissions;
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
    public List<PlayerLinkModule> DenyPlayerLinkModules;
}

#if UNITY_EDITOR
public class InteractablePickupCompiler
{
    [MenuItem("SchematicManager/Compile Interactable pickups", priority = -11)]
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

        InteractablePickup[] pickups = AmertCompile.Collect<InteractablePickup>(schematic);
        foreach (InteractablePickup pickup in pickups)
            AmertCompile.RefreshMeta(pickup);

        AmertCompile.WritePlain(dir, schematic, "Pickups", pickups.Where(x => !x.UseScriptValue).Select(x => x.data).ToList());
        AmertCompile.WriteTyped(dir, schematic, "FPickups", pickups.Where(x => x.UseScriptValue).Select(x => x.ScriptValueData).ToList());
        Debug.Log("Compiled Interactable Pickups (" + pickups.Length + ") for " + schematic.gameObject.name);
    }
}
#endif
