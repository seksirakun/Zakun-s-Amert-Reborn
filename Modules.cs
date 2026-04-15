using AdminToys;
using CustomPlayerEffects;
using Footprinting;
using Interactables.Interobjects.DoorUtils;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Pickups;
using LabApi.Features.Wrappers;
using Mirror;
using ProjectMER.Features.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using Utils;
using Log = ZAMERT.ZAMERTLogger;

namespace ZAMERT
{

public class ModuleGeneralArguments
{
    public SchematicObject Schematic { get; set; }
    public Player Player { get; set; }
    public Player[] Targets { get; set; }
    public bool TargetCalculated { get; set; }
    public Transform Transform { get; set; }
    public Dictionary<string, Func<object[], string>> Interpolations { get; set; }
    public object[] InterpolationsList { get; set; }
}

[Serializable]
public class ZAMERTInteractable : NetworkBehaviour
{
    public ZAMERTDTO Base { get; set; }
    public SchematicObject OSchematic { get; set; }
    public bool Active { get; set; }

    protected virtual void OnDestroy()
    {
        if (ZAMERTPlugin.Singleton == null) return;
        if (ZAMERTPlugin.Singleton.CodeClassPair.TryGetValue(OSchematic, out var codeDict))
            codeDict.Remove(Base.Code);
        if (!string.IsNullOrEmpty(Base.ScriptGroup)
            && ZAMERTPlugin.Singleton.ZAMERTGroup.TryGetValue(OSchematic, out var groupDict)
            && groupDict.TryGetValue(Base.ScriptGroup, out var groupList))
            groupList.Remove(this);
    }

    public static void AlphaWarhead(WarheadActionType type)
    {
        foreach (WarheadActionType warhead in Enum.GetValues(typeof(WarheadActionType)))
        {
            if (type.HasFlag(warhead))
            {
                switch (warhead)
                {
                    case WarheadActionType.Start:
                        Warhead.Start();
                        break;
                    case WarheadActionType.Stop:
                        Warhead.Stop();
                        break;
                    case WarheadActionType.Lock:
                        Warhead.IsLocked = true;
                        break;
                    case WarheadActionType.UnLock:
                        Warhead.IsLocked = false;
                        break;
                    case WarheadActionType.Disable:
                        Warhead.LeverStatus = false;
                        break;
                    case WarheadActionType.Enable:
                        Warhead.LeverStatus = true;
                        break;
                }
            }
        }
    }
}

[Serializable]
public class ZAMERTDTO
{
    public bool Active { get; set; }
    public string ObjectId { get; set; }
    public int Code { get; set; }
    public string ScriptGroup { get; set; }
    public bool UseScriptValue { get; set; }
}

public class MDTO : ZAMERTDTO
{
    public List<AnimationDTO> AnimationModules { get; set; }
    public WarheadActionType warheadActionType { get; set; }
    public List<MessageModule> MessageModules { get; set; }
    public List<DropItem> dropItems { get; set; }
    public List<Commanding> commandings { get; set; }
    public List<ExplodeModule> ExplodeModules { get; set; }
    public List<EffectGivingModule> effectGivingModules { get; set; }
    public List<AudioModule> AudioModules { get; set; }
    public List<CGNModule> GroovieNoiseToCall { get; set; }
    public List<CFEModule> FunctionToCall { get; set; }
    public List<PrimitiveModifyModule> PrimitiveModifyModules { get; set; }
    public List<LoopSpeakerControlModule> LoopSpeakerModules { get; set; }
    public List<ItemSpawnerControlModule> ItemSpawnerModules { get; set; }
}

public class FMDTO : ZAMERTDTO
{
    public List<FAnimationDTO> AnimationModules { get; set; }
    public ScriptValue warheadActionType { get; set; }
    public List<FMessageModule> MessageModules { get; set; }
    public List<FDropItem> dropItems { get; set; }
    public List<FCommanding> commandings { get; set; }
    public List<FExplodeModule> ExplodeModules { get; set; }
    public List<FEffectGivingModule> effectGivingModules { get; set; }
    public List<FAudioModule> AudioModules { get; set; }
    public List<FCGNModule> GroovieNoiseToCall { get; set; }
    public List<FCFEModule> FunctionToCall { get; set; }
    public List<FPrimitiveModifyModule> PrimitiveModifyModules { get; set; }
    public List<FLoopSpeakerControlModule> LoopSpeakerModules { get; set; }
    public List<FItemSpawnerControlModule> ItemSpawnerModules { get; set; }
}

[Serializable]
public class HODTO : MDTO
{
    public float Health { get; set; }
    [Range(0, 100)]
    public int ArmorEfficient { get; set; }
    public DeadType DeadType { get; set; }
    public float DeadActionDelay { get; set; }
    public float ResetHPTo { get; set; }
    public bool DoNotDestroyAfterDeath { get; set; }
    public List<WhitelistWeapon> whitelistWeapons { get; set; }
}

[Serializable]
public class FHODTO : FMDTO
{
    public ScriptValue Health { get; set; }
    [Range(0, 100)]
    public ScriptValue ArmorEfficient { get; set; }
    public DeadType DeadType { get; set; }
    public ScriptValue DeadActionDelay { get; set; }
    public ScriptValue ResetHPTo { get; set; }
    public ScriptValue DoNotDestroyAfterDeath { get; set; }
    public List<FWhitelistWeapon> whitelistWeapons { get; set; }
}

[Serializable]
public class ITDTO : MDTO
{
    public TeleportInvokeType InvokeType { get; set; }
    public IPActionType ActionType { get; set; }
}

[Serializable]
public class IODTO : MDTO
{
    public int InputKeyCode { get; set; }
    public float InteractionMaxRange { get; set; }
    public IPActionType ActionType { get; set; }
    public Scp914Mode Scp914Mode { get; set; }

    public DoorPermissionFlags KeycardPermissions { get; set; }

    public bool RequireAllPermissions { get; set; }

    public IPActionType DenyActionType { get; set; }
    public List<MessageModule> DenyMessageModules { get; set; } = new List<MessageModule>();
    public List<AudioModule> DenyAudioModules { get; set; } = new List<AudioModule>();
    public List<CGNModule> DenyGroovieNoiseToCall { get; set; } = new List<CGNModule>();
    public List<CFEModule> DenyFunctionToCall { get; set; } = new List<CFEModule>();
}

[Serializable]
public class FIODTO : FMDTO
{
    public int InputKeyCode { get; set; }
    public ScriptValue InteractionMaxRange { get; set; }
    public IPActionType ActionType { get; set; }
    public ScriptValue Scp914Mode { get; set; }

    public DoorPermissionFlags KeycardPermissions { get; set; }

    public bool RequireAllPermissions { get; set; }

    public IPActionType DenyActionType { get; set; }
    public List<FMessageModule> DenyMessageModules { get; set; } = new List<FMessageModule>();
    public List<FAudioModule> DenyAudioModules { get; set; } = new List<FAudioModule>();
    public List<FCGNModule> DenyGroovieNoiseToCall { get; set; } = new List<FCGNModule>();
    public List<FCFEModule> DenyFunctionToCall { get; set; } = new List<FCFEModule>();
}

[Serializable]
public class IPDTO : MDTO
{
    public InvokeType InvokeType { get; set; }
    public IPActionType ActionType { get; set; }
    public bool CancelActionWhenActive { get; set; }
    public Scp914Mode Scp914Mode { get; set; }

    public DoorPermissionFlags KeycardPermissions { get; set; }

    public bool RequireAllPermissions { get; set; }

