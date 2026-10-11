using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.ComponentModel;
using System.IO;
using UnityEditor;
using UnityEngine;

public class CustomCollider : FakeMono
{
    public new CCDTO data = new CCDTO();
    public new FCCDTO ScriptValueData = new FCCDTO();
    public override DTO _data { get => data; }
    public override DTO _ScriptValueData { get => ScriptValueData; }
}

[Serializable]
public class CCDTO : DTO
{
    public override void OnValidate()
    {
        AnimationModules.ForEach(x => x.OnValidate());
    }

    public ColliderActionType ColliderActionType;
    public CollisionType CollisionType;
    public DetectType DetectType;
    [Header("Players with these roles are ignored by this collider.")]
    public List<RoleTypeId> IgnoredRoles = new List<RoleTypeId>();
    public float ModifyHealthAmount;
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

[Serializable]
public class FCCDTO : DTO
{
    public override void OnValidate()
    {
        CollisionType.OnValidate();
        DetectType.OnValidate();
        ModifyHealthAmount.OnValidate();
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

    public ColliderActionType ColliderActionType;
    public ScriptValue CollisionType;
    public ScriptValue DetectType;
    [Header("Players with these roles are ignored by this collider.")]
    public List<RoleTypeId> IgnoredRoles = new List<RoleTypeId>();
    public ScriptValue ModifyHealthAmount;
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
public class CustomColliderCompiler
{
    [MenuItem("SchematicManager/Compile custom collider", priority = -11)]
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

        CustomCollider[] colliders = AmertCompile.Collect<CustomCollider>(schematic);
        foreach (CustomCollider collider in colliders)
            AmertCompile.RefreshMeta(collider);

        AmertCompile.WritePlain(dir, schematic, "Colliders", colliders.Where(x => !x.UseScriptValue).Select(x => x.data).ToList());
        AmertCompile.WriteTyped(dir, schematic, "FColliders", colliders.Where(x => x.UseScriptValue).Select(x => x.ScriptValueData).ToList());
        Debug.Log("Compiled Custom Colliders (" + colliders.Length + ") for " + schematic.gameObject.name);
    }
}
#endif
