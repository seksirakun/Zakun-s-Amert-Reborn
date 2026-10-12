namespace ZAMERT
{
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using System.Linq;

public class HealthObject : FakeMono
{
    public new HODTO data = new HODTO();
    public new FHODTO ScriptValueData = new FHODTO();
    public override DTO _data { get => data; }
    public override DTO _ScriptValueData { get => ScriptValueData; }
    public string[] formats = "DO NOT TOUCH THE VALUES.\n{attacker_i} = attacker's player id.\n{attacker_name}\n{a_pos} = attacker's position.\n{a_room} = attacker's room\n{a_zone} = attacker's zone\n{a_role} = attacker's role\n{s_pos} = schematic's exact position.\n{s_room} = schematic's exact room.\n{s_zone} = schematic's zone.\n{a_item} = attacker's current item.\n{damage}".Split('\n');
}

[Serializable]
public class HODTO : DTO
{
    public override void OnValidate()
    {
        AnimationModules.ForEach(x => x.OnValidate());
    }

    public float Health;
    [Range(0, 100)]
    public int ArmorEfficient;
    public DeadType DeadType;
    public float DeadActionDelay;
    public float ResetHPTo;
    public bool DoNotDestroyAfterDeath;
    [Header("SCP melee whitelist (All = every supported SCP can damage this object)")]
    public ScpDamageType AllowedScpDamageTypes = ScpDamageType.All;
    public List<WhitelistWeapon> whitelistWeapons;
    public List<AnimationDTO> AnimationModules;
    // Field names must match the plugin's HODTO contract exactly
    // (the plugin reads "warheadActionType", not "warheadAction").
    [FormerlySerializedAs("warheadAction")]
    public WarheadActionType warheadActionType;
    public List<MessageModule> MessageModules;
    [FormerlySerializedAs("DropItems")]
    public List<DropItem> dropItems;
    [FormerlySerializedAs("Commandings")]
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

[Serializable]
public class FHODTO : DTO
{
    public override void OnValidate()
    {
        Health.OnValidate();
        ArmorEfficient.OnValidate();
        DeadActionDelay.OnValidate();
        ResetHPTo.OnValidate();
        DoNotDestroyAfterDeath.OnValidate();
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

    public ScriptValue Health;
    [Range(0, 100)]
    public ScriptValue ArmorEfficient;
    public DeadType DeadType;
    public ScriptValue DeadActionDelay;
    public ScriptValue ResetHPTo;
    public ScriptValue DoNotDestroyAfterDeath;
    [Header("SCP melee whitelist (All = every supported SCP can damage this object)")]
    public ScpDamageType AllowedScpDamageTypes = ScpDamageType.All;
    public List<FWhitelistWeapon> whitelistWeapons;
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
}

#if UNITY_EDITOR
public class HealthObjectCompiler
{
    [MenuItem("SchematicManager/Compile Health Schematics", priority = -11)]
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

        HealthObject[] healthObjects = AmertCompile.Collect<HealthObject>(schematic);
        foreach (HealthObject healthObject in healthObjects)
            AmertCompile.RefreshMeta(healthObject);

        AmertCompile.WritePlain(dir, schematic, "HealthObjects", healthObjects.Where(x => !x.UseScriptValue).Select(x => x.data).ToList());
        AmertCompile.WriteTyped(dir, schematic, "FHealthObjects", healthObjects.Where(x => x.UseScriptValue).Select(x => x.ScriptValueData).ToList());
        Debug.Log("Compiled Health Objects (" + healthObjects.Length + ") for " + schematic.gameObject.name);
    }
}
#endif
}