    public IPActionType DenyActionType { get; set; }
    public List<MessageModule> DenyMessageModules { get; set; } = new List<MessageModule>();
    public List<AudioModule> DenyAudioModules { get; set; } = new List<AudioModule>();
    public List<CGNModule> DenyGroovieNoiseToCall { get; set; } = new List<CGNModule>();
    public List<CFEModule> DenyFunctionToCall { get; set; } = new List<CFEModule>();
}

[Serializable]
public class FIPDTO : FMDTO
{
    public InvokeType InvokeType { get; set; }
    public IPActionType ActionType { get; set; }
    public ScriptValue CancelActionWhenActive { get; set; }
    public ScriptValue Scp914Mode { get; set; }

    public DoorPermissionFlags KeycardPermissions { get; set; }

    public bool RequireAllPermissions { get; set; }

    public IPActionType DenyActionType { get; set; }
    public List<FMessageModule> DenyMessageModules { get; set; } = new List<FMessageModule>();
    public List<FAudioModule> DenyAudioModules { get; set; } = new List<FAudioModule>();
    public List<FCGNModule> DenyGroovieNoiseToCall { get; set; } = new List<FCGNModule>();
    public List<FCFEModule> DenyFunctionToCall { get; set; } = new List<FCFEModule>();
}

[Serializable]
public class CCDTO : MDTO
{
    public ColliderActionType ColliderActionType { get; set; }
    public CollisionType CollisionType { get; set; }
    public DetectType DetectType { get; set; }
    public float ModifyHealthAmount { get; set; }
    public List<DropItem> dropItems { get; set; } = new List<DropItem>();
}

[Serializable]
public class FCCDTO : FMDTO
{
    public ColliderActionType ColliderActionType { get; set; }
    public ScriptValue CollisionType { get; set; }
    public ScriptValue DetectType { get; set; }
    public ScriptValue ModifyHealthAmount { get; set; }
    public List<FDropItem> dropItems { get; set; } = new List<FDropItem>();
}

[Serializable]
public class LSDTO : ZAMERTDTO
{
    public string AudioName { get; set; }
    public float Volume { get; set; } = 1f;
    public bool IsSpatial { get; set; }
    public float MaxDistance { get; set; } = 30f;
    public float MinDistance { get; set; } = 5f;
    public SVector3 LocalPosition { get; set; }
    public bool AutoStart { get; set; } = true;
}

[Serializable]
public class ISDTO : ZAMERTDTO
{
    public ItemType ItemType { get; set; }
    public int Count { get; set; } = 1;
    public SVector3 LocalPosition { get; set; }
    public bool AutoStart { get; set; } = true;
    public float SpawnDelay { get; set; }
    public float RespawnTime { get; set; }
}

[Serializable]
public class PCTDTO : MDTO
{

    public int TriggerThreshold { get; set; } = 1;
    public PlayerCountTriggerMode TriggerMode { get; set; } = PlayerCountTriggerMode.OnReachThreshold;
    public IPActionType ActionType { get; set; }

    public float Cooldown { get; set; } = 1f;
    public bool AutoStart { get; set; } = true;
}

[Serializable]
public class FPCTDTO : FMDTO
{
    public int TriggerThreshold { get; set; } = 1;
    public PlayerCountTriggerMode TriggerMode { get; set; } = PlayerCountTriggerMode.OnReachThreshold;
    public IPActionType ActionType { get; set; }
    public ScriptValue Cooldown { get; set; }
    public bool AutoStart { get; set; } = true;
}

[Serializable]
public class DTTDTO : ZAMERTDTO
{
    public float DamageAmount { get; set; } = 10f;
    public float DamageInterval { get; set; } = 1f;

    public float MinimumHealth { get; set; } = 1f;
    public bool AutoStart { get; set; } = true;
}

[Serializable]
public class GNDTO : ZAMERTDTO
{
    public List<GMDTO> Settings { get; set; }
}

[Serializable]
public class FGNDTO : ZAMERTDTO
{
    public List<FGMDTO> Settings { get; set; }
}

[Serializable]
public class CDDTO : ZAMERTDTO
{
    public string Animator { get; set; }
    public DoorType DoorType { get; set; }
    public string OpenAnimation { get; set; }
    public string CloseAnimation { get; set; }
    public string LockAnimation { get; set; }
    public string UnlockAnimation { get; set; }
    public string BrokenAnimation { get; set; }
    public Vector3 DoorInstallPos { get; set; }
    public Vector3 DoorInstallRot { get; set; }
    public Vector3 DoorInstallScl { get; set; }
    public float DoorHealth { get; set; }
    public DoorPermissionFlags DoorPermissions { get; set; }
    public DoorDamageType DoorDamageType { get; set; }
}

[Serializable]
public class DGDTO
{
    public string ObjectId { get; set; }
    public float Health { get; set; }
    public DoorDamageType DamagableDamageType { get; set; }
    public DoorPermissionFlags DoorPermissions { get; set; }
}

[Serializable]
public class RandomExecutionModule
{
    public float ChanceWeight { get; set; }
    public bool ForceExecute { get; set; }
    public float ActionDelay { get; set; }

    public static RandomExecutionModule GetSingleton<T>()
        where T : RandomExecutionModule, new()
    {
        if (!ZAMERTPlugin.Singleton.TypeSingletonPair.TryGetValue(typeof(T), out RandomExecutionModule type))
        {
            ZAMERTPlugin.Singleton.TypeSingletonPair.Add(typeof(T), type = new T());
        }
        return type;
    }

    public static List<T> SelectList<T>(List<T> list)
        where T : RandomExecutionModule, new()
    {
        float chance = list.Sum(x => x.ChanceWeight);
        chance = UnityEngine.Random.Range(0f, chance);
        List<T> output = new List<T> { };
        foreach (T element in list)
        {
            if (element.ForceExecute)
            {
                output.Add(element);
            }
            else
            {
                if (chance <= 0)
                {
                    continue;
                }
                chance -= element.ChanceWeight;
                if (chance <= 0)
                {
                    output.Add(element);
                }
            }
        }
        return output;
    }

    public static void Execute<T>(List<T> list, ModuleGeneralArguments args)
        where T : RandomExecutionModule, new()
    {
        SelectList(list).ForEach(x => x.Execute(args));
    }

    public static Player[] GetTargets(SendType type, ModuleGeneralArguments args)
    {
        List<Player> targets = new List<Player> { };
        if (type.HasFlag(SendType.AllExceptAboveOne))
        {
            targets.AddRange(Player.List.Where(x => x != args.Player));
        }
        if (type.HasFlag(SendType.Spectators))
        {
            targets.AddRange(Player.List.Where(x => !x.IsAlive));
        }
        if (type.HasFlag(SendType.Alive))
        {
            targets.AddRange(Player.List.Where(x => x.IsAlive));
        }
        if (type.HasFlag(SendType.Interactor))
        {
            targets.Add(args.Player);
        }

        return targets.Distinct().ToArray();
    }

    public virtual void Execute(ModuleGeneralArguments args) { }
}

[Serializable]
public class FRandomExecutionModule
{
    public ScriptValue ChanceWeight { get; set; }
    public ScriptValue ForceExecute { get; set; }
    public ScriptValue ActionDelay { get; set; }
    private float calcedWeight;

    public static List<T> SelectList<T>(List<T> list, FunctionArgument args)
        where T : FRandomExecutionModule, new()
    {
        float chance = list.Sum(x => x.calcedWeight = x.ChanceWeight.GetValue(args, 0f));
        chance = UnityEngine.Random.Range(0f, chance);
        List<T> output = new List<T> { };
        foreach (T element in list)
        {
            if (element.ForceExecute.GetValue(args, false))
            {
                output.Add(element);
            }
            else
            {
                if (chance <= 0)
                {
                    continue;
                }
                chance -= element.calcedWeight;
                if (chance <= 0)
                {
                    output.Add(element);
                }
            }
        }
        return output;
    }

    public static void Execute<T>(List<T> list, FunctionArgument args)
        where T : FRandomExecutionModule, new()
    {
        SelectList(list, args).ForEach(x => x.Execute(args));
    }

    public virtual void Execute(FunctionArgument args) { }
}

[Serializable]
public class CGNModule : RandomExecutionModule
{
    public int GroovieNoiseId { get; set; }
    public string GroovieNoiseGroup { get; set; }

    public override void Execute(ModuleGeneralArguments args)
    {
        System.Action action = () =>
        {
            Dictionary<int, ZAMERTInteractable> codeDict;
            if (!ZAMERTPlugin.Singleton.CodeClassPair.TryGetValue(args.Schematic, out codeDict))
                return;
            Dictionary<string, List<ZAMERTInteractable>> groupDict;
            ZAMERTPlugin.Singleton.ZAMERTGroup.TryGetValue(args.Schematic, out groupDict);

            ZAMERTInteractable v;
            if (codeDict.TryGetValue(GroovieNoiseId, out v))
            {
                GroovyNoise gn = v as GroovyNoise;
                if (gn != null)
                    gn.Fire();
                else
                    v.Active = true;
            }

            if (groupDict != null)
            {
                List<ZAMERTInteractable> vs;
                if (groupDict.TryGetValue(GroovieNoiseGroup, out vs))
                {
                    foreach (ZAMERTInteractable item in vs)
                    {
                        GroovyNoise gn = item as GroovyNoise;
                        if (gn != null)
                            gn.Fire();
                        else
                            item.Active = true;
                    }
                }
            }
        };

        if (ActionDelay <= 0f)
            action();
        else
            MEC.Timing.CallDelayed(ActionDelay, action);
    }
}

[Serializable]
public class FCGNModule : FRandomExecutionModule
{
    public ScriptValue GroovieNoiseId { get; set; }
    public ScriptValue GroovieNoiseGroup { get; set; }

    public override void Execute(FunctionArgument args)
    {
        float delay   = ActionDelay.GetValue(args, 0f);
        int   noiseId = GroovieNoiseId.GetValue(args, 0);
        string noiseGroup = GroovieNoiseGroup.GetValue(args, "");

        System.Action action = () =>
        {
            Dictionary<int, ZAMERTInteractable> codeDict;
            if (!ZAMERTPlugin.Singleton.CodeClassPair.TryGetValue(args.Schematic, out codeDict))
                return;
            Dictionary<string, List<ZAMERTInteractable>> groupDict;
            ZAMERTPlugin.Singleton.ZAMERTGroup.TryGetValue(args.Schematic, out groupDict);

            ZAMERTInteractable v;
            if (codeDict.TryGetValue(noiseId, out v))
            {
                FGroovyNoise fgn = v as FGroovyNoise;
                if (fgn != null)
                    fgn.FireF(args);
                else
                {
                    GroovyNoise gn = v as GroovyNoise;
                    if (gn != null)
                        gn.Fire();
                    else
                        v.Active = true;
                }
            }

            if (groupDict != null)
            {
                List<ZAMERTInteractable> vs;
                if (groupDict.TryGetValue(noiseGroup, out vs))
                {
                    foreach (ZAMERTInteractable item in vs)
                    {
                        FGroovyNoise fgni = item as FGroovyNoise;
                        if (fgni != null)
                            fgni.FireF(args);
                        else
                        {
                            GroovyNoise gni = item as GroovyNoise;
                            if (gni != null)
                                gni.Fire();
                            else
                                item.Active = true;
                        }
                    }
                }
            }
        };

        if (delay <= 0f)
            action();
        else
            MEC.Timing.CallDelayed(delay, action);
    }
}

[Serializable]
public class GMDTO : RandomExecutionModule
{
    public List<int> Targets { get; set; }
    public List<string> TargetGroups { get; set; }
    public bool Enable { get; set; }

    public override void Execute(ModuleGeneralArguments args)
    {
        System.Action action = () =>
        {
            Dictionary<int, ZAMERTInteractable> codeDict;
            if (!ZAMERTPlugin.Singleton.CodeClassPair.TryGetValue(args.Schematic, out codeDict))
                return;
            Dictionary<string, List<ZAMERTInteractable>> groupDict;
            ZAMERTPlugin.Singleton.ZAMERTGroup.TryGetValue(args.Schematic, out groupDict);

            foreach (int x in Targets)
            {
                ZAMERTInteractable v;
                if (codeDict.TryGetValue(x, out v))
                    v.Active = Enable;
            }
            if (groupDict != null)
            {
                foreach (string x in TargetGroups)
                {
                    List<ZAMERTInteractable> vs;
                    if (groupDict.TryGetValue(x, out vs))
                        vs.ForEach(y => y.Active = Enable);
                }
            }
        };

        if (ActionDelay <= 0f)
            action();
        else
            MEC.Timing.CallDelayed(ActionDelay, action);
    }
}

[Serializable]
public class FGMDTO : FRandomExecutionModule
{
    public ScriptValue Targets { get; set; }
    public ScriptValue TargetGroups { get; set; }
    public ScriptValue Enable { get; set; }

    public override void Execute(FunctionArgument args)
    {
        float delay = ActionDelay.GetValue(args, 0f);

        System.Action action = () =>
        {
            Dictionary<int, ZAMERTInteractable> codeDict;
            if (!ZAMERTPlugin.Singleton.CodeClassPair.TryGetValue(args.Schematic, out codeDict))
                return;
            Dictionary<string, List<ZAMERTInteractable>> groupDict;
            ZAMERTPlugin.Singleton.ZAMERTGroup.TryGetValue(args.Schematic, out groupDict);

            object[] tArr = Targets.GetValue(args) as object[];
            if (tArr != null)
            {
                foreach (object x in tArr)
                {
                    int id = x is int ? (int)x
                           : x is float ? Mathf.RoundToInt((float)x)
                           : -1;
                    if (id < 0) continue;
                    ZAMERTInteractable v;
                    if (codeDict.TryGetValue(id, out v))
                        v.Active = Enable.GetValue(args, v.Active);
                }
            }

            if (groupDict != null)
            {
                object[] gArr = TargetGroups.GetValue(args) as object[];
                if (gArr != null)
                {
                    foreach (object x in gArr)
                    {
                        string s = x as string;
                        if (s == null) continue;
                        List<ZAMERTInteractable> vs;
                        if (groupDict.TryGetValue(s, out vs))
                        {
                            bool en = Enable.GetValue(args, false);
                            vs.ForEach(y => y.Active = en);
                        }
                    }
                }
            }
        };

        if (delay <= 0f)
            action();
        else
            MEC.Timing.CallDelayed(delay, action);
    }
}

[Serializable]
public class EffectGivingModule : RandomExecutionModule
{
    public EffectFlagE EffectFlag { get; set; }

    public EffectType effectType { get; set; }
    public SendType GivingTo { get; set; }
    public byte Inensity { get; set; }
    public float Duration { get; set; }

    public override void Execute(ModuleGeneralArguments args)
    {
        MEC.Timing.CallDelayed(ActionDelay, () =>
        {
            if (!args.TargetCalculated)
            {
                args.Targets = GetTargets(GivingTo, args);
            }

            string effectName = effectType.ToString();
            Log.Debug($"Giving effect: {effectName} (intensity={Inensity}, duration={Duration}, effectFlag={EffectFlag})");
            foreach (Player player in args.Targets)
            {
                if (player.TryGetEffect(effectName, out StatusEffectBase effect))
                {
                    if (EffectFlag.HasFlag(EffectFlagE.Disable))
                    {
                        player.DisableEffect(effect);
                    }
                    else if (EffectFlag.HasFlag(EffectFlagE.Enable))
                    {
                        byte intensity = Inensity;
                        if (EffectFlag.HasFlag(EffectFlagE.ModifyIntensity))
                        {
                            intensity += effect.Intensity;
                        }
                        player.EnableEffect(effect, intensity, Duration, addDuration: EffectFlag.HasFlag(EffectFlagE.ModifyDuration));
                    }
                }
                else
                {
                    Log.Warn($"Attempted to modify effect '{effectName}' on player '{player.Nickname}' but the effect does not exist");
                }
            }
        });
    }
}

[Serializable]
public class FEffectGivingModule : FRandomExecutionModule
{
    public ScriptValue EffectFlag { get; set; }
    public ScriptValue effectType { get; set; }
    public ScriptValue GivingTo { get; set; }
    public ScriptValue Inensity { get; set; }
    public ScriptValue Duration { get; set; }

    public override void Execute(FunctionArgument args)
    {
        MEC.Timing.CallDelayed(ActionDelay.GetValue(args, 0f), () =>
        {
            EffectFlagE effectFlag = EffectFlag.GetValue<EffectFlagE>(args, 0);
            EffectType effectValue = effectType.GetValue<EffectType>(args, 0);
            string effectName = effectValue.ToString();
            byte intensity = (byte)Inensity.GetValue(args, 0);
            float duration = Duration.GetValue(args, 0);
            Log.Debug($"Giving effect: {effectName} (intensity={intensity}, duration={duration}, effectFlag={effectFlag})");
            foreach (Player player in GivingTo.GetValue(args, new Player[] { }))
            {
                if (player.TryGetEffect(effectName, out StatusEffectBase effect))
                {
                    if (effectFlag.HasFlag(EffectFlagE.Disable))
                    {
                        player.DisableEffect(effect);
                    }
                    else if (effectFlag.HasFlag(EffectFlagE.Enable))
                    {
                        if (effectFlag.HasFlag(EffectFlagE.ModifyIntensity))
                        {
                            intensity += effect.Intensity;
                        }
                        player.EnableEffect(effect, intensity, duration, addDuration: effectFlag.HasFlag(EffectFlagE.ModifyDuration));
                    }
                }
                else
                {
                    Log.Warn($"Attempted to modify effect '{effectName}' on player '{player.Nickname}' but the effect does not exist");
                }
            }
        });
    }
}

[Serializable]
public class ExplodeModule : RandomExecutionModule
{
    public bool FFon { get; set; }
    public bool EffectOnly { get; set; }
    public SVector3 LocalPosition { get; set; }

    public override void Execute(ModuleGeneralArguments args)
    {
        ReferenceHub.TryGetLocalHub(out ReferenceHub local);

        Vector3 position = args.Transform != null
            ? args.Transform.TransformPoint(LocalPosition)
            : (Vector3)LocalPosition;
        MEC.Timing.CallDelayed(ActionDelay, () =>
        {
            if (EffectOnly)
            {
                ExplosionUtils.ServerSpawnEffect(position, ItemType.GrenadeHE);
            }
            else
            {
                ReferenceHub hub;
                if (args.Player != null && args.Player.ReferenceHub != null)
                    hub = args.Player.ReferenceHub;
                else
                    ReferenceHub.TryGetLocalHub(out hub);
                if (hub != null)
                    ExplosionUtils.ServerExplode(position, FFon ? new Footprint(local) : new Footprint(hub), ExplosionType.Grenade);
            }
        });
    }
}

[Serializable]
public class FExplodeModule : FRandomExecutionModule
{
    public ScriptValue FFon { get; set; }
    public ScriptValue EffectOnly { get; set; }
    public ScriptValue LocalPosition { get; set; }

    public override void Execute(FunctionArgument args)
    {
        ReferenceHub.TryGetLocalHub(out ReferenceHub local);

        Vector3 localPos = LocalPosition.GetValue(args, Vector3.zero);
        Vector3 position = args.Transform != null
            ? args.Transform.TransformPoint(localPos)
            : localPos;
        MEC.Timing.CallDelayed(ActionDelay.GetValue(args, 0f), () =>
        {
            if (EffectOnly.GetValue(args, true))
            {
                ExplosionUtils.ServerSpawnEffect(position, ItemType.GrenadeHE);
            }
            else
            {
                ReferenceHub hub;
                if (args.Player != null && args.Player.ReferenceHub != null)
                    hub = args.Player.ReferenceHub;
                else
                    ReferenceHub.TryGetLocalHub(out hub);
                if (hub != null)
                    ExplosionUtils.ServerExplode(position, FFon.GetValue(args, false) ? new Footprint(local) : new Footprint(hub), ExplosionType.Grenade);
            }
        });
    }
}

[Serializable]
public class AudioModule : RandomExecutionModule
{
    public string AudioName { get; set; }
    [Header("0: Loop")]
    public int PlayCount { get; set; }
    public bool IsSpatial { get; set; }
    public float MaxDistance { get; set; }
    public float MinDistance { get; set; }
    public float Volume { get; set; }
    public SVector3 LocalPlayPosition { get; set; }
    public AudioPlayer AudioPlayer { get; set; }
    private bool loaded;

    public override void Execute(ModuleGeneralArguments args)
    {
        MEC.Timing.CallDelayed(ActionDelay, () =>
        {
            if (!loaded)
            {
                if (!Directory.Exists(ZAMERTPlugin.Singleton.Config.AudioFolderPath))
                {
                    ServerConsole.AddLog("Cannot find Audio Folder Directory!", ConsoleColor.Red);
                    return;
                }
                if (!AudioClipStorage.AudioClips.ContainsKey(AudioName))
                {
                    AudioClipStorage.LoadClip(Path.Combine(ZAMERTPlugin.Singleton.Config.AudioFolderPath, AudioName), AudioName);
                }

                loaded = true;
            }

            if (AudioPlayer == null)
            {
                AudioPlayer = AudioPlayer.Create($"AudioHandler-{args.Transform.GetHashCode()}-{GetHashCode()}");
                Speaker speaker = AudioPlayer.AddSpeaker("Primary", args.Transform.TransformPoint(LocalPlayPosition), Volume, IsSpatial, MinDistance, MaxDistance);
                AudioPlayer.transform.parent = speaker.transform.parent = args.Transform;
                AudioPlayer.transform.localPosition = speaker.transform.localPosition = LocalPlayPosition;
            }
            if (PlayCount == 0)
            {
                AudioPlayer.AddClip(AudioName, Volume, true, false);
            }
            for (int i = 0; i < PlayCount; i++)
            {
                AudioPlayer.AddClip(AudioName, Volume, false, false);
            }
        });
    }
}

[Serializable]
public class FAudioModule : FRandomExecutionModule
{
    public ScriptValue AudioName { get; set; }
    [Header("0: Loop")]
    public ScriptValue PlayCount { get; set; }
    public ScriptValue IsSpatial { get; set; }
    public ScriptValue MaxDistance { get; set; }
    public ScriptValue MinDistance { get; set; }
    public ScriptValue Volume { get; set; }
    public ScriptValue LocalPlayPosition { get; set; }
    public AudioPlayer AudioPlayer { get; set; }

    public override void Execute(FunctionArgument args)
    {
        MEC.Timing.CallDelayed(ActionDelay.GetValue(args, 0f), () =>
        {
            string audioName = AudioName.GetValue<string>(args, null);
            if (audioName == null)
            {
                return;
            }
            if (!Directory.Exists(ZAMERTPlugin.Singleton.Config.AudioFolderPath))
            {
                ServerConsole.AddLog("Cannot find Audio Folder Directory!", ConsoleColor.Red);
                return;
            }
            if (!AudioClipStorage.AudioClips.ContainsKey(audioName))
            {
                AudioClipStorage.LoadClip(Path.Combine(ZAMERTPlugin.Singleton.Config.AudioFolderPath, audioName), audioName);
            }

            Vector3 vector = LocalPlayPosition.GetValue(args, Vector3.zero);
            float vol = Volume.GetValue(args, 1f);
            if (AudioPlayer == null)
            {
                AudioPlayer = AudioPlayer.Create($"AudioHandler-{args.Transform.GetHashCode()}-{GetHashCode()}");
                Speaker speaker = AudioPlayer.AddSpeaker($"Primary-{audioName}", args.Transform.TransformPoint(vector), vol, IsSpatial.GetValue(args, true), MinDistance.GetValue(args, 5f), MaxDistance.GetValue(args, 5f));
            }
            AudioPlayer.SpeakersByName[$"Primary-{audioName}"].transform.parent = args.Transform;
            AudioPlayer.SpeakersByName[$"Primary-{audioName}"].transform.localPosition = vector;
            int count = PlayCount.GetValue(args, 1);
            if (count == 0)
            {
                AudioPlayer.AddClip(audioName, vol, true, false);
            }
            for (int i = 0; i < count; i++)
            {
                AudioPlayer.AddClip(audioName, vol, false, false);
            }
        });
    }
}

[Serializable]
public class MessageModule : RandomExecutionModule
{
    public SendType SendType { get; set; }
    public string MessageContent { get; set; }
    public MessageTypeE MessageType { get; set; }
    public float Duration { get; set; }

    public override void Execute(ModuleGeneralArguments args)
    {
        MEC.Timing.CallDelayed(ActionDelay, () =>
        {
            string content = MessageContent;
            foreach (string v in args.Interpolations.Keys)
            {
                try
                {
                    content = content.Replace(v, args.Interpolations[v](args.InterpolationsList));
                }
                catch (Exception) { }
            }
            try
            {
                content = ServerConsole.Singleton.NameFormatter.ProcessExpression(content);
            }
            catch (Exception) { }
            if (!args.TargetCalculated)
            {
                args.Targets = GetTargets(SendType, args);
            }

            if (MessageType == MessageTypeE.Cassie)
            {
                Announcer.Message(content, playBackground: true);
            }
            else
            {
                foreach (Player p in args.Targets)
                {
                    if (MessageType == MessageTypeE.BroadCast)
                    {
                        p.SendBroadcast(content, (ushort)Math.Round(Duration));
                    }
                    else
                    {
                        p.SendHint(content, Duration);
                    }
                }
            }
        });
    }
}

[Serializable]
public class FMessageModule : FRandomExecutionModule
{
    public ScriptValue SendType { get; set; }
    public ScriptValue MessageContent { get; set; }
    public ScriptValue MessageType { get; set; }
    public ScriptValue Duration { get; set; }

    public override void Execute(FunctionArgument args)
    {
        MEC.Timing.CallDelayed(ActionDelay.GetValue(args, 0f), () =>
        {
            string content = MessageContent.GetValue(args, "");
            MessageTypeE type = MessageType.GetValue(args, MessageTypeE.BroadCast);
            if (type == MessageTypeE.Cassie)
            {
                Announcer.Message(content, playBackground: true);
            }
            else
            {
                foreach (Player p in SendType.GetValue(args, new Player[] { }))
                {
                    if (type == MessageTypeE.BroadCast)
                    {
                        p.SendBroadcast(content, (ushort)Math.Round(Duration.GetValue(args, 0f)));
                    }
                    else
                    {
                        p.SendHint(content, Duration.GetValue(args, 0f));
                    }
                }
            }
        });
    }
}

[Serializable]
public class AnimationDTO : RandomExecutionModule
{
    public Animator Animator { get; set; }
    [HideInInspector]
    public string AnimatorAdress { get; set; }
    public string AnimationName { get; set; }
    public AnimationTypeE AnimationType { get; set; }
    public ParameterTypeE ParameterType { get; set; }
    public string ParameterName { get; set; }
    [Header("If parameter type is bool or trigger, input 0 for false, and input 1 for true.")]
    public string ParameterValue { get; set; }

    public override void Execute(ModuleGeneralArguments args)
    {
        MEC.Timing.CallDelayed(ActionDelay, () =>
        {
            if (Animator == null)
            {
                if (!ZAMERTEventHandlers.FindObjectWithPath(args.Schematic.GetComponentInParent<SchematicObject>().transform, AnimatorAdress).TryGetComponent(out Animator animator))
                {
                    ServerConsole.AddLog("Cannot find appopriate animator!");
                    return;
                }
                Animator = animator;
            }
            if (AnimationType == AnimationTypeE.Start)
            {
                Animator.Play(AnimationName);
                Animator.speed = 1f;
            }
            else if (AnimationType == AnimationTypeE.Stop)
            {
                Animator.speed = 0f;
            }
            else
            {
                switch (ParameterType)
                {
                    case ParameterTypeE.Bool:
                        Animator.SetBool(ParameterName, ParameterValue == "1");
                        break;
                    case ParameterTypeE.Float:
                        Animator.SetFloat(ParameterName, float.Parse(ParameterValue));
                        break;
                    case ParameterTypeE.Integer:
                        Animator.SetInteger(ParameterName, int.Parse(ParameterValue));
                        break;
                    case ParameterTypeE.Trigger:
                        Animator.SetTrigger(ParameterName);
                        break;
                }
            }
        });
    }
}

[Serializable]
public class FAnimationDTO : FRandomExecutionModule
{
    public Animator Animator { get; set; }
    [HideInInspector]
    public string AnimatorAdress { get; set; }
    public ScriptValue AnimationName { get; set; }
    public ScriptValue AnimationType { get; set; }
    public ScriptValue ParameterType { get; set; }
    public ScriptValue ParameterName { get; set; }
    [Header("If parameter type is bool or trigger, input 0 for false, and input 1 for true.")]
    public ScriptValue ParameterValue { get; set; }

    public override void Execute(FunctionArgument args)
    {
        MEC.Timing.CallDelayed(ActionDelay.GetValue(args, 0f), () =>
        {
            if (Animator == null)
            {
                if (!ZAMERTEventHandlers.FindObjectWithPath(args.Schematic.GetComponentInParent<SchematicObject>().transform, AnimatorAdress).TryGetComponent(out Animator animator))
                {
                    ServerConsole.AddLog("Cannot find appopriate animator!");
                    return;
                }
                Animator = animator;
            }
            AnimationTypeE type = AnimationType.GetValue(args, AnimationTypeE.Start);
            if (type == AnimationTypeE.Start)
            {
                Animator.Play(AnimationName.GetValue(args, ""));
                Animator.speed = 1f;
            }
            else if (type == AnimationTypeE.Stop)
            {
                Animator.speed = 0f;
            }
            else
            {
                string pm = ParameterName.GetValue<string>(args, null);
                if (pm == null)
                {
                    return;
                }

                switch (ParameterType.GetValue(args, ParameterTypeE.Integer))
                {
                    case ParameterTypeE.Bool:
                        Animator.SetBool(pm, ParameterValue.GetValue(args, "1") == "1");
                        break;
                    case ParameterTypeE.Float:
                        Animator.SetFloat(pm, ParameterValue.GetValue(args, 0f));
                        break;
                    case ParameterTypeE.Integer:
                        Animator.SetInteger(pm, ParameterValue.GetValue(args, 0));
                        break;
                    case ParameterTypeE.Trigger:
                        Animator.SetTrigger(pm);
                        break;
                }
            }
        });
    }
}

[Serializable]
public class DropItem : RandomExecutionModule
{
    public ItemType ItemType { get; set; }
    public uint CustomItemId { get; set; }
    public int Count { get; set; }
    public SVector3 DropLocalPosition { get; set; }

    public override void Execute(ModuleGeneralArguments args)
    {
        MEC.Timing.CallDelayed(ActionDelay, () =>
        {

            Vector3 position = args.Transform.TransformPoint(DropLocalPosition);
            if (CustomItemId != 0 && Item.TryGet((ushort)CustomItemId, out Item customItem))
            {
                for (int i = 0; i < Count; i++)
                {
                    Pickup customPickup = Pickup.Create(ItemType, position);

                    customPickup.Spawn();
                }
            }
            else
            {
                if (!InventoryItemLoader.AvailableItems.TryGetValue(ItemType, out ItemBase itemBase) || itemBase.PickupDropModel == null)
                {
                    return;
                }
                for (int i = 0; i < Count; i++)
                {
                    ItemPickupBase itemPickupBase = UnityEngine.Object.Instantiate<ItemPickupBase>(itemBase.PickupDropModel, position, args.Transform.rotation);
                    itemPickupBase.NetworkInfo = new PickupSyncInfo(ItemType, itemBase.Weight, 0, false);
                    NetworkServer.Spawn(itemPickupBase.gameObject);
                }
            }
        });
    }
}

[Serializable]
public class FDropItem : FRandomExecutionModule
{
    public ScriptValue ItemType { get; set; }
    public ScriptValue CustomItemId { get; set; }
    public ScriptValue Count { get; set; }
    public ScriptValue DropLocalPosition { get; set; }

    public override void Execute(FunctionArgument args)
    {
        MEC.Timing.CallDelayed(ActionDelay.GetValue(args, 0f), () =>
        {
            Vector3 position = args.Transform.TransformPoint(DropLocalPosition.GetValue(args, Vector3.zero));
            int u = CustomItemId.GetValue(args, 0);
            int c = Count.GetValue(args, 1);
            if (u != 0 && Item.TryGet((ushort)u, out Item customItem))
            {
                for (int i = 0; i < c; i++)
                {
                    Pickup customPickup = Pickup.Create(ItemType.GetValue(args, global::ItemType.None), position);

                    customPickup.Spawn();
                }
            }
            else
            {
                ItemType value = ItemType.GetValue(args, global::ItemType.KeycardJanitor);
                if (!InventoryItemLoader.AvailableItems.TryGetValue(value, out ItemBase itemBase) || itemBase.PickupDropModel == null)
                {
                    return;
                }
                for (int i = 0; i < c; i++)
                {
                    ItemPickupBase itemPickupBase = UnityEngine.Object.Instantiate<ItemPickupBase>(itemBase.PickupDropModel, position, args.Transform.rotation);
                    itemPickupBase.NetworkInfo = new PickupSyncInfo(value, itemBase.Weight, 0, false);
                    NetworkServer.Spawn(itemPickupBase.gameObject);
                }
            }
        });
    }
}

[Serializable]
public class CFEModule : RandomExecutionModule
{
    public string FunctionName { get; set; }

    public override void Execute(ModuleGeneralArguments args)
    {
        MEC.Timing.CallDelayed(ActionDelay, () =>
        {
            if (ZAMERTPlugin.Singleton.FunctionExecutors[args.Schematic].TryGetValue(FunctionName, out FunctionExecutor function))
            {
                function.Data.Execute(new FunctionArgument { Player = args.Player });
            }
        });
    }
}

[Serializable]
public class FCFEModule : FRandomExecutionModule
{
    public ScriptValue FunctionName { get; set; }
    public List<ScriptValue> FunctionArguments { get; set; }

    public override void Execute(FunctionArgument args)
    {
        MEC.Timing.CallDelayed(ActionDelay.GetValue(args, 0f), () =>
        {
            if (ZAMERTPlugin.Singleton.FunctionExecutors[args.Schematic].TryGetValue(FunctionName.GetValue(args, ""), out FunctionExecutor function))
            {
                function.Data.Execute(new FunctionArgument { Arguments = FunctionArguments.Select(x => x.GetValue(args)).ToList(), Player = args.Player, Schematic = args.Schematic });
            }
        });
    }
}

[Serializable]
public class Commanding : RandomExecutionModule
{
    public string CommandContext { get; set; }

    public override void Execute(ModuleGeneralArguments args)
    {
        MEC.Timing.CallDelayed(ActionDelay, () =>
        {
            string content = CommandContext;
            foreach (string v in args.Interpolations.Keys)
            {
                try
                {
                    content = content.Replace(v, args.Interpolations[v](args.InterpolationsList));
                }
                catch (Exception) { }
            }
            content = ServerConsole.Singleton.NameFormatter.ProcessExpression(content);
            ZAMERTPlugin.ExecuteCommand(content);
        });
    }
}

[Serializable]
public class FCommanding : FRandomExecutionModule
{
    public ScriptValue CommandContext { get; set; }

    public override void Execute(FunctionArgument args)
    {
        MEC.Timing.CallDelayed(ActionDelay.GetValue(args, 0f), () =>
        {
            ZAMERTPlugin.ExecuteCommand(CommandContext.GetValue(args, ""));
        });
    }
}

[Serializable]
public class PrimitiveModifyModule : RandomExecutionModule
{

    public string TargetName { get; set; }
    public PrimitiveModifyType ModifyType { get; set; }
    public SColor TargetColor { get; set; }
    public SVector3 TargetScale { get; set; }
    public bool Visible { get; set; }
    public bool Collidable { get; set; }

    public float LerpDuration { get; set; }

    public override void Execute(ModuleGeneralArguments args)
    {
        MEC.Timing.CallDelayed(ActionDelay, () =>
        {
            if (args.Transform == null) return;
            foreach (AdminToys.PrimitiveObjectToy toy in GetTargets(args.Transform))
            {
                LabApi.Features.Wrappers.PrimitiveObjectToy wrapper = LabApi.Features.Wrappers.PrimitiveObjectToy.Get(toy);
                if (wrapper == null) continue;
                if (LerpDuration <= 0f)
                    ApplyInstant(wrapper, toy.transform);
                else
                    MEC.Timing.RunCoroutine(LerpApply(wrapper, toy.transform));
            }
        });
    }

    private IEnumerable<AdminToys.PrimitiveObjectToy> GetTargets(Transform root)
    {
        if (string.IsNullOrEmpty(TargetName))
        {
            AdminToys.PrimitiveObjectToy self = root.GetComponent<AdminToys.PrimitiveObjectToy>();
            if (self != null) yield return self;
        }
        else if (TargetName == "*")
        {
            foreach (AdminToys.PrimitiveObjectToy t in root.GetComponentsInChildren<AdminToys.PrimitiveObjectToy>())
                yield return t;
        }
        else
        {
            Transform found = root.Find(TargetName);
            if (found != null)
            {
                AdminToys.PrimitiveObjectToy t = found.GetComponent<AdminToys.PrimitiveObjectToy>();
                if (t != null) yield return t;
            }
        }
    }

    private void ApplyInstant(LabApi.Features.Wrappers.PrimitiveObjectToy wrapper, Transform t)
    {
        if (ModifyType.HasFlag(PrimitiveModifyType.Color) && TargetColor != null)
            wrapper.Color = TargetColor;
        if (ModifyType.HasFlag(PrimitiveModifyType.Scale) && TargetScale != null)
            t.localScale = TargetScale;
        if (ModifyType.HasFlag(PrimitiveModifyType.Visibility))
        {
            AdminToys.PrimitiveFlags flags = wrapper.Flags;
            if (Visible) flags |= AdminToys.PrimitiveFlags.Visible;
            else flags &= ~AdminToys.PrimitiveFlags.Visible;
            if (Collidable) flags |= AdminToys.PrimitiveFlags.Collidable;
            else flags &= ~AdminToys.PrimitiveFlags.Collidable;
            wrapper.Flags = flags;
        }
    }

    private IEnumerator<float> LerpApply(LabApi.Features.Wrappers.PrimitiveObjectToy wrapper, Transform t)
    {
        Color startColor = wrapper.Color;
        Vector3 startScale = t.localScale;
        float elapsed = 0f;
        while (elapsed < LerpDuration)
        {
            elapsed += MEC.Timing.DeltaTime;
            float frac = Mathf.Clamp01(elapsed / LerpDuration);
            if (ModifyType.HasFlag(PrimitiveModifyType.Color) && TargetColor != null)
                wrapper.Color = Color.Lerp(startColor, TargetColor, frac);
            if (ModifyType.HasFlag(PrimitiveModifyType.Scale) && TargetScale != null)
                t.localScale = Vector3.Lerp(startScale, TargetScale, frac);
            yield return MEC.Timing.WaitForOneFrame;
        }
        ApplyInstant(wrapper, t);
    }
}

[Serializable]
public class FPrimitiveModifyModule : FRandomExecutionModule
{
    public ScriptValue TargetName { get; set; }
    public PrimitiveModifyType ModifyType { get; set; }
    public ScriptValue TargetColor { get; set; }
    public ScriptValue TargetScale { get; set; }
    public ScriptValue Visible { get; set; }
    public ScriptValue Collidable { get; set; }
    public ScriptValue LerpDuration { get; set; }

    public override void Execute(FunctionArgument args)
    {
        MEC.Timing.CallDelayed(ActionDelay.GetValue(args, 0f), () =>
        {
            if (args.Transform == null) return;
            string targetName = TargetName.GetValue(args, "");
            float lerpDur = LerpDuration.GetValue(args, 0f);
            foreach (AdminToys.PrimitiveObjectToy toy in GetTargets(args.Transform, targetName))
            {
                LabApi.Features.Wrappers.PrimitiveObjectToy wrapper = LabApi.Features.Wrappers.PrimitiveObjectToy.Get(toy);
                if (wrapper == null) continue;
                if (lerpDur <= 0f)
                    ApplyInstant(wrapper, toy.transform, args);
                else
                    MEC.Timing.RunCoroutine(LerpApply(wrapper, toy.transform, args, lerpDur));
            }
        });
    }

    private IEnumerable<AdminToys.PrimitiveObjectToy> GetTargets(Transform root, string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            AdminToys.PrimitiveObjectToy self = root.GetComponent<AdminToys.PrimitiveObjectToy>();
            if (self != null) yield return self;
        }
        else if (name == "*")
        {
            foreach (AdminToys.PrimitiveObjectToy t in root.GetComponentsInChildren<AdminToys.PrimitiveObjectToy>())
                yield return t;
        }
        else
        {
            Transform found = root.Find(name);
            if (found != null)
            {
                AdminToys.PrimitiveObjectToy t = found.GetComponent<AdminToys.PrimitiveObjectToy>();
                if (t != null) yield return t;
            }
        }
    }

    private void ApplyInstant(LabApi.Features.Wrappers.PrimitiveObjectToy wrapper, Transform t, FunctionArgument args)
    {
        if (ModifyType.HasFlag(PrimitiveModifyType.Color))
            wrapper.Color = TargetColor.GetValue(args, wrapper.Color);
        if (ModifyType.HasFlag(PrimitiveModifyType.Scale))
            t.localScale = TargetScale.GetValue(args, t.localScale);
        if (ModifyType.HasFlag(PrimitiveModifyType.Visibility))
        {
            AdminToys.PrimitiveFlags flags = wrapper.Flags;
            if (Visible.GetValue(args, true)) flags |= AdminToys.PrimitiveFlags.Visible;
            else flags &= ~AdminToys.PrimitiveFlags.Visible;
            if (Collidable.GetValue(args, false)) flags |= AdminToys.PrimitiveFlags.Collidable;
            else flags &= ~AdminToys.PrimitiveFlags.Collidable;
            wrapper.Flags = flags;
        }
    }

    private IEnumerator<float> LerpApply(LabApi.Features.Wrappers.PrimitiveObjectToy wrapper, Transform t, FunctionArgument args, float dur)
    {
        Color startColor = wrapper.Color;
        Vector3 startScale = t.localScale;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += MEC.Timing.DeltaTime;
            float frac = Mathf.Clamp01(elapsed / dur);
            if (ModifyType.HasFlag(PrimitiveModifyType.Color))
                wrapper.Color = Color.Lerp(startColor, TargetColor.GetValue(args, startColor), frac);
            if (ModifyType.HasFlag(PrimitiveModifyType.Scale))
                t.localScale = Vector3.Lerp(startScale, TargetScale.GetValue(args, startScale), frac);
            yield return MEC.Timing.WaitForOneFrame;
        }
        ApplyInstant(wrapper, t, args);
    }
}

[Serializable]
public class LoopSpeakerControlModule : RandomExecutionModule
{
    public int TargetSpeakerCode { get; set; }
    public string TargetSpeakerGroup { get; set; }
    public LoopSpeakerAction Action { get; set; }
    public string NewAudioName { get; set; }
    public float NewVolume { get; set; }

    public override void Execute(ModuleGeneralArguments args)
    {
        MEC.Timing.CallDelayed(ActionDelay, () =>
        {
            Dictionary<int, ZAMERTInteractable> codeDict;
            if (!ZAMERTPlugin.Singleton.CodeClassPair.TryGetValue(args.Schematic, out codeDict))
                return;

            List<LoopSpeaker> targets = new List<LoopSpeaker>();

            if (TargetSpeakerCode != 0 && codeDict.TryGetValue(TargetSpeakerCode, out ZAMERTInteractable v))
            {
                if (v is LoopSpeaker ls) targets.Add(ls);
            }

            if (!string.IsNullOrEmpty(TargetSpeakerGroup))
            {
                Dictionary<string, List<ZAMERTInteractable>> groupDict;
                if (ZAMERTPlugin.Singleton.ZAMERTGroup.TryGetValue(args.Schematic, out groupDict) &&
                    groupDict.TryGetValue(TargetSpeakerGroup, out List<ZAMERTInteractable> group))
                {
                    foreach (ZAMERTInteractable gi in group)
                        if (gi is LoopSpeaker gls && !targets.Contains(gls)) targets.Add(gls);
                }
            }

            foreach (LoopSpeaker speaker in targets)
            {
                if (Action.HasFlag(LoopSpeakerAction.Stop)) speaker.Stop();
                if (Action.HasFlag(LoopSpeakerAction.Play)) speaker.Play(string.IsNullOrEmpty(NewAudioName) ? null : NewAudioName);
                if (Action.HasFlag(LoopSpeakerAction.ChangeClip)) speaker.ChangeClip(NewAudioName);
                if (Action.HasFlag(LoopSpeakerAction.SetVolume)) speaker.SetVolume(NewVolume);
            }
        });
    }
}

[Serializable]
public class FLoopSpeakerControlModule : FRandomExecutionModule
{
    public ScriptValue TargetSpeakerCode { get; set; }
    public ScriptValue TargetSpeakerGroup { get; set; }
    public LoopSpeakerAction Action { get; set; }
    public ScriptValue NewAudioName { get; set; }
    public ScriptValue NewVolume { get; set; }

    public override void Execute(FunctionArgument args)
    {
        MEC.Timing.CallDelayed(ActionDelay.GetValue(args, 0f), () =>
        {
            Dictionary<int, ZAMERTInteractable> codeDict;
            if (!ZAMERTPlugin.Singleton.CodeClassPair.TryGetValue(args.Schematic, out codeDict))
                return;

            List<LoopSpeaker> targets = new List<LoopSpeaker>();
            int code = TargetSpeakerCode.GetValue(args, 0);
            string group = TargetSpeakerGroup.GetValue<string>(args, null);

            if (code != 0 && codeDict.TryGetValue(code, out ZAMERTInteractable v))
            {
                if (v is LoopSpeaker ls) targets.Add(ls);
            }

            if (!string.IsNullOrEmpty(group))
            {
                Dictionary<string, List<ZAMERTInteractable>> groupDict;
                if (ZAMERTPlugin.Singleton.ZAMERTGroup.TryGetValue(args.Schematic, out groupDict) &&
                    groupDict.TryGetValue(group, out List<ZAMERTInteractable> gList))
                {
                    foreach (ZAMERTInteractable gi in gList)
                        if (gi is LoopSpeaker gls && !targets.Contains(gls)) targets.Add(gls);
                }
            }

            string audioName = NewAudioName.GetValue<string>(args, null);
            float vol = NewVolume.GetValue(args, 1f);

            foreach (LoopSpeaker speaker in targets)
            {
                if (Action.HasFlag(LoopSpeakerAction.Stop)) speaker.Stop();
                if (Action.HasFlag(LoopSpeakerAction.Play)) speaker.Play(string.IsNullOrEmpty(audioName) ? null : audioName);
                if (Action.HasFlag(LoopSpeakerAction.ChangeClip)) speaker.ChangeClip(audioName);
                if (Action.HasFlag(LoopSpeakerAction.SetVolume)) speaker.SetVolume(vol);
            }
        });
    }
}

[Serializable]
public class ItemSpawnerControlModule : RandomExecutionModule
{
    public int TargetSpawnerCode { get; set; }
    public string TargetSpawnerGroup { get; set; }
    public ItemSpawnerAction Action { get; set; }

    public override void Execute(ModuleGeneralArguments args)
    {
        MEC.Timing.CallDelayed(ActionDelay, () =>
        {
            Dictionary<int, ZAMERTInteractable> codeDict;
            if (!ZAMERTPlugin.Singleton.CodeClassPair.TryGetValue(args.Schematic, out codeDict))
                return;

            List<ItemSpawner> targets = new List<ItemSpawner>();

            if (TargetSpawnerCode != 0 && codeDict.TryGetValue(TargetSpawnerCode, out ZAMERTInteractable v))
            {
                if (v is ItemSpawner s) targets.Add(s);
            }

            if (!string.IsNullOrEmpty(TargetSpawnerGroup))
            {
                Dictionary<string, List<ZAMERTInteractable>> groupDict;
                if (ZAMERTPlugin.Singleton.ZAMERTGroup.TryGetValue(args.Schematic, out groupDict) &&
                    groupDict.TryGetValue(TargetSpawnerGroup, out List<ZAMERTInteractable> group))
                {
                    foreach (ZAMERTInteractable gi in group)
                        if (gi is ItemSpawner gs && !targets.Contains(gs)) targets.Add(gs);
                }
            }

            foreach (ItemSpawner spawner in targets)
            {
                if (Action.HasFlag(ItemSpawnerAction.Stop))  spawner.Stop();
                if (Action.HasFlag(ItemSpawnerAction.Spawn))  spawner.SpawnItems();
                if (Action.HasFlag(ItemSpawnerAction.Reset))  spawner.Reset();
            }
        });
    }
}

[Serializable]
public class FItemSpawnerControlModule : FRandomExecutionModule
{
    public ScriptValue TargetSpawnerCode { get; set; }
    public ScriptValue TargetSpawnerGroup { get; set; }
    public ItemSpawnerAction Action { get; set; }

    public override void Execute(FunctionArgument args)
    {
        MEC.Timing.CallDelayed(ActionDelay.GetValue(args, 0f), () =>
        {
            Dictionary<int, ZAMERTInteractable> codeDict;
            if (!ZAMERTPlugin.Singleton.CodeClassPair.TryGetValue(args.Schematic, out codeDict))
                return;

            List<ItemSpawner> targets = new List<ItemSpawner>();
            int code = TargetSpawnerCode.GetValue(args, 0);
            string group = TargetSpawnerGroup.GetValue<string>(args, null);

            if (code != 0 && codeDict.TryGetValue(code, out ZAMERTInteractable v))
            {
                if (v is ItemSpawner s) targets.Add(s);
            }

            if (!string.IsNullOrEmpty(group))
            {
                Dictionary<string, List<ZAMERTInteractable>> groupDict;
                if (ZAMERTPlugin.Singleton.ZAMERTGroup.TryGetValue(args.Schematic, out groupDict) &&
                    groupDict.TryGetValue(group, out List<ZAMERTInteractable> gList))
                {
                    foreach (ZAMERTInteractable gi in gList)
                        if (gi is ItemSpawner gs && !targets.Contains(gs)) targets.Add(gs);
                }
            }

            foreach (ItemSpawner spawner in targets)
            {
                if (Action.HasFlag(ItemSpawnerAction.Stop))  spawner.Stop();
                if (Action.HasFlag(ItemSpawnerAction.Spawn))  spawner.SpawnItems();
                if (Action.HasFlag(ItemSpawnerAction.Reset))  spawner.Reset();
            }
        });
    }
}

[Serializable]
public class SColor
{
    public float r;
    public float g;
    public float b;
    public float a = 1f;

    public static implicit operator Color(SColor c) => new Color(c.r, c.g, c.b, c.a);
    public static implicit operator SColor(Color c) => new SColor { r = c.r, g = c.g, b = c.b, a = c.a };
}

[Serializable]
public class SVector3
{
    public float x;
    public float y;
    public float z;

        public static implicit operator Vector3(SVector3 sVector)
        {
            return new Vector3(sVector.x, sVector.y, sVector.z);
        }
    }

[Serializable]
public class GateSerializable
{
    public DoorPermissionFlags DoorPermissions { get; set; }
    public bool RequireAllPermission { get; set; }
    public bool IsLocked { get; set; }
    public bool IsOpened { get; set; }
}

[Serializable]
public class WhitelistWeapon
{
    public ItemType ItemType { get; set; }
    public uint CustomItemId { get; set; }
}

[Serializable]
public class FWhitelistWeapon : Value
{
    public ScriptValue ItemType { get; set; }
    public ScriptValue CustomItemId { get; set; }

    public override void OnValidate()
    {
        ItemType.OnValidate();
        CustomItemId.OnValidate();
    }
}
}
